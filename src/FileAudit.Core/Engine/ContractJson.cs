using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileAudit.Abstractions;

namespace FileAudit.Core.Engine;

/// <summary>
/// JSON serialization configured to match the stable JSONL contract (docs/contract.md):
/// snake_case keys, string enums with contract-specified casing, null optionals omitted.
/// </summary>
internal static class ContractJson
{
    public static readonly JsonSerializerOptions Options = Build();

    private static JsonSerializerOptions Build()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Report is a file artifact, not HTML; keep messages literal (no \u00XX escaping).
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        // Per-type converters (contract casing) take precedence over the generic fallback.
        o.Converters.Add(new SeverityConverter());
        o.Converters.Add(new FileStatusConverter());
        o.Converters.Add(new ScanModeConverter());
        // DefectKind: contract uses the PascalCase member name (e.g. IoReadError).
        o.Converters.Add(new JsonStringEnumConverter());

        return o;
    }

    private sealed class SeverityConverter : JsonConverter<Severity>
    {
        public override Severity Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => reader.GetString() switch
            {
                "INFO" => Severity.Info,
                "WARN" => Severity.Warn,
                "FAIL" => Severity.Fail,
                var s => throw new JsonException($"Unknown severity: {s}")
            };

        public override void Write(Utf8JsonWriter w, Severity v, JsonSerializerOptions o)
            => w.WriteStringValue(v switch
            {
                Severity.Info => "INFO",
                Severity.Warn => "WARN",
                Severity.Fail => "FAIL",
                _ => v.ToString()
            });
    }

    private sealed class FileStatusConverter : JsonConverter<FileStatus>
    {
        public override FileStatus Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => reader.GetString() switch
            {
                "OK" => FileStatus.Ok,
                "WARN" => FileStatus.Warn,
                "FAIL" => FileStatus.Fail,
                "SKIP" => FileStatus.Skip,
                var s => throw new JsonException($"Unknown status: {s}")
            };

        public override void Write(Utf8JsonWriter w, FileStatus v, JsonSerializerOptions o)
            => w.WriteStringValue(v switch
            {
                FileStatus.Ok => "OK",
                FileStatus.Warn => "WARN",
                FileStatus.Fail => "FAIL",
                FileStatus.Skip => "SKIP",
                _ => v.ToString()
            });
    }

    private sealed class ScanModeConverter : JsonConverter<ScanMode>
    {
        public override ScanMode Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => reader.GetString() switch
            {
                "audit" => ScanMode.Audit,
                "read" => ScanMode.Read,
                var s => throw new JsonException($"Unknown mode: {s}")
            };

        public override void Write(Utf8JsonWriter w, ScanMode v, JsonSerializerOptions o)
            => w.WriteStringValue(v switch
            {
                ScanMode.Audit => "audit",
                ScanMode.Read => "read",
                _ => v.ToString().ToLowerInvariant()
            });
    }
}
