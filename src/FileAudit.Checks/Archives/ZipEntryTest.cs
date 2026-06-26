using System.IO.Compression;

namespace FileAudit.Checks.Archives;

public static class ZipEntryTest
{
    public static IEnumerable<CheckFinding> TestAllEntries(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ZipArchive? zip;

        try
        {
            zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);
        }
        catch (Exception ex)
        {
            yield return new CheckFinding("archive.zip.parse_failed", $"ZIP parse failed: {ex.Message}");
            yield break;
        }

        foreach (var entry in zip.Entries)
        {
            try
            {
                using var es = entry.Open();
                es.CopyTo(Stream.Null);
            }
            catch (InvalidDataException ex)
            {
                yield return new CheckFinding(
                    "archive.zip.entry_crc_mismatch",
                    $"ZIP entry read failed (CRC/data): {ex.Message}",
                    $"entry={entry.FullName}"
                );
            }
            catch (Exception ex)
            {
                yield return new CheckFinding(
                    "archive.zip.entry_read_failed",
                    $"ZIP entry read failed: {ex.Message}",
                    $"entry={entry.FullName}"
                );
            }
        }
    }
}
