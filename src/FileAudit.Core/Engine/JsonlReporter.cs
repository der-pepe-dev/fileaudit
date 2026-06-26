using System.Text.Json;
using FileAudit.Abstractions;

namespace FileAudit.Core.Engine;

public sealed class JsonlReporter : IReporter
{
    private readonly StreamWriter _writer;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = false };

    public JsonlReporter(string outPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        _writer = new StreamWriter(File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.Read));
    }

    public async Task ReportFileAsync(FileReport report, CancellationToken ct)
    {
        await _writer.WriteLineAsync(JsonSerializer.Serialize(report, _json).AsMemory(), ct);
        await _writer.FlushAsync(ct);
    }

    public ValueTask DisposeAsync() => _writer.DisposeAsync();
}
