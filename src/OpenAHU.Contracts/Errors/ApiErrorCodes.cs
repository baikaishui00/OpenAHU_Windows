namespace OpenAHU.Contracts.Errors;

/// <summary>
/// <see cref="ApiError.Code"/> 的冻结取值集合，与 HTTP 状态码一一对应。
/// 新增错误码属于契约变更，必须走跨组评审（见 README「变更流程」）。
/// </summary>
public static class ApiErrorCodes
{
    /// <summary>401：无效或缺失的本地进程会话令牌。</summary>
    public const string Unauthorized = "unauthorized";

    /// <summary>400：请求参数不合法（缺参、越界、格式错误）。</summary>
    public const string InvalidRequest = "invalid_request";

    /// <summary>404：资源不存在（未知 termId、未知课程 id）。</summary>
    public const string NotFound = "not_found";

    /// <summary>409：版本/关联冲突（如删除仍有成绩或考试的课程、并发写入冲突）。</summary>
    public const string Conflict = "conflict";

    /// <summary>500：未预期错误，日志中保留细节，响应体只给通用文案。</summary>
    public const string Internal = "internal";

    /// <summary>503：数据库不可用（文件被占用、只读、迁移未完成、无法连接）。</summary>
    public const string DbUnavailable = "db_unavailable";
}
