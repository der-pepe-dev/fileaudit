using FileAudit.Abstractions;
using FileAudit.Checks.Archives;

namespace FileAudit.Plugins.Zip;

public sealed class ZipVerifier : IVerifier
{
    public string Name => "zip";
    public int Order => 10;

    private static readonly HashSet<string> Exts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".cbz", ".epub", ".docx", ".xlsx", ".pptx"
    };

    public bool CanVerify(string path, ReadOnlySpan<byte> header)
    {
        var ext = Path.GetExtension(path);
        if (Exts.Contains(ext)) return true;

        // Optional ZIP magic sniff
        if (header.Length >= 4 && header[0] == (byte)'P' && header[1] == (byte)'K')
            return true;

        return false;
    }

    public async IAsyncEnumerable<DefectEvent> VerifyAsync(string path, ScanOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        foreach (var f in ZipEntryTest.TestAllEntries(path))
        {
            ct.ThrowIfCancellationRequested();

            yield return f.Code switch
            {
                "archive.zip.entry_crc_mismatch" => new DefectEvent(Severity.Fail, DefectKind.CrcMismatch, f.Code, f.Message, f.Location, Tool: Name),
                "archive.zip.parse_failed" => new DefectEvent(Severity.Fail, DefectKind.ParseError, f.Code, f.Message, f.Location, Tool: Name),
                "archive.zip.entry_read_failed" => new DefectEvent(Severity.Fail, DefectKind.ParseError, f.Code, f.Message, f.Location, Tool: Name),
                _ => new DefectEvent(Severity.Fail, DefectKind.ParseError, f.Code, f.Message, f.Location, Tool: Name),
            };
        }

        await Task.CompletedTask;
    }
}
