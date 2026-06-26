namespace FileAudit.Checks;

public sealed record CheckFinding(string Code, string Message, string? Location = null);
