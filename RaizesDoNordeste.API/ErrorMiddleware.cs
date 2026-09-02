using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RaizesDoNordeste.Application;

namespace RaizesDoNordeste.API
{
    public record ApiError(string Error, string Message, object[] Details, DateTime Timestamp, string Path, string RequestId)
    {
        public static ApiError Create(HttpContext context, string code, string message, object[]? details = null) =>
            new(code, message, details ?? [], DateTime.UtcNow, context.Request.Path.Value ?? "/", context.TraceIdentifier);
    }

    public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try { await next(context); }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                var (status, code, message) = ex switch
                {
                    BusinessException business => (business.StatusCode, business.Code, business.Message),
                    DbUpdateConcurrencyException => (409, "CONCORRENCIA", "O registro foi alterado. Consulte novamente e repita a operação."),
                    DbUpdateException => (409, "CONFLITO_DADOS", "Operação conflita com dados existentes. Consulte e tente novamente."),
                    PostgresException { SqlState: "40001" or "40P01" } => (409, "CONCORRENCIA", "Operações simultâneas. Tente novamente."),
                    SqliteException { SqliteErrorCode: 5 or 6 } => (409, "CONCORRENCIA", "Banco ocupado. Tente novamente."),
                    _ => (500, "ERRO_INTERNO", "Não foi possível concluir a solicitação.")
                };
                // Não registrar payload, senha, token, endereço ou mensagem interna do banco.
                if (status == 500) logger.LogError("Falha {ExceptionType}; requestId={RequestId}", ex.GetType().Name, context.TraceIdentifier);
                context.Response.Clear();
                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(ApiError.Create(context, code, message));
            }
        }
    }
}
