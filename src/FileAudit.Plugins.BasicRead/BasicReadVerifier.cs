using FileAudit.Abstractions;

namespace FileAudit.Plugins.BasicRead;

public sealed class BasicReadVerifier : IVerifier
{
    public string Name => "basicread";
    public int Order => 0;

    public bool CanVerify(string path, ReadOnlySpan<byte> header) => true;

    public async IAsyncEnumerable<DefectEvent> VerifyAsync(string path, ScanOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        long offset = 0;
        byte[] buf = new byte[Math.Max(4096, options.ReadBufferBytes)];

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                int n = await fs.ReadAsync(buf, ct);
                if (n <= 0) break;
                offset += n;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            yield return new DefectEvent(Severity.Fail, DefectKind.AccessDenied, "io.access_denied", ex.Message, Location: $"offset={offset}", Tool: Name);
        }
        catch (Exception ex)
        {
            yield return new DefectEvent(Severity.Fail, DefectKind.IoReadError, "io.read_failed", ex.Message, Location: $"offset={offset}", Tool: Name);
        }
    }
}
