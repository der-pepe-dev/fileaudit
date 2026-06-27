using System.Runtime.CompilerServices;
using FileAudit.Abstractions;
using FileAudit.Core.Engine;

namespace FileAudit.Tests;

/// <summary>Verifier with scriptable match decision and emitted events.</summary>
internal sealed class FakeVerifier : IVerifier
{
    private readonly Func<string, bool> _canVerify;
    private readonly IReadOnlyList<DefectEvent> _events;

    public FakeVerifier(string name, int order, bool canVerify, params DefectEvent[] events)
        : this(name, order, _ => canVerify, events) { }

    public FakeVerifier(string name, int order, Func<string, bool> canVerify, params DefectEvent[] events)
    {
        Name = name;
        Order = order;
        _canVerify = canVerify;
        _events = events;
    }

    public string Name { get; }
    public int Order { get; }

    public bool CanVerify(string path, ReadOnlySpan<byte> header) => _canVerify(path);

    public async IAsyncEnumerable<DefectEvent> VerifyAsync(
        string path, ScanOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Yield();
        foreach (var e in _events)
        {
            ct.ThrowIfCancellationRequested();
            yield return e;
        }
    }
}

/// <summary>Collects reports in memory instead of writing JSONL.</summary>
internal sealed class CapturingReporter : IReporter
{
    public List<FileReport> Reports { get; } = new();

    public Task ReportFileAsync(FileReport report, CancellationToken ct)
    {
        Reports.Add(report);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>Temp file that deletes itself on dispose.</summary>
internal sealed class TempFile : IDisposable
{
    public string Path { get; }

    public TempFile(byte[]? content = null)
    {
        Path = System.IO.Path.GetTempFileName();
        File.WriteAllBytes(Path, content ?? new byte[] { 1, 2, 3, 4 });
    }

    public void Dispose()
    {
        try { File.Delete(Path); } catch { /* best effort */ }
    }
}

/// <summary>Unique temp file path that deletes on dispose. Does not pre-create the file
/// (unlike <see cref="TempFile"/>), so callers can create it themselves (e.g. SQLite).</summary>
internal sealed class TempPath : IDisposable
{
    public string Path { get; }

    public TempPath(string extension = ".tmp")
        => Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"fa-{Guid.NewGuid():N}{extension}");

    public void Dispose()
    {
        try { if (File.Exists(Path)) File.Delete(Path); } catch { /* best effort */ }
    }
}

internal static class TestEngine
{
    public static ScanOptions Options(
        ScanMode mode = ScanMode.Audit,
        ReadMode read = ReadMode.Never,
        bool pluginsAfterReadError = false)
        => new(mode, read, pluginsAfterReadError, ReadBufferBytes: 4096, MaxEventsPerFile: 200, ToolsPath: null);

    public static async Task<(int exit, IReadOnlyList<FileReport> reports)> Run(
        IEnumerable<string> inputs, ScanOptions options, params IVerifier[] verifiers)
    {
        var engine = new AuditEngine(verifiers);
        var reporter = new CapturingReporter();
        int exit = await engine.ScanAsync(inputs, options, reporter, CancellationToken.None);
        return (exit, reporter.Reports);
    }

    public static DefectEvent Event(Severity sev, DefectKind kind, string code)
        => new(sev, kind, code, $"{code} message");

    /// <summary>Run a single verifier over a path and collect its events.</summary>
    public static async Task<List<DefectEvent>> CollectAsync(IVerifier verifier, string path)
    {
        var events = new List<DefectEvent>();
        await foreach (var e in verifier.VerifyAsync(path, Options(), CancellationToken.None))
            events.Add(e);
        return events;
    }
}
