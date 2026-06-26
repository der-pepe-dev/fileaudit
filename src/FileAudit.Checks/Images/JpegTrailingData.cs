using FileAudit.Checks.IO;

namespace FileAudit.Checks.Images;

public static class JpegTrailingData
{
    public static CheckFinding? TryDetectTrailingData(string path, int tailScanBytes = 4 * 1024 * 1024)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists) return null;
        long size = fi.Length;
        if (size < 4) return null;

        long eoi = TailScan.FindLastPatternInTail(path, new byte[] { 0xFF, 0xD9 }, tailScanBytes);
        if (eoi < 0) return null;

        long end = eoi + 2;
        long trailing = size - end;
        if (trailing <= 0) return null;

        return new CheckFinding(
            Code: "image.jpeg.trailing_data",
            Message: "Trailing bytes found after JPEG EOI marker.",
            Location: $"end={end} trailing={trailing}"
        );
    }
}
