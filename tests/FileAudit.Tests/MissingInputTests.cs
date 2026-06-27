using FileAudit.Core.Engine;

namespace FileAudit.Tests;

public class MissingInputTests
{
    private static string NonexistentPath()
        => Path.Combine(Path.GetTempPath(), $"fa-missing-{Guid.NewGuid():N}");

    [Test]
    public async Task MissingPath_Throws()
    {
        string missing = NonexistentPath();
        await Assert.That(async () =>
                await TestEngine.Run(new[] { missing }, TestEngine.Options()))
            .ThrowsExactly<InputPathNotFoundException>();
    }

    [Test]
    public async Task MixedValidAndMissing_Throws_AndScansNothing()
    {
        using var good = new TempFile();
        string missing = NonexistentPath();
        var semantic = new FakeVerifier("zip", 10, canVerify: true);

        var ex = await Assert.That(async () =>
                await TestEngine.Run(new[] { good.Path, missing }, TestEngine.Options(), semantic))
            .ThrowsExactly<InputPathNotFoundException>();

        await Assert.That(ex!.Paths).Contains(missing);
    }
}
