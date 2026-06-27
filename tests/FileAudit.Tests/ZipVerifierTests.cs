using System.IO.Compression;
using FileAudit.Abstractions;
using FileAudit.Plugins.Zip;

namespace FileAudit.Tests;

public class ZipVerifierTests
{
    private static byte[] BuildZip(
        string entryName = "hello.txt",
        string content = "hello world",
        CompressionLevel level = CompressionLevel.NoCompression)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(entryName, level);
            using var es = entry.Open();
            es.Write(System.Text.Encoding.ASCII.GetBytes(content));
        }
        return ms.ToArray();
    }

    [Test]
    public async Task CleanZip_NoEvents()
    {
        using var f = new TempFile(BuildZip());
        var events = await TestEngine.CollectAsync(new ZipVerifier(), f.Path);
        await Assert.That(events).IsEmpty();
    }

    [Test]
    public async Task GarbageZip_ParseFailed()
    {
        using var p = new TempPath(".zip");
        File.WriteAllBytes(p.Path, new byte[] { 0x50, 0x4B, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });

        var events = await TestEngine.CollectAsync(new ZipVerifier(), p.Path);

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Severity).IsEqualTo(Severity.Fail);
        await Assert.That(events[0].Code).IsEqualTo("archive.zip.parse_failed");
    }

    [Test]
    public async Task TruncatedZip_ParseFailed()
    {
        byte[] full = BuildZip();
        byte[] truncated = full[..(full.Length / 2)]; // drop end-of-central-directory
        using var p = new TempPath(".zip");
        File.WriteAllBytes(p.Path, truncated);

        var events = await TestEngine.CollectAsync(new ZipVerifier(), p.Path);

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Code).IsEqualTo("archive.zip.parse_failed");
    }

    [Test]
    public async Task CorruptEntryData_FailsOnEntryRead()
    {
        // Deflated entry with compressible content. Corrupt the whole compressed-data region
        // (between the local file header and the central directory) so inflate fails on read,
        // while leaving the central directory + end-of-central-directory intact so the archive
        // still opens. .NET does not flag a stored-entry CRC flip on read, but a broken
        // DEFLATE stream throws InvalidDataException deterministically.
        string content = string.Concat(Enumerable.Repeat("The quick brown fox 0123456789 ", 200));
        byte[] bytes = BuildZip("a.txt", content, CompressionLevel.Optimal);

        int cd = IndexOf(bytes, new byte[] { 0x50, 0x4B, 0x01, 0x02 }); // central directory start
        await Assert.That(cd).IsGreaterThan(44); // sanity: room for a compressed-data region
        for (int i = 40; i < cd - 4; i++)
            bytes[i] ^= 0xFF;

        using var p = new TempPath(".zip");
        File.WriteAllBytes(p.Path, bytes);

        var events = await TestEngine.CollectAsync(new ZipVerifier(), p.Path);

        await Assert.That(events.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(events[0].Severity).IsEqualTo(Severity.Fail);
        await Assert.That(new[] { "archive.zip.entry_crc_mismatch", "archive.zip.entry_read_failed" })
            .Contains(events[0].Code);
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
                return i;
        }
        return -1;
    }
}
