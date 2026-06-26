using System.Text;
using FileAudit.Checks.IO;

namespace FileAudit.Checks.Images;

public static class PngTrailingData
{
    private static readonly byte[] PngSig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static CheckFinding? TryDetectTrailingData(string path)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists) return null;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Span<byte> sig = stackalloc byte[8];
        if (!StreamReadUtils.TryReadExactly(fs, sig)) return null;
        if (!sig.SequenceEqual(PngSig)) return null;

        Span<byte> lenBuf = stackalloc byte[4];
        Span<byte> typeBuf = stackalloc byte[4];
        while (true)
        {
            if (!StreamReadUtils.TryReadExactly(fs, lenBuf)) return null;
            if (!StreamReadUtils.TryReadExactly(fs, typeBuf)) return null;

            uint len = StreamReadUtils.ReadU32BE(lenBuf);
            string type = Encoding.ASCII.GetString(typeBuf);

            if (fs.Length - fs.Position < (long)len + 4) return null;
            fs.Seek(len + 4, SeekOrigin.Current);

            if (type == "IEND")
            {
                long end = fs.Position;
                long trailing = fi.Length - end;
                if (trailing <= 0) return null;

                return new CheckFinding(
                    Code: "image.png.trailing_data",
                    Message: "Trailing bytes found after PNG IEND chunk.",
                    Location: $"end={end} trailing={trailing}"
                );
            }
        }
    }
}
