using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;

namespace MetricService.Storage;

public sealed record LogLine(DateTime Time, string Level, string Line, IReadOnlyDictionary<string, string> Labels);

public class LogStorage
{
    private readonly string _connectionString;

    public LogStorage(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task WriteLogLinesAsync(string moduleId, IReadOnlyList<LogLine> lines, CancellationToken ct)
    {
        if (lines.Count == 0)
        {
            return;
        }

        using var connection = new ClickHouseConnection(_connectionString);

        using var bulkCopy = new ClickHouseBulkCopy(connection)
        {
            DestinationTableName = "logs",
            ColumnNames = new[] { "time", "module_id", "level", "line", "labels" },
        };

        await bulkCopy.InitAsync();

        var rows = lines.Select(l => new object[]
        {
            l.Time,
            moduleId,
            l.Level,
            l.Line,
            new Dictionary<string, string>(l.Labels),
        });

        await bulkCopy.WriteToServerAsync(rows, ct);
    }
}