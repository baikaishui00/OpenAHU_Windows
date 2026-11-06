namespace OpenAHU.Application;

public enum CampusErrorKind { InvalidRequest, NotFound, Conflict, DbUnavailable }

/// <summary>应用层可预期异常；只携带可向用户展示的非敏感消息。</summary>
public sealed class CampusException : Exception
{
    public CampusErrorKind Kind { get; }
    public CampusException(CampusErrorKind kind, string message) : base(message) => Kind = kind;
}
