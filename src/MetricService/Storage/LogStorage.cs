using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using ClickHouse.Client.Utility;

namespace MetricService.Storage;

public sealed record LogLine(DateTime Time, string Level, string Line, IReadOnlyDictionary<string, string> Labels);
public sealed record LogRecord(DateTime Time, string ModuleId, string Level, string Line, IReadOnlyDictionary<string, string> Labels);

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
    public async Task<List<LogRecord>> QueryLogsAsync(string moduleId, DateTime since, DateTime until, CancellationToken ct)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(ct);

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT time, module_id, level, line, labels
            FROM logs
            WHERE module_id = {moduleId:String} AND time BETWEEN {since:DateTime64(9)} AND {until:DateTime64(9)}
            ORDER BY time
            """;
        command.AddParameter("moduleId", moduleId);
        command.AddParameter("since", since);
        command.AddParameter("until", until);

        var records = new List<LogRecord>();
        using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            records.Add(new LogRecord(
                reader.GetDateTime(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                (IReadOnlyDictionary<string, string>)reader.GetValue(4)));
        }

        return records;
    }
}