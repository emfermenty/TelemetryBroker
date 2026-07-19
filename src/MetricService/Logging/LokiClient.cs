using System.Text;
using System.Text.Json;

namespace MetricService.Logging;

public readonly record struct LokiEntry(long TimeUnixNano, string Level, string Line);

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
            .GroupBy(e => e.Level)
            .Select(group => new
            {
                stream = new Dictionary<string, string>
                {
                    ["module_id"] = moduleId,
                    ["level"] = group.Key,
                },
                values = group
                    .Select(e => new[] { e.TimeUnixNano.ToString(), e.Line })
                    .ToArray(),
            });

        var payload = new { streams };
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("/loki/api/v1/push", content, ct);
        response.EnsureSuccessStatusCode();
    }
}
