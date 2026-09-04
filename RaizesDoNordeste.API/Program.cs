using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RaizesDoNordeste.API;
using RaizesDoNordeste.Application;
using RaizesDoNordeste.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "A conexão ConnectionStrings:Default não foi configurada.");

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Key com pelo menos 32 bytes.");
}

var databaseProvider =
    builder.Configuration["Database:Provider"]
    ?? "Postgres";

if (databaseProvider.Equals(
    "Postgres",
    StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else if (databaseProvider.Equals(
    "Sqlite",
    StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    throw new InvalidOperationException(
        "Database:Provider deve ser Postgres ou Sqlite.");
}

builder.Services.AddScoped<IAppDbContext>(services =>
    services.GetRequiredService<AppDbContext>());

// Serviços da aplicação
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IPaymentGateway, MockPaymentGateway>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                allowIntegerValues: false));
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;

    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(item => item.Value!.Errors.Count > 0)
            .Select(item => (object)new
            {
                field = item.Key,
                issue = "Campo obrigatório ausente ou valor inválido."
            })
            .ToArray();

        var error = ApiError.Create(
            context.HttpContext,
            "VALIDACAO",
            "Verifique os campos enviados.",
            details);

        return new BadRequestObjectResult(error);
    };
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Raízes do Nordeste API",
        Version = "v1",
        Description =
            "API acadêmica. Os pagamentos são simulados e não devem ser utilizados dados reais."
    });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Faça login em /auth/login e informe somente o accessToken."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer",
                document)] = []
        });

    options.OperationFilter<ApiDocumentationFilter>();
    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, "RaizesDoNordeste.API.xml"));
});

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,
                ValidIssuer = "RaizesDoNordeste",

                ValidateAudience = true,
                ValidAudience = "RaizesDoNordeste.Clients",

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    if (builder.Configuration.GetValue<bool>("Demo:Seed"))
    {
        await DatabaseSeeder.SeedAsync(db);
    }
}

app.UseMiddleware<ErrorMiddleware>();

app.UseStatusCodePages(async statusContext =>
{
    var httpContext = statusContext.HttpContext;
    var response = httpContext.Response;

    var result = response.StatusCode switch
    {
        401 => (
            "NAO_AUTENTICADO",
            "Informe um token válido."),

        403 => (
            "SEM_PERMISSAO",
            "Seu perfil não tem permissão para esta ação."),

        404 => (
            "NAO_ENCONTRADO",
            "Recurso não encontrado."),

        429 => (
            "LIMITE_REQUISICOES",
            "Aguarde antes de tentar novamente."),

        _ => (
            "ERRO_HTTP",
            "Não foi possível atender a solicitação.")
    };

    await response.WriteAsJsonAsync(
        ApiError.Create(
            httpContext,
            result.Item1,
            result.Item2));
});

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Raízes do Nordeste v1");
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
    Results.Redirect("/swagger"));

app.MapGet("/health", async (AppDbContext db) =>
{
    var connected =
        await db.Database.CanConnectAsync();

    return connected
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(503);
});

app.MapControllers();

app.Run();

// Permite que WebApplicationFactory inicialize a API nos testes de integração
public partial class Program
{
}
