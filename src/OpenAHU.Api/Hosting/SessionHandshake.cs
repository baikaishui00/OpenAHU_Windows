using System.Text;
using System.Text.Json;
using OpenAHU.Contracts.Json;

namespace OpenAHU.Api.Hosting;

/// <summary>
/// 后端启动后写给桌面端的交接文件内容（<c>%LocalAppData%\OpenAHU\session.json</c>）。
/// 这是进程间运维用品，不是对外 API 负载，因此不放进 OpenAHU.Contracts。
/// </summary>
/// <param name="Port">实际绑定的回环端口（由系统随机分配）。</param>
/// <param name="Token">会话令牌，客户端放入 <c>X-OpenAHU-Session</c> 头。</param>
/// <param name="ProcessId">后端进程号，客户端退出时用于确认进程归属。</param>
/// <param name="StartedAt">写入时间（带偏移）。</param>
/// <param name="BaseUrl">完整基地址，例如 <c>http://127.0.0.1:54321</c>。</param>
public sealed record SessionHandshake(
    int Port,
    string Token,
    int ProcessId,
    DateTimeOffset StartedAt,
    string BaseUrl)
{
    /// <summary>交接文件默认位置：当前用户的 LocalAppData 下，其他用户无权读取。</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenAHU",
        "session.json");

    /// <summary>从 Kestrel 实际监听地址构造交接内容；取不到有效端口时抛出。</summary>
    /// <param name="urls">宿主地址集合（<c>WebApplication.Urls</c>）。</param>
    /// <param name="token">本次运行生成的会话令牌。</param>
    /// <param name="processId">当前进程号。</param>
    /// <returns>写盘用的交接内容。</returns>
    /// <exception cref="InvalidOperationException">没有可用地址或端口未分配。</exception>
    public static SessionHandshake Create(IEnumerable<string>? urls, string token, int processId)
    {
        var url = urls?.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
        if (url is null ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Port <= 0)
        {
            throw new InvalidOperationException("无法获取回环监听地址，后端未能完成启动握手。");
        }

        return new SessionHandshake(uri.Port, token, processId, DateTimeOffset.Now, uri.GetLeftPart(UriPartial.Authority));
    }

    /// <summary>原子写入交接文件（先写临时文件再替换，避免客户端读到半截 JSON）。</summary>
    /// <param name="path">目标路径。</param>
    /// <param name="handshake">交接内容。</param>
    public static void Write(string path, SessionHandshake handshake)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(handshake, ContractJson.CreateOptions());
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>删除交接文件；进程异常退出时残留文件由下次启动覆盖。</summary>
    /// <param name="path">目标路径。</param>
    public static void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // 退出路径上的清理失败不应掩盖真正的关闭原因。
        }
        catch (UnauthorizedAccessException)
        {
            // 同上：交接文件残留不会造成安全问题，下次启动会覆盖。
        }
    }
}
