using System.Text.Json;
using System.Text.Json.Serialization;
using Dsk.Models;

namespace Dsk.Rendering;

/// <summary>
/// JSON output renderer with source-generated serialization for AOT compatibility.
/// </summary>
public static class JsonRenderer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        TypeInfoResolver = DskJsonContext.Default,
    };
    
    /// <summary>
    /// Render mounts as JSON to stdout.
    /// </summary>
    public static void Render(List<Mount> mounts)
    {
        var json = JsonSerializer.Serialize(mounts, DskJsonContext.Default.ListMount);
        Console.WriteLine(json);
    }
}

/// <summary>
/// Source-generated JSON serialization context for AOT compatibility.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Mount))]
[JsonSerializable(typeof(List<Mount>))]
internal partial class DskJsonContext : JsonSerializerContext
{
}

