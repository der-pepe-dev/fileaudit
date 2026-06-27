using FileAudit.Abstractions;
using FileAudit.Core.Engine;
using FileAudit.Plugins.BasicRead;
using FileAudit.Plugins.FFmpeg;
using FileAudit.Plugins.Image;
using FileAudit.Plugins.SQLite;
using FileAudit.Plugins.Zip;

static int Usage()
{
    Console.WriteLine("FileAudit (v0.1)");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  fileaudit scan <path...> [--out <report.jsonl>] [--mode audit|read] [--read never|unmatched|on-fail|always] [--plugins-after-read-error] [--tools <dir>]");
    return 10;
}

if (args.Length == 0 || args[0] is "-h" or "--help")
    return Usage();

if (!string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
    return Usage();

var inputs = new List<string>();
string outPath = "fileaudit-report.jsonl";
ScanMode mode = ScanMode.Audit;
ReadMode readMode = ReadMode.Never;
bool pluginsAfterReadError = false;
string? tools = null;

for (int i = 1; i < args.Length; i++)
{
    string a = args[i];

    if (a == "--out" && i + 1 < args.Length) { outPath = args[++i]; continue; }
    if (a == "--mode" && i + 1 < args.Length)
    {
        string v = args[++i];
        mode = v.Equals("read", StringComparison.OrdinalIgnoreCase) ? ScanMode.Read : ScanMode.Audit;
        continue;
    }
    if (a == "--read" && i + 1 < args.Length)
    {
        string v = args[++i].ToLowerInvariant();
        readMode = v switch
        {
            "never" => ReadMode.Never,
            "unmatched" => ReadMode.Unmatched,
            "on-fail" => ReadMode.OnFail,
            "always" => ReadMode.Always,
            _ => readMode
        };
        continue;
    }
    if (a == "--plugins-after-read-error") { pluginsAfterReadError = true; continue; }
    if (a == "--tools" && i + 1 < args.Length) { tools = args[++i]; continue; }

    if (a.StartsWith("--", StringComparison.Ordinal))
        return Usage();

    inputs.Add(a);
}

if (inputs.Count == 0)
{
    Console.Error.WriteLine("No paths provided.");
    return 10;
}

// Validate inputs before opening the report file, so a typo'd path never clobbers an
// existing report at --out with an empty one.
var missingInputs = AuditEngine.FindMissingInputs(inputs);
if (missingInputs.Count > 0)
{
    foreach (var p in missingInputs)
        Console.Error.WriteLine($"Input path not found: {p}");
    return 10;
}

var options = new ScanOptions(
    Mode: mode,
    ReadMode: readMode,
    PluginsAfterReadError: pluginsAfterReadError,
    ReadBufferBytes: 1024 * 1024,
    MaxEventsPerFile: 200,
    ToolsPath: tools
);

var verifiers = new IVerifier[]
{
    new BasicReadVerifier(),
    new ImageVerifier(),
    new ZipVerifier(),
    new SqliteVerifier(),
    new FFmpegVerifier(),
};

var engine = new AuditEngine(verifiers);

try
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    await using var reporter = new JsonlReporter(outPath);
    int code = await engine.ScanAsync(inputs, options, reporter, cts.Token);

    // Contract exit codes:
    // 0 = OK/SKIP only
    // 2 = WARN present
    // 3 = FAIL present
    // 10 = run-level error (handled by catch)
    return code;
}
catch (InputPathNotFoundException ex)
{
    foreach (var p in ex.Paths)
        Console.Error.WriteLine($"Input path not found: {p}");
    return 10;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.ToString());
    return 10;
}
