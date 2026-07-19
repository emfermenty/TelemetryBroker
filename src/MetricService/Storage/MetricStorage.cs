using Dapper;
using Npgsql;

namespace MetricService.Storage;

public class MetricStorage
{
    private readonly NpgsqlDataSource _dataSource;

    public MetricStorage(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }
    public async Task UpsertModuleAsync(string moduleId, string kind, CancellationToken ct)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO modules (id, kind)
            VALUES (@Id, @Kind)
            ON CONFLICT (id) DO UPDATE SET last_seen = now()
            """,
            new { Id = moduleId, Kind = kind },
            cancellationToken: ct));
    }
    public async Task WriteMetricPointsAsync(string moduleId, IReadOnlyList<MetricPoint> points, CancellationToken ct)
    {
        if (points.Count == 0)
        {
            return;
        }

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await using var writer = await connection.BeginBinaryImportAsync(
            "COPY metric_points (time, module_id, metric_name, attributes, value) FROM STDIN (FORMAT BINARY)",
            ct);

        foreach (var point in points)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(point.Time, NpgsqlTypes.NpgsqlDbType.TimestampTz, ct);
            await writer.WriteAsync(moduleId, NpgsqlTypes.NpgsqlDbType.Text, ct);
            await writer.WriteAsync(point.MetricName, NpgsqlTypes.NpgsqlDbType.Text, ct);
            await writer.WriteAsync(point.AttributesJson, NpgsqlTypes.NpgsqlDbType.Jsonb, ct);
            await writer.WriteAsync(point.Value, NpgsqlTypes.NpgsqlDbType.Double, ct);
        }

        await writer.CompleteAsync(ct);
    }
}
