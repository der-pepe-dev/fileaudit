using FileAudit.Abstractions;

namespace FileAudit.Tests;

public class StatusAndExitTests
{
    [Test]
    public async Task NoMatch_NoRead_IsSkip_Exit0()
    {
        using var f = new TempFile();
        var semantic = new FakeVerifier("zip", 10, canVerify: false);

        var (exit, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(), semantic);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Skip);
        await Assert.That(reports[0].Coverage).IsEqualTo("none");
        await Assert.That(exit).IsEqualTo(0);
    }

    [Test]
    public async Task MatchedVerifier_NoEvents_IsOk_Exit0()
    {
        using var f = new TempFile();
        var semantic = new FakeVerifier("zip", 10, canVerify: true);

        var (exit, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(), semantic);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Ok);
        await Assert.That(exit).IsEqualTo(0);
    }

    [Test]
    public async Task WarnEvent_IsWarn_Exit2()
    {
        using var f = new TempFile();
        var semantic = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Warn, DefectKind.DecodeWarning, "media.decode.warning"));

        var (exit, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(), semantic);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Warn);
        await Assert.That(exit).IsEqualTo(2);
    }

    [Test]
    public async Task FailEvent_IsFail_Exit3()
    {
        using var f = new TempFile();
        var semantic = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "db.sqlite.open_failed"));

        var (exit, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(), semantic);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Fail);
        await Assert.That(exit).IsEqualTo(3);
    }

    [Test]
    public async Task InfoOnly_DoesNotUpgrade_StaysOk()
    {
        using var f = new TempFile();
        var semantic = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Info, DefectKind.UnsupportedFormat, "core.info"));

        var (_, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(), semantic);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Ok);
    }

    // Regression: clean full read is real verification -> OK, not SKIP (PR #1 fix).
    [Test]
    public async Task CleanFullRead_ReadMode_IsOk()
    {
        using var f = new TempFile();
        var basicread = new FakeVerifier("basicread", 0, canVerify: true); // emits nothing

        var (exit, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(mode: ScanMode.Read), basicread);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Ok);
        await Assert.That(reports[0].Coverage).IsEqualTo("fullread");
        await Assert.That(exit).IsEqualTo(0);
    }

    // Regression: unmatched format + clean fallback BasicRead -> OK, not SKIP (PR #1 Codex fix).
    [Test]
    public async Task UnmatchedThenCleanFallbackRead_IsOk()
    {
        using var f = new TempFile();
        var basicread = new FakeVerifier("basicread", 0, canVerify: true);
        var semantic = new FakeVerifier("zip", 10, canVerify: false);

        var (exit, reports) = await TestEngine.Run(
            new[] { f.Path }, TestEngine.Options(read: ReadMode.Unmatched), basicread, semantic);

        await Assert.That(reports[0].Status).IsEqualTo(FileStatus.Ok);
        await Assert.That(reports[0].Coverage).IsEqualTo("fullread");
        await Assert.That(exit).IsEqualTo(0);
    }

    [Test]
    public async Task ExitCode_IsMaxAcrossFiles()
    {
        using var warnFile = new TempFile();
        using var failFile = new TempFile();

        // First a WARN file, then a FAIL file: exit must be the max (3).
        var failVerifier = new FakeVerifier("sqlite", 20, canVerify: path => path == failFile.Path,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "db.sqlite.open_failed"));
        var warnVerifier = new FakeVerifier("zip", 10, canVerify: path => path == warnFile.Path,
            TestEngine.Event(Severity.Warn, DefectKind.DecodeWarning, "media.decode.warning"));

        var (exit, reports) = await TestEngine.Run(
            new[] { warnFile.Path, failFile.Path }, TestEngine.Options(), warnVerifier, failVerifier);

        await Assert.That(reports.Count).IsEqualTo(2);
        await Assert.That(exit).IsEqualTo(3);
    }
}
