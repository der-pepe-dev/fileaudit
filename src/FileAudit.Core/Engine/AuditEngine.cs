using FileAudit.Abstractions;

namespace FileAudit.Core.Engine;

public sealed class AuditEngine
{
    private readonly IReadOnlyList<IVerifier> _verifiers;

    public AuditEngine(IEnumerable<IVerifier> verifiers)
        => _verifiers = verifiers.OrderBy(v => v.Order).ToList();

    public async Task<int> ScanAsync(IEnumerable<string> inputs, ScanOptions options, IReporter reporter, CancellationToken ct)
    {
        int maxExit = 0; // 0 OK/SKIP, 2 WARN, 3 FAIL

        foreach (var path in ExpandInputs(inputs))
        {
            ct.ThrowIfCancellationRequested();

            var report = await ScanOneAsync(path, options, ct);
            await reporter.ReportFileAsync(report, ct);

            maxExit = Math.Max(maxExit, report.Status switch
            {
                FileStatus.Fail => 3,
                FileStatus.Warn => 2,
                _ => 0
            });
        }

        return maxExit;
    }

    private async Task<FileReport> ScanOneAsync(string path, ScanOptions options, CancellationToken ct)
    {
        DateTime utc = DateTime.UtcNow;
        var events = new List<DefectEvent>(capacity: 8);
        var ran = new List<string>(capacity: 6);

        // Header sniff (best effort)
        byte[] header = new byte[64];
        int headerLen = 0;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            headerLen = await fs.ReadAsync(header, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            events.Add(new DefectEvent(Severity.Fail, DefectKind.AccessDenied, "io.access_denied", ex.Message, Tool: "basicread"));
            return Build(path, utc, options, ran, events, "none", "none", false, true, null);
        }
        catch (Exception ex)
        {
            events.Add(new DefectEvent(Severity.Fail, DefectKind.IoReadError, "io.open_failed", ex.Message, Tool: "basicread"));
            return Build(path, utc, options, ran, events, "none", "none", false, true, null);
        }

        ReadOnlySpan<byte> headerSpan = new ReadOnlySpan<byte>(header, 0, headerLen);

        // Applicable semantic verifiers (exclude basicread by name).
        // Explicit loop: ReadOnlySpan cannot be captured in a LINQ lambda (CS8175).
        var applicable = new List<IVerifier>();
        foreach (var v in _verifiers)
        {
            if (v.Name == "basicread") continue;
            if (v.CanVerify(path, headerSpan)) applicable.Add(v);
        }

        bool readPerformed = false;
        bool ioFailed = false;
        string readPhase = "none";
        string coverage = "none";
        bool? pluginsAfter = null;

        var basicRead = _verifiers.FirstOrDefault(v => v.Name == "basicread");

        if (options.Mode == ScanMode.Read)
        {
            if (basicRead is null)
            {
                events.Add(new DefectEvent(Severity.Fail, DefectKind.ExternalToolFailed, "core.missing_basicread", "basicread verifier missing", Tool: "core"));
                return Build(path, utc, options, ran, events, "none", "none", false, true, null);
            }

            ran.Add(basicRead.Name);
            await foreach (var ev in basicRead.VerifyAsync(path, options, ct)) events.Add(ev);

            readPerformed = true;
            readPhase = "pre";
            ioFailed = events.Any(e => e.Kind is DefectKind.IoReadError or DefectKind.AccessDenied);
            coverage = "fullread";
            return Build(path, utc, options, ran, events, coverage, readPhase, readPerformed, ioFailed, null);
        }

        // Audit mode: preflight read if read=always
        if (options.ReadMode == ReadMode.Always && basicRead is not null)
        {
            ran.Add(basicRead.Name);
            await foreach (var ev in basicRead.VerifyAsync(path, options, ct)) events.Add(ev);

            readPerformed = true;
            readPhase = "pre";
            ioFailed = events.Any(e => e.Kind is DefectKind.IoReadError or DefectKind.AccessDenied);
            coverage = "fullread";

            if (ioFailed && !options.PluginsAfterReadError)
            {
                events.Add(new DefectEvent(Severity.Info, DefectKind.ExternalToolFailed, "core.skipped_due_to_io_error",
                    "Skipped semantic plugins due to I/O error (use --plugins-after-read-error to continue).", Tool: "core"));
                return Build(path, utc, options, ran, events, coverage, readPhase, readPerformed, ioFailed, false);
            }

            if (ioFailed && options.PluginsAfterReadError)
                pluginsAfter = true;
        }

        // Run semantic plugins
        foreach (var v in applicable)
        {
            ran.Add(v.Name);
            try
            {
                await foreach (var ev in v.VerifyAsync(path, options, ct))
                {
                    events.Add(ev);
                    if (events.Count >= options.MaxEventsPerFile) break;
                }
            }
            catch (Exception ex)
            {
                events.Add(new DefectEvent(Severity.Fail, DefectKind.ExternalToolFailed, "core.verifier_threw", $"{v.Name} threw: {ex.Message}", Tool: v.Name));
            }
        }

        if (!applicable.Any())
        {
            events.Add(new DefectEvent(Severity.Info, DefectKind.NoVerifierMatched, "core.no_verifier_matched",
                "No verifier matched this file type.", Tool: "core"));
        }

        // Post-read policies
        bool postRead = options.ReadMode switch
        {
            ReadMode.Unmatched => !applicable.Any(),
            ReadMode.OnFail => events.Any(e => e.Severity == Severity.Fail),
            _ => false
        };

        if (postRead && basicRead is not null)
        {
            ran.Add(basicRead.Name);
            await foreach (var ev in basicRead.VerifyAsync(path, options, ct)) events.Add(ev);

            readPerformed = true;
            readPhase = "post";
            ioFailed = ioFailed || events.Any(e => e.Kind is DefectKind.IoReadError or DefectKind.AccessDenied);
            coverage = "fullread";
        }
        else if (coverage != "fullread")
        {
            coverage = applicable.Any() ? "semantic" : "none";
        }

        return Build(path, utc, options, ran, events, coverage, readPhase, readPerformed, ioFailed, pluginsAfter);
    }

    private static FileReport Build(
        string path,
        DateTime utc,
        ScanOptions options,
        List<string> ran,
        List<DefectEvent> events,
        string coverage,
        string readPhase,
        bool readPerformed,
        bool ioFailed,
        bool? pluginsAfter)
    {
        // SKIP means nothing meaningfully verified the file: no verifier ran, or the only
        // outcome was "no verifier matched" AND no full read happened. A successful BasicRead
        // (coverage=fullread) is real verification even when no semantic plugin matched.
        bool nothingVerified =
            !ran.Any() ||
            (coverage != "fullread" && events.Count > 0 && events.All(e => e.Kind == DefectKind.NoVerifierMatched));

        FileStatus status =
            events.Any(e => e.Severity == Severity.Fail) ? FileStatus.Fail :
            events.Any(e => e.Severity == Severity.Warn) ? FileStatus.Warn :
            nothingVerified ? FileStatus.Skip :
            FileStatus.Ok;

        return new FileReport(
            File: path,
            Utc: utc,
            Mode: options.Mode,
            Status: status,
            VerifiersRun: ran,
            Events: events,
            Coverage: coverage,
            ReadMode: options.Mode == ScanMode.Audit ? FormatReadMode(options.ReadMode) : null,
            ReadPhase: readPhase,
            ReadPerformed: readPerformed,
            IoFailed: ioFailed,
            PluginsRanAfterIoError: pluginsAfter
        );
    }

    // Contract read_mode tokens: never|unmatched|on-fail|always (hyphenated).
    private static string FormatReadMode(ReadMode mode) => mode switch
    {
        ReadMode.Never => "never",
        ReadMode.Unmatched => "unmatched",
        ReadMode.OnFail => "on-fail",
        ReadMode.Always => "always",
        _ => mode.ToString().ToLowerInvariant()
    };

    private static IEnumerable<string> ExpandInputs(IEnumerable<string> inputs)
    {
        foreach (var p in inputs)
        {
            if (File.Exists(p))
            {
                yield return Path.GetFullPath(p);
                continue;
            }

            if (Directory.Exists(p))
            {
                foreach (var f in Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                    yield return f;
            }
        }
    }
}
