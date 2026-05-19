using System.Text.Json;
using System.Text.Json.Nodes;
using Halen.Application.Interfaces;

namespace Halen.Application.Pipeline;

public static class AuditMetadataSerializer
{
    private const int MaxLength = 4096;

    public static string Serialize<T>(T command)
    {
        try
        {
            var json = JsonSerializer.Serialize(command);
            var redactedProperties = typeof(T)
                .GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(AuditRedactAttribute), false).Length > 0)
                .Select(p => p.Name)
                .ToHashSet();

            if (redactedProperties.Count > 0)
            {
                var node = JsonNode.Parse(json);
                if (node is JsonObject obj)
                {
                    foreach (var prop in redactedProperties)
                    {
                        if (obj.ContainsKey(prop))
                            obj[prop] = "[REDACTED]";
                    }
                    json = obj.ToJsonString();
                }
            }

            if (json.Length > MaxLength)
                json = json[..MaxLength];

            return json;
        }
        catch
        {
            return command?.GetType().Name ?? "Unknown";
        }
    }
}
