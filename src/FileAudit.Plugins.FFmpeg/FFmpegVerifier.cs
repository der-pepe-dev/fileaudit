using System.Diagnostics;
using FileAudit.Abstractions;

namespace FileAudit.Plugins.FFmpeg;

public sealed class FFmpegVerifier : IVerifier
{
    public string Name => "ffmpeg";
    public int Order => 30;

    private static readonly HashSet<string> Exts = new(StringComparer.OrdinalIgnoreCase)
    {
        // video
        ".mp4",".m4v",".mov",".mkv",".webm",".avi",".mpg",".mpeg",".m2ts",".ts",".mts",".flv",".wmv",".asf",".3gp",".3g2",
        // audio
        ".mp3",".m4a",".aac",".flac",".wav",".ogg",".opus",".wma"
    };

    public bool CanVerify(string path, ReadOnlySpan<byte> header)
        => Exts.Contains(Path.GetExtension(path));

    public async IAsyncEnumerable<DefectEvent> VerifyAsync(string path, ScanOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        string? ffmpeg = ResolveTool("ffmpeg", options.ToolsPath);
        if (ffmpeg is null)
        {
            yield return new DefectEvent(Severity.Info, DefectKind.ExternalToolMissing, "media.ffmpeg.missing",
                "ffmpeg not found (set --tools or add to PATH).", Tool: Name);
            yield break;
        }

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = $"-hide_banner -v warning -i "{path}" -f null -",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var p = Process.Start(psi);
        if (p is null)
        {
            yield return new DefectEvent(Severity.Fail, DefectKind.ExternalToolFailed, "media.ffmpeg.start_failed",
                "Failed to start ffmpeg process.", Tool: Name);
            yield break;
        }

        string stderr = await p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync(ct);

        bool hasConceal = ContainsAny(stderr, new[] { "conceal", "concealing" });
        bool hasCorrupt = ContainsAny(stderr, new[] { "corrupt" });
        bool hasHardFail = p.ExitCode != 0
            || ContainsAny(stderr, new[] { "Invalid data found when processing input", "moov atom not found", "Conversion failed", "Error opening input" });

        if (hasHardFail)
        {
            yield return new DefectEvent(Severity.Fail, DefectKind.DecodeError, "media.decode.aborted",
                Summarize(stderr, p.ExitCode), Tool: Name);
            yield break;
        }

        if (hasConceal)
        {
            yield return new DefectEvent(Severity.Warn, DefectKind.ConcealmentDetected, "media.decode.concealment",
                "Decoder concealment reported (recoverable defect).", Tool: Name);
        }

        if (hasCorrupt)
        {
            yield return new DefectEvent(Severity.Warn, DefectKind.DecodeWarning, "media.decode.warning",
                "Decoder reported corruption warnings during decode.", Tool: Name);
        }
    }

    private static string? ResolveTool(string name, string? toolsPath)
    {
        string exe = OperatingSystem.IsWindows() ? name + ".exe" : name;

        if (!string.IsNullOrWhiteSpace(toolsPath))
        {
            var candidate = Path.Combine(toolsPath, exe);
            if (File.Exists(candidate)) return candidate;
        }

        // If not in tools, rely on PATH. If missing, Process.Start will fail; we accept that in v0.1.
        return exe;
    }

    private static bool ContainsAny(string s, IEnumerable<string> needles)
    {
        foreach (var n in needles)
            if (s.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    private static string Summarize(string stderr, int exitCode)
    {
        var lines = stderr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string last = lines.Length > 0 ? lines[^1] : "ffmpeg failed";
        return $"ffmpeg exit={exitCode}: {last}";
    }
}
