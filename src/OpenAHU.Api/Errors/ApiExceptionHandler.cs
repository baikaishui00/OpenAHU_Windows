using Microsoft.AspNetCore.Diagnostics;
using OpenAHU.Application;
using OpenAHU.Contracts.Errors;

namespace OpenAHU.Api.Errors;

/// <summary>集中处理应用异常和未预期异常；仅日志中记录实现细节。</summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) => _logger=logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx,Exception exception,CancellationToken ct)
    {
        if(ctx.Response.HasStarted) return false;
        if(exception is OperationCanceledException && ctx.RequestAborted.IsCancellationRequested)
            return false;
        string code;string message;int status;
        if(exception is CampusException campus)
        {
            (status,code) = campus.Kind switch {
                CampusErrorKind.InvalidRequest => (400,ApiErrorCodes.InvalidRequest),
                CampusErrorKind.NotFound => (404,ApiErrorCodes.NotFound),
                CampusErrorKind.Conflict => (409,ApiErrorCodes.Conflict),
                CampusErrorKind.DbUnavailable => (503,ApiErrorCodes.DbUnavailable),
                _ => (500,ApiErrorCodes.Internal)
            };
            message=campus.Message;
            _logger.LogWarning("业务响应 {Status} {Code} {Path} traceId={Trace}",status,code,ctx.Request.Path,ApiResults.TraceId(ctx));
        }
        else
        {
            (status,code,message)=(500,ApiErrorCodes.Internal,"本地服务发生未预期错误，请稍后重试。");
            _logger.LogError(exception,"请求未处理异常 {Method} {Path} traceId={Trace}",ctx.Request.Method,ctx.Request.Path,ApiResults.TraceId(ctx));
        }
        await ApiResults.WriteAsync(ctx,status,code,message,ct);
        return true;
    }
}
