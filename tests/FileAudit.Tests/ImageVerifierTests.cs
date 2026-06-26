using FileAudit.Abstractions;
using FileAudit.Plugins.Image;

namespace FileAudit.Tests;

public class ImageVerifierTests
{
    private static readonly byte[] PngSig = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    // sig + IEND chunk (len=0, "IEND", 4 CRC bytes). The check seeks chunks; CRC is not validated.
    private static byte[] PngClean() =>
    [
        .. PngSig,
        0x00, 0x00, 0x00, 0x00,           // length = 0
        0x49, 0x45, 0x4E, 0x44,           // "IEND"
        0xAE, 0x42, 0x60, 0x82            // CRC (unchecked)
    ];

    private static byte[] PngTrailing() => [.. PngClean(), 0xDE, 0xAD];

    private static byte[] JpegClean() => [0xFF, 0xD8, 0xFF, 0xD9]; // SOI ... EOI
    private static byte[] JpegTrailing() => [.. JpegClean(), 0xDE, 0xAD];

    private static async Task<List<DefectEvent>> Verify(string path)
    {
        var v = new ImageVerifier();
        var events = new List<DefectEvent>();
        await foreach (var e in v.VerifyAsync(path, TestEngine.Options(), CancellationToken.None))
            events.Add(e);
        return events;
    }

    [Test]
    public async Task PngClean_NoEvents()
    {
        using var f = new TempFile(PngClean());
        var events = await Verify(f.Path);
        await Assert.That(events).IsEmpty();
    }

    [Test]
    public async Task PngTrailing_WarnsWithContractCode()
    {
        using var f = new TempFile(PngTrailing());
        var events = await Verify(f.Path);

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Severity).IsEqualTo(Severity.Warn);
        await Assert.That(events[0].Kind).IsEqualTo(DefectKind.TrailingData);
        await Assert.That(events[0].Code).IsEqualTo("image.png.trailing_data");
        await Assert.That(events[0].Tool).IsEqualTo("image");
    }

    [Test]
    public async Task JpegClean_NoEvents()
    {
        using var f = new TempFile(JpegClean());
        var events = await Verify(f.Path);
        await Assert.That(events).IsEmpty();
    }

    [Test]
    public async Task JpegTrailing_WarnsWithContractCode()
    {
        using var f = new TempFile(JpegTrailing());
        var events = await Verify(f.Path);

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Severity).IsEqualTo(Severity.Warn);
        await Assert.That(events[0].Kind).IsEqualTo(DefectKind.TrailingData);
        await Assert.That(events[0].Code).IsEqualTo("image.jpeg.trailing_data");
    }

    [Test]
    public async Task CanVerify_ByExtension()
    {
        var v = new ImageVerifier();
        await Assert.That(v.CanVerify("/x/photo.png", ReadOnlySpan<byte>.Empty)).IsTrue();
        await Assert.That(v.CanVerify("/x/photo.JPG", ReadOnlySpan<byte>.Empty)).IsTrue();
    }

    [Test]
    public async Task CanVerify_ByMagic_WhenExtensionUnknown()
    {
        var v = new ImageVerifier();
        await Assert.That(v.CanVerify("/x/blob.bin", PngSig)).IsTrue();
        await Assert.That(v.CanVerify("/x/blob.bin", new byte[] { 0xFF, 0xD8, 0xFF })).IsTrue();
    }

    [Test]
    public async Task CanVerify_False_ForNonImage()
    {
        var v = new ImageVerifier();
        await Assert.That(v.CanVerify("/x/notes.txt", new byte[] { 0x68, 0x69 })).IsFalse();
    }
}
