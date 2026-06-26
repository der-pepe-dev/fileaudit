using System.Text.Json;
using FileAudit.Abstractions;
using FileAudit.Core.Engine;

namespace FileAudit.Tests;

public class JsonlContractTests
{
    private static async Task<JsonElement> FirstLine(
        string input, ScanOptions options, params IVerifier[] verifiers)
    {
        string outPath = Path.Combine(Path.GetTempPath(), $"fa-{Guid.NewGuid():N}.jsonl");
        try
        {
            var engine = new AuditEngine(verifiers);
            await using (var reporter = new JsonlReporter(outPath))
                await engine.ScanAsync(new[] { input }, options, reporter, CancellationToken.None);

            string line = File.ReadLines(outPath).First();
            // Clone so the parsed value outlives the JsonDocument.
            return JsonDocument.Parse(line).RootElement.Clone();
        }
        finally
        {
            File.Delete(outPath);
        }
    }

    [Test]
    public async Task Keys_AreSnakeCase_NotPascalCase()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "archive.zip.parse_failed"));

        var root = await FirstLine(f.Path, TestEngine.Options(), v);

        foreach (var key in new[] { "file", "utc", "mode", "status", "verifiers_run", "events" })
            await Assert.That(root.TryGetProperty(key, out _)).IsTrue();
        await Assert.That(root.TryGetProperty("Status", out _)).IsFalse();
        await Assert.That(root.TryGetProperty("File", out _)).IsFalse();
    }

    [Test]
    public async Task Enums_AreContractStrings()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "archive.zip.parse_failed"));

        var root = await FirstLine(f.Path, TestEngine.Options(), v);

        await Assert.That(root.GetProperty("mode").GetString()).IsEqualTo("audit");
        await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("FAIL");

        var ev = root.GetProperty("events")[0];
        await Assert.That(ev.GetProperty("severity").GetString()).IsEqualTo("FAIL");
        await Assert.That(ev.GetProperty("kind").GetString()).IsEqualTo("ParseError"); // DefectKind: PascalCase per contract
        await Assert.That(ev.GetProperty("code").GetString()).IsEqualTo("archive.zip.parse_failed");
    }

    [Test]
    public async Task NullOptionalEventFields_AreOmitted()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "archive.zip.parse_failed"));

        var root = await FirstLine(f.Path, TestEngine.Options(), v);
        var ev = root.GetProperty("events")[0];

        await Assert.That(ev.TryGetProperty("location", out _)).IsFalse();
        await Assert.That(ev.TryGetProperty("tool", out _)).IsFalse();
    }

    [Test]
    public async Task ReadMode_OnFail_UsesHyphenatedToken()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true);

        var root = await FirstLine(f.Path, TestEngine.Options(read: ReadMode.OnFail), v);

        await Assert.That(root.GetProperty("read_mode").GetString()).IsEqualTo("on-fail");
    }

    [Test]
    public async Task ReadMode_OmittedInReadMode()
    {
        using var f = new TempFile();
        var basicread = new FakeVerifier("basicread", 0, canVerify: true);

        var root = await FirstLine(f.Path, TestEngine.Options(mode: ScanMode.Read), basicread);

        await Assert.That(root.GetProperty("mode").GetString()).IsEqualTo("read");
        await Assert.That(root.TryGetProperty("read_mode", out _)).IsFalse();
    }
}
