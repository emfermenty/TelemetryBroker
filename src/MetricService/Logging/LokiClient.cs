using System.Text;
using System.Text.Json;
using MetricService.Logging;

namespace MetricService.Logging;

public readonly record struct LokiEntry(long TimeUnixNano, string Level, string Line, IReadOnlyDictionary<string, string> Labels);

public class LokiClient
{
    private readonly HttpClient _httpClient;
    public LokiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task PushAsync(string moduleId, IReadOnlyList<LokiEntry> entries, CancellationToken ct)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var streams = entries
            .GroupBy(e => BuildLabelKey(moduleId, e))
            .Select(group =>
            {
                var first = group.First();
                var labels = new Dictionary<string, string>(first.Labels)
                {
                    ["module_id"] = moduleId,
                    ["level"] = first.Level,
                };

                return new
                {
                    stream = labels,
                    values = group.Select(e => new[] { e.TimeUnixNano.ToString(), e.Line }).ToArray(),
                };
            });
            
        var payload = new { streams };
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("/loki/api/v1/push", content, ct);
        response.EnsureSuccessStatusCode();
    }
    private static string BuildLabelKey(string moduleId, LokiEntry entry)
    {
        var labelParts = entry.Labels
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}");

        return string.Join('|', new[] { $"module_id={moduleId}", $"level={entry.Level}" }.Concat(labelParts));
    }
}
