using FileAudit.Abstractions;
using Microsoft.Data.Sqlite;

namespace FileAudit.Plugins.SQLite;

public sealed class SqliteVerifier : IVerifier
{
    public string Name => "sqlite";
    public int Order => 20;

    private static readonly HashSet<string> Exts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".sqlite", ".sqlite3", ".db"
    };

    public bool CanVerify(string path, ReadOnlySpan<byte> header)
    {
        if (header.Length >= 16)
        {
            ReadOnlySpan<byte> magic = "SQLite format 3\0"u8;
            if (header.Slice(0, 16).SequenceEqual(magic))
                return true;
        }

        return Exts.Contains(Path.GetExtension(path));
    }

    public async IAsyncEnumerable<DefectEvent> VerifyAsync(string path, ScanOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        try
        {
            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly
            };

            using var conn = new SqliteConnection(csb.ConnectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = await cmd.ExecuteScalarAsync(ct);

            string s = Convert.ToString(result) ?? "";
            if (!string.Equals(s.Trim(), "ok", StringComparison.OrdinalIgnoreCase))
            {
                yield return new DefectEvent(
                    Severity.Fail,
                    DefectKind.IntegrityCheckFailed,
                    "db.sqlite.integrity_failed",
                    $"SQLite integrity_check returned: {s}",
                    Location: "pragma=integrity_check",
                    Tool: Name
                );
            }
        }
        catch (Exception ex)
        {
            yield return new DefectEvent(
                Severity.Fail,
                DefectKind.ParseError,
                "db.sqlite.open_failed",
                ex.Message,
                Tool: Name
            );
        }
    }
}
