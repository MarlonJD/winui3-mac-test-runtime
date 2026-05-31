using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinUI3.MacRuntime;

public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
