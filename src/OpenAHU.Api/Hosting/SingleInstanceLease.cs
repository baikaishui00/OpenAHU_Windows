namespace OpenAHU.Api.Hosting;

/// <summary>当前用户同一交接文件只允许一个后端实例，避免覆盖会话令牌。</summary>
public sealed class SingleInstanceLease : IDisposable
{
    private readonly FileStream _file;
    private SingleInstanceLease(FileStream file) => _file=file;
    public static SingleInstanceLease Acquire(string handshakePath)
    {
        var lockPath=handshakePath+".lock";
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        try
        {
            return new SingleInstanceLease(new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None));
        }
        catch (IOException)
        {
            throw new InvalidOperationException("已有OpenAHU本地服务正在运行。请使用现有会话或关闭旧进程。");
        }
    }
    public void Dispose() => _file.Dispose();
}
