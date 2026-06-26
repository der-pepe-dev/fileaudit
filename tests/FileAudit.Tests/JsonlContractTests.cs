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

    [Fact]
    public async Task Keys_AreSnakeCase_NotPascalCase()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "archive.zip.parse_failed"));

        var root = await FirstLine(f.Path, TestEngine.Options(), v);

        foreach (var key in new[] { "file", "utc", "mode", "status", "verifiers_run", "events" })
            Assert.True(root.TryGetProperty(key, out _), $"missing key: {key}");
        Assert.False(root.TryGetProperty("Status", out _), "PascalCase 'Status' leaked");
        Assert.False(root.TryGetProperty("File", out _), "PascalCase 'File' leaked");
    }

    [Fact]
    public async Task Enums_AreContractStrings()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "archive.zip.parse_failed"));

        var root = await FirstLine(f.Path, TestEngine.Options(), v);

        Assert.Equal("audit", root.GetProperty("mode").GetString());
        Assert.Equal("FAIL", root.GetProperty("status").GetString());

        var ev = root.GetProperty("events")[0];
        Assert.Equal("FAIL", ev.GetProperty("severity").GetString());
        Assert.Equal("ParseError", ev.GetProperty("kind").GetString()); // DefectKind: PascalCase per contract
        Assert.Equal("archive.zip.parse_failed", ev.GetProperty("code").GetString());
    }

    [Fact]
    public async Task NullOptionalEventFields_AreOmitted()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true,
            TestEngine.Event(Severity.Fail, DefectKind.ParseError, "archive.zip.parse_failed"));

        var root = await FirstLine(f.Path, TestEngine.Options(), v);
        var ev = root.GetProperty("events")[0];

        Assert.False(ev.TryGetProperty("location", out _), "null location should be omitted");
        // Tool was set on core events but not on TestEngine.Event(); this one has no tool.
        Assert.False(ev.TryGetProperty("tool", out _), "null tool should be omitted");
    }

    [Fact]
    public async Task ReadMode_OnFail_UsesHyphenatedToken()
    {
        using var f = new TempFile();
        var v = new FakeVerifier("zip", 10, canVerify: true);

        var root = await FirstLine(f.Path, TestEngine.Options(read: ReadMode.OnFail), v);

        Assert.Equal("on-fail", root.GetProperty("read_mode").GetString());
    }

    [Fact]
    public async Task ReadMode_OmittedInReadMode()
    {
        using var f = new TempFile();
        var basicread = new FakeVerifier("basicread", 0, canVerify: true);

        var root = await FirstLine(f.Path, TestEngine.Options(mode: ScanMode.Read), basicread);

        Assert.Equal("read", root.GetProperty("mode").GetString());
        Assert.False(root.TryGetProperty("read_mode", out _), "read_mode should be omitted in read mode");
    }
}
