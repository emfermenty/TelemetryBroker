namespace MetricService.Storage;

public record MetricPoint(DateTime Time, string MetricName, string AttributesJson, double Value);