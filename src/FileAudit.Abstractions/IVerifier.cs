namespace FileAudit.Abstractions;

public interface IVerifier
{
    string Name { get; }
    int Order { get; } // lower runs first

    bool CanVerify(string path, ReadOnlySpan<byte> header);

    IAsyncEnumerable<DefectEvent> VerifyAsync(string path, ScanOptions options, CancellationToken ct);
}
