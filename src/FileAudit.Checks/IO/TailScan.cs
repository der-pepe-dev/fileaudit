namespace FileAudit.Checks.IO;

public static class TailScan
{
    public static long FindLastPatternInTail(string path, ReadOnlySpan<byte> pattern, int tailBytes)
    {
        var fi = new FileInfo(path);
        if (!fi.Exists) return -1;

        long size = fi.Length;
        if (size <= 0) return -1;

        int toRead = (int)Math.Min(tailBytes, size);
        long start = size - toRead;

        byte[] buf = new byte[toRead];
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fs.Seek(start, SeekOrigin.Begin);
        int got = fs.Read(buf, 0, toRead);
        if (got <= 0) return -1;

        int last = -1;
        for (int i = 0; i <= got - pattern.Length; i++)
        {
            if (buf.AsSpan(i, pattern.Length).SequenceEqual(pattern))
                last = i;
        }

        return last < 0 ? -1 : start + last;
    }
}
