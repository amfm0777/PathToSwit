using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using TelecomOps.Core;

namespace TelecomOps.Infrastructure;

public class InfluxDbMetricsPublisher : IMetricsPublisher
{
    private readonly InfluxDBClient _client;
    private readonly string _org;
    private readonly string _bucket;

    public InfluxDbMetricsPublisher(string url, string token, string org, string bucket)
    {
        var options = new InfluxDBClientOptions(url)
        {
            Token = token
        };
        _client = new InfluxDBClient(options);
        _org = org;
        _bucket = bucket;
    }

    public async Task PublishAsync(TelemetryEvent telemetryEvent)
    {
        var point = PointData.Measurement("telemetry")
            .Tag("node_id", telemetryEvent.NodeId.ToString())
            .Tag("status", telemetryEvent.Status.ToString())
            .Field("latency_ms", telemetryEvent.LatencyMs)
            .Field("throughput_mbps", telemetryEvent.ThroughputMbps)
            .Timestamp(telemetryEvent.Timestamp, WritePrecision.Ms);

        if (!string.IsNullOrEmpty(telemetryEvent.AdditionalData))
        {
            point = point.Field("additional_data", telemetryEvent.AdditionalData);
        }

        var writeApi = _client.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _org);
    }
}