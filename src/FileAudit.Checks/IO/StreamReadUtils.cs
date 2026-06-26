using System.Buffers.Binary;

namespace FileAudit.Checks.IO;

public static class StreamReadUtils
{
    public static bool TryReadExactly(Stream s, Span<byte> buffer)
    {
        int readTotal = 0;
        while (readTotal < buffer.Length)
        {
            int n = s.Read(buffer.Slice(readTotal));
            if (n <= 0) return false;
            readTotal += n;
        }
        return true;
    }

    public static uint ReadU32BE(ReadOnlySpan<byte> b) => BinaryPrimitives.ReadUInt32BigEndian(b);
}
