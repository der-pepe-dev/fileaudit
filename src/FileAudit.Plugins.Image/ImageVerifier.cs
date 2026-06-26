using System.Runtime.CompilerServices;
using FileAudit.Abstractions;
using FileAudit.Checks;
using FileAudit.Checks.Images;

namespace FileAudit.Plugins.Image;

public sealed class ImageVerifier : IVerifier
{
    public string Name => "image";
    public int Order => 5; // cheap header check; PNG/JPEG never co-match zip/sqlite/ffmpeg

    private static readonly byte[] PngSig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegSoi = { 0xFF, 0xD8, 0xFF };

    private static readonly HashSet<string> Exts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg"
    };

    public bool CanVerify(string path, ReadOnlySpan<byte> header)
    {
        if (Exts.Contains(Path.GetExtension(path))) return true;
        return IsPng(header) || IsJpeg(header);
    }

    public async IAsyncEnumerable<DefectEvent> VerifyAsync(
        string path, ScanOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        // VerifyAsync is not given the sniffed header; re-read a few bytes to classify.
        Span<byte> head = stackalloc byte[8];
        int n = ReadHead(path, head);
        ReadOnlySpan<byte> header = head[..n];

        CheckFinding? finding =
            IsPng(header) ? PngTrailingData.TryDetectTrailingData(path) :
            IsJpeg(header) ? JpegTrailingData.TryDetectTrailingData(path) :
            null;

        if (finding is not null)
        {
            ct.ThrowIfCancellationRequested();
            yield return new DefectEvent(
                Severity.Warn, DefectKind.TrailingData, finding.Code, finding.Message, finding.Location, Tool: Name);
        }

        await Task.CompletedTask;
    }

    private static int ReadHead(string path, Span<byte> buffer)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int total = 0;
        while (total < buffer.Length)
        {
            int read = fs.Read(buffer[total..]);
            if (read <= 0) break;
            total += read;
        }
        return total;
    }

    private static bool IsPng(ReadOnlySpan<byte> header)
        => header.Length >= PngSig.Length && header[..PngSig.Length].SequenceEqual(PngSig);

    private static bool IsJpeg(ReadOnlySpan<byte> header)
        => header.Length >= JpegSoi.Length && header[..JpegSoi.Length].SequenceEqual(JpegSoi);
}
