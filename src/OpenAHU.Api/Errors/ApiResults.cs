using System.Diagnostics;
using OpenAHU.Contracts.Errors;

namespace OpenAHU.Api.Errors;

public static class ApiResults
{
    public static string TraceId(HttpContext context) => Activity.Current?.Id ?? context.TraceIdentifier;
    public static ApiError ErrorBody(string code,string message,HttpContext context) =>
        new(code,message,TraceId(context));
    public static IResult Error(int status,string code,string message,HttpContext context) =>
        Results.Json(ErrorBody(code,message,context),statusCode:status);
    public static Task WriteAsync(HttpContext context,int status,string code,string message,CancellationToken ct)
    {
        context.Response.StatusCode=status;
        context.Response.ContentType="application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(ErrorBody(code,message,context),cancellationToken:ct);
    }
}
