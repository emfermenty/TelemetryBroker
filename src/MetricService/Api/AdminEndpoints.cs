using Google.Protobuf.WellKnownTypes;
using MetricService.Commands;
using MetricService.Storage;
using ModuleTelemetry.V1;

namespace MetricService.Api;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/modules", async (MetricStorage storage, CancellationToken ct) =>
        {
            var modules = await storage.GetModulesAsync(ct);
            return Results.Ok(modules);
        });

        app.MapPost("/api/modules/{moduleId}/commands/send-logs", (string moduleId, SendLogsCommandRequest request, CommandQueue queue) =>
        {
            var command = new Command
            {
                Id = Guid.NewGuid().ToString(),
                SendLogs = new SendLogsCommand
                {
                    Since = Timestamp.FromDateTimeOffset(request.Since),
                    Until = Timestamp.FromDateTimeOffset(request.Until),
                },
            };

            queue.Enqueue(moduleId, command);

            return Results.Accepted();
        });

        app.MapGet("/api/modules/{moduleId}/logs/export", async (string moduleId, DateTime since, DateTime until, LogStorage logStorage, CancellationToken ct) =>
        {
        var logs = await logStorage.QueryLogsAsync(moduleId, since, until, ct);
        var fileName = $"{moduleId}_{since:yyyyMMddHHmmss}_{until:yyyyMMddHHmmss}.json";

        return Results.File(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(logs),
            contentType: "application/json",
            fileDownloadName: fileName);
        });
    }
}

public sealed record SendLogsCommandRequest(DateTimeOffset Since, DateTimeOffset Until);
