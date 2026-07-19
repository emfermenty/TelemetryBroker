using MetricService.Commands;
using MetricService.Logging;
using MetricService.Services;
using MetricService.Storage;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Timescale")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Timescale configuration");

builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<MetricStorage>();
builder.Services.AddSingleton<CommandQueue>();

builder.Services.AddHttpClient<LokiClient>(client =>
{
    var lokiBaseUrl = builder.Configuration["Loki:BaseUrl"]
        ?? throw new InvalidOperationException("Missing Loki:BaseUrl configuration");
    client.BaseAddress = new Uri(lokiBaseUrl);
});

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ModuleMetricsGrpcService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();