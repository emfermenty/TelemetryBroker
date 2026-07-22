using MetricService.Api;
using MetricService.Commands;
using MetricService.Services;
using MetricService.Storage;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
    options.ListenAnyIP(8082, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
});

var connectionString = builder.Configuration.GetConnectionString("Timescale")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Timescale configuration");
var clickHouseConnectionString = builder.Configuration.GetConnectionString("ClickHouse")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:ClickHouse configuration");

builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<MetricStorage>();
builder.Services.AddSingleton<CommandQueue>();
builder.Services.AddSingleton(new LogStorage(clickHouseConnectionString));

builder.Services.AddGrpc();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGrpcService<ModuleMetricsGrpcService>();
app.MapAdminEndpoints();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();