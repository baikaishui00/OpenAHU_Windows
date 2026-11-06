using System.Net;
using OpenAHU.Api.Errors;

namespace OpenAHU.Api.Hosting;

/// <summary>
/// 会话握手 + 回环来源校验中间件。所有 <c>/api/*</c> 请求必须同时满足：
/// ① 来源是回环地址；② 携带与本次运行一致的 <see cref="SessionToken.HeaderName"/>。
/// 只靠 CORS 无法阻止本机其他进程用 HttpClient 直接调用写入接口。
/// </summary>
public sealed class SessionTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SessionToken _token;
    private readonly ILogger<SessionTokenMiddleware> _logger;

    /// <summary>创建中间件。</summary>
    /// <param name="next">管道中的下一个委托。</param>
    /// <param name="token">本次运行的会话令牌。</param>
    /// <param name="logger">日志。</param>
    public SessionTokenMiddleware(RequestDelegate next, SessionToken token, ILogger<SessionTokenMiddleware> logger)
    {
        _next = next;
        _token = token;
        _logger = logger;
    }

    /// <summary>执行校验；不通过时直接返回 <c>401</c> 与统一错误体。</summary>
    /// <param name="context">当前请求上下文。</param>
    /// <returns>表示异步处理的任务。</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!IsLoopback(context.Connection.RemoteIpAddress))
        {
            _logger.LogWarning(
                "拒绝非回环来源：{Method} {Path} from {RemoteIp}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);
            await WriteUnauthorizedAsync(context);
            return;
        }

        var provided = context.Request.Headers[SessionToken.HeaderName].ToString();
        if (!_token.Matches(provided))
        {
            _logger.LogWarning("会话令牌不匹配：{Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteUnauthorizedAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsLoopback(IPAddress? address)
    {
        return address is not null && IPAddress.IsLoopback(address);
    }

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        return ApiResults.WriteAsync(
            context,
            StatusCodes.Status401Unauthorized,
            OpenAHU.Contracts.Errors.ApiErrorCodes.Unauthorized,
            "本地服务会话无效，请从桌面客户端启动本程序。",
            context.RequestAborted);
    }
}
