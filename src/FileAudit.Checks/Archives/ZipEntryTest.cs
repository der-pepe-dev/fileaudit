using System.IO.Compression;

namespace FileAudit.Checks.Archives;

public static class ZipEntryTest
{
    public static IEnumerable<CheckFinding> TestAllEntries(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ZipArchive? zip = null;
        CheckFinding? parseFail = null;

        try
        {
            zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);
        }
        catch (Exception ex)
        {
            parseFail = new CheckFinding("archive.zip.parse_failed", $"ZIP parse failed: {ex.Message}");
        }

        if (parseFail is not null)
        {
            yield return parseFail;
            yield break;
        }

        foreach (var entry in zip!.Entries)
        {
            CheckFinding? finding = null;
            try
            {
                using var es = entry.Open();
                es.CopyTo(Stream.Null);
            }
            catch (InvalidDataException ex)
            {
                finding = new CheckFinding(
                    "archive.zip.entry_crc_mismatch",
                    $"ZIP entry read failed (CRC/data): {ex.Message}",
                    $"entry={entry.FullName}"
                );
            }
            catch (Exception ex)
            {
                finding = new CheckFinding(
                    "archive.zip.entry_read_failed",
                    $"ZIP entry read failed: {ex.Message}",
                    $"entry={entry.FullName}"
                );
            }

            if (finding is not null)
                yield return finding;
        }
    }
}
