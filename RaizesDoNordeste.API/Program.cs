using Microsoft.EntityFrameworkCore;
using RaizesDoNordeste.Application;
using RaizesDoNordeste.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "A conexão ConnectionStrings:Default não foi configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAppDbContext>(services =>
    services.GetRequiredService<AppDbContext>());

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
