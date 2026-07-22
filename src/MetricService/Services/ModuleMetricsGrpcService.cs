using System.Globalization;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MetricService.Commands;
using MetricService.Identity.Resolver;
using MetricService.Storage;
using ModuleTelemetry.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using static ModuleTelemetry.V1.ModuleMetricsService;

namespace MetricService.Services;

public class ModuleMetricsGrpcService : ModuleMetricsServiceBase
{
    private readonly MetricStorage _storage;
    private readonly CommandQueue _commandQueue;
    private readonly LogStorage _logStorage;
    private readonly ILogger<ModuleMetricsGrpcService> _logger;

    public ModuleMetricsGrpcService(MetricStorage storage, CommandQueue commandQueue, ILogger<ModuleMetricsGrpcService> logger, LogStorage logStorage)
    {
        _storage = storage;
        _commandQueue = commandQueue;
        _logStorage = logStorage;
        _logger = logger;
    }

    public override async Task<ExportResponse> Export(ExportMetricsServiceRequest request, ServerCallContext context)
    {
        var response = new ExportResponse();

        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            if (!IdentityResolver.TryResolve(resourceMetrics.Resource, out var identity))
            {
                _logger.LogWarning("Rejected metrics batch: no license.name or hwid");
                continue;
            }

            await _storage.UpsertModuleAsync(identity.Value, identity.Kind, context.CancellationToken);

            var points = ExtractPoints(resourceMetrics);
            await _storage.WriteMetricPointsAsync(identity.Value, points, context.CancellationToken);

            foreach (var command in _commandQueue.DequeueAll(identity.Value))
            {
                response.Commands.Add(command);
            }
        }

        return response;
    }

    public override async Task<SendLogsResponse> SendLogs(SendLogsRequest request, ServerCallContext context)
    {
        if (!IdentityResolver.TryResolve(request.Resource, out var identity))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "No license.name or hwid attribute found"));
        }

        var lines = request.Entries
            .Select(e => new LogLine(e.Time.ToDateTime(), e.Level, e.Line, e.Labels.ToDictionary(kv => kv.Key, kv => kv.Value)))
            .ToList();

        await _logStorage.WriteLogLinesAsync(identity.Value, lines, context.CancellationToken);

        return new SendLogsResponse();
    }

    private static long ToUnixNano(Timestamp timestamp) => timestamp.Seconds * 1_000_000_000L + timestamp.Nanos;

    private static List<MetricPoint> ExtractPoints(ResourceMetrics resourceMetrics)
    {
        var points = new List<MetricPoint>();

        foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
        {
            foreach (var metric in scopeMetrics.Metrics)
            {
                switch (metric.DataCase)
                {
                    case Metric.DataOneofCase.Gauge:
                        AddNumberPoints(points, metric.Name, metric.Gauge.DataPoints);
                        break;
                    case Metric.DataOneofCase.Sum:
                        AddNumberPoints(points, metric.Name, metric.Sum.DataPoints);
                        break;
                    case Metric.DataOneofCase.Histogram:
                        AddHistogramPoints(points, metric.Name, metric.Histogram.DataPoints);
                        break;
                }
            }
        }

        return points;
    }

    private static void AddNumberPoints(List<MetricPoint> points, string metricName, IEnumerable<NumberDataPoint> dataPoints)
    {
        foreach (var point in dataPoints)
        {
            var value = point.ValueCase == NumberDataPoint.ValueOneofCase.AsDouble
                ? point.AsDouble
                : point.AsInt;

            var attributes = ToAttributeDict(point.Attributes);
            points.Add(new MetricPoint(ToDateTime(point.TimeUnixNano), metricName, JsonSerializer.Serialize(attributes), value));
        }
    }

    private static void AddHistogramPoints(List<MetricPoint> points, string metricName, IEnumerable<HistogramDataPoint> dataPoints)
    {
        foreach (var point in dataPoints)
        {
            var time = ToDateTime(point.TimeUnixNano);
            var baseAttributes = ToAttributeDict(point.Attributes);

            for (var i = 0; i < point.BucketCounts.Count; i++)
            {
                var bucketAttributes = new Dictionary<string, string>(baseAttributes)
                {
                    ["le"] = i < point.ExplicitBounds.Count
                        ? point.ExplicitBounds[i].ToString(CultureInfo.InvariantCulture)
                        : "+Inf",
                };

                points.Add(new MetricPoint(time, $"{metricName}_bucket", JsonSerializer.Serialize(bucketAttributes), point.BucketCounts[i]));
            }

            points.Add(new MetricPoint(time, $"{metricName}_count", JsonSerializer.Serialize(baseAttributes), point.Count));

            if (point.HasSum)
            {
                points.Add(new MetricPoint(time, $"{metricName}_sum", JsonSerializer.Serialize(baseAttributes), point.Sum));
            }
        }
    }

    private static DateTime ToDateTime(ulong timeUnixNano) => DateTime.UnixEpoch.AddTicks((long)(timeUnixNano / 100));

    private static Dictionary<string, string> ToAttributeDict(IEnumerable<KeyValue> attributes)
    {
        var dict = new Dictionary<string, string>();
        foreach (var kv in attributes)
        {
            if (kv.Value.HasStringValue)
            {
                dict[kv.Key] = kv.Value.StringValue;
            }
        }

        return dict;
    }
}
