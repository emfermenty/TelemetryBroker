namespace MetricService.Storage;

public sealed record ModuleInfo(string Id, string Kind, DateTime FirstSeen, DateTime LastSeen);

