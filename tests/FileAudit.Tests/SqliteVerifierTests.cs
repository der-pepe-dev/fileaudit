using FileAudit.Abstractions;
using FileAudit.Plugins.SQLite;
using Microsoft.Data.Sqlite;

namespace FileAudit.Tests;

public class SqliteVerifierTests
{
    private static void CreateDb(string path)
    {
        var csb = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate };
        using var conn = new SqliteConnection(csb.ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY, v TEXT); INSERT INTO t(v) VALUES('x'),('y');";
        cmd.ExecuteNonQuery();
        SqliteConnection.ClearAllPools(); // release the file handle so the verifier can reopen
    }

    [Test]
    public async Task CleanDb_NoEvents()
    {
        using var p = new TempPath(".db");
        CreateDb(p.Path);

        var events = await TestEngine.CollectAsync(new SqliteVerifier(), p.Path);
        await Assert.That(events).IsEmpty();
    }

    [Test]
    public async Task GarbageDb_OpenFailed()
    {
        using var p = new TempPath(".db");
        File.WriteAllBytes(p.Path, System.Text.Encoding.ASCII.GetBytes("this is not a sqlite database at all"));

        var events = await TestEngine.CollectAsync(new SqliteVerifier(), p.Path);

        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Severity).IsEqualTo(Severity.Fail);
        await Assert.That(events[0].Code).IsEqualTo("db.sqlite.open_failed");
    }

    [Test]
    public async Task CorruptedDb_Fails()
    {
        using var p = new TempPath(".db");
        CreateDb(p.Path);

        // Flip a run of bytes in the second half of the file (well past the 100-byte file
        // header) so the DB still opens but a data page is damaged -> integrity_check fails
        // (or open fails). Size-relative so it works regardless of page/file size.
        byte[] bytes = File.ReadAllBytes(p.Path);
        int start = Math.Max(200, bytes.Length / 2);
        for (int i = start; i < start + 256 && i < bytes.Length; i++)
            bytes[i] ^= 0xFF;
        File.WriteAllBytes(p.Path, bytes);

        var events = await TestEngine.CollectAsync(new SqliteVerifier(), p.Path);

        await Assert.That(events.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(events[0].Severity).IsEqualTo(Severity.Fail);
        await Assert.That(new[] { "db.sqlite.integrity_failed", "db.sqlite.open_failed" })
            .Contains(events[0].Code);
    }
}
