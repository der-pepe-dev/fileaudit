using FileAudit.Abstractions;
using FileAudit.Plugins.BasicRead;

namespace FileAudit.Tests;

public class BasicReadVerifierTests
{
    [Test]
    public async Task CleanFile_NoEvents()
    {
        using var f = new TempFile(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var events = await TestEngine.CollectAsync(new BasicReadVerifier(), f.Path);
        await Assert.That(events).IsEmpty();
    }

    [Test]
    public async Task UnreadableFile_AccessDenied()
    {
        if (OperatingSystem.IsWindows())
            return; // Unix permission bits; CI runs on Linux.

        using var f = new TempFile();
        var mode = File.GetUnixFileMode(f.Path);
        try
        {
            File.SetUnixFileMode(f.Path, UnixFileMode.None); // chmod 000

            var events = await TestEngine.CollectAsync(new BasicReadVerifier(), f.Path);

            await Assert.That(events.Count).IsEqualTo(1);
            await Assert.That(events[0].Severity).IsEqualTo(Severity.Fail);
            await Assert.That(events[0].Kind).IsEqualTo(DefectKind.AccessDenied);
            await Assert.That(events[0].Code).IsEqualTo("io.access_denied");
        }
        finally
        {
            File.SetUnixFileMode(f.Path, mode); // restore so TempFile can delete
        }
    }
}
