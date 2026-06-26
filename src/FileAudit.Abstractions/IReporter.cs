namespace FileAudit.Abstractions;

public interface IReporter : IAsyncDisposable
{
    Task ReportFileAsync(FileReport report, CancellationToken ct);
}
