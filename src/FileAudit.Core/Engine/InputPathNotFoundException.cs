namespace FileAudit.Core.Engine;

/// <summary>
/// Thrown when one or more provided input paths are neither a file nor a directory.
/// A run-level error (CLI exit code 10); no report line is emitted for the missing paths.
/// </summary>
public sealed class InputPathNotFoundException : Exception
{
    public IReadOnlyList<string> Paths { get; }

    public InputPathNotFoundException(IReadOnlyList<string> paths)
        : base($"Input path(s) not found: {string.Join(", ", paths)}")
        => Paths = paths;
}
