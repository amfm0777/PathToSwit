using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// ─────────────────────────────────────────────
//  TelecomOps Simulator Agent
//  Usage: dotnet run -- [options]
//    --api      <url>   API base URL  (default: http://localhost:5000)
//    --nodes    <n>     Number of nodes to create (default: 5)
//    --duration <secs>  How long to run before auto-cleanup (default: 60)
//    --interval <secs>  Telemetry publish interval (default: 5)
//    --no-cleanup       Skip cleanup on exit
// ─────────────────────────────────────────────

var cliArgs = Args.Parse(Environment.GetCommandLineArgs()[1..]);

Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║       TelecomOps Simulator Agent  v1.1           ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.WriteLine($"  API        : {cliArgs.ApiUrl}");
Console.WriteLine($"  Nodes      : {cliArgs.NodeCount}");
Console.WriteLine($"  Duration   : {cliArgs.DurationSeconds}s");
Console.WriteLine($"  Interval   : {cliArgs.IntervalSeconds}s");
Console.WriteLine($"  Auto-clean : {!cliArgs.NoCleanup}");
Console.WriteLine();

using var cts = new CancellationTokenSource();
var agent = new SimulatorAgent(cliArgs.ApiUrl);

Console.CancelKeyPress += async (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[!] Interrupt received — shutting down…");
    cts.Cancel();
};

await agent.WaitForApiAsync(cts.Token);

var createdNodes = await agent.CreateNodesAsync(cliArgs.NodeCount, cts.Token);
if (createdNodes.Count == 0)
{
    Console.WriteLine("[!] No nodes were created. Exiting.");
    return 1;
}

await agent.ActivateNodesAsync(createdNodes, cts.Token);

await agent.RunTelemetryLoopAsync(createdNodes, cliArgs.DurationSeconds, cliArgs.IntervalSeconds, cts.Token);

if (!cliArgs.NoCleanup)
    await agent.CleanupAsync(createdNodes);

Console.WriteLine("\n[✓] Simulation complete.");
return 0;

// ════════════════════════════════════════════════════════

sealed class SimulatorAgent(string apiUrl)
{
    private static readonly string[] NodePrefixes = ["GNB", "ENB", "CELL", "SITE", "NODE", "UE"];
    private static readonly string[] FrequencyBands = ["Band3", "Band7", "Band28", "Band78"];

    // Realistic per-band throughput ranges (Mbps)
    private static readonly Dictionary<string, (double Min, double Max)> BandThroughput = new()
    {
        ["Band3"]  = (50,  300),
        ["Band7"]  = (80,  500),
        ["Band28"] = (20,  150),
        ["Band78"] = (200, 950),  // mmWave-like, highest throughput
    };

    private static readonly Random Rng = new();

    private readonly HttpClient _http = new() { BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/") };
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    // ── Phase 1 ──────────────────────────────────────────

    public async Task WaitForApiAsync(CancellationToken ct)
    {
        Console.Write("[~] Waiting for API");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var resp = await _http.GetAsync("", ct);
                if (resp.IsSuccessStatusCode) { Console.WriteLine(" ✓"); return; }
            }
            catch { }
            Console.Write(".");
            await Task.Delay(2000, ct);
        }
    }

    // ── Phase 2 ──────────────────────────────────────────

    public async Task<List<NodeInfo>> CreateNodesAsync(int count, CancellationToken ct)
    {
        Console.WriteLine($"[+] Creating {count} nodes…");
        var nodes = new List<NodeInfo>();

        for (int i = 0; i < count && !ct.IsCancellationRequested; i++)
        {
            var name = $"{NodePrefixes[Rng.Next(NodePrefixes.Length)]}-{Rng.Next(1000, 9999)}";
            var band = FrequencyBands[Rng.Next(FrequencyBands.Length)];

            try
            {
                var resp = await _http.PostAsync(
                    $"nodes?name={Uri.EscapeDataString(name)}&frequencyBand={band}", null, ct);

                if (resp.IsSuccessStatusCode)
                {
                    var dto = await resp.Content.ReadFromJsonAsync<NodeDto>(_json, ct);
                    if (dto is not null)
                    {
                        // Assign a stable base latency per node for realistic variance
                        var baseLatency = 5 + Rng.NextDouble() * 35;
                        nodes.Add(new NodeInfo(dto.Id, dto.Name, band, baseLatency));
                        Console.WriteLine($"    ✓ {name} [{band}] id={dto.Id:D}");
                    }
                }
                else
                {
                    Console.WriteLine($"    ✗ {name} → HTTP {(int)resp.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ✗ {name} → {ex.Message}");
            }

            await Task.Delay(200, ct);
        }

        Console.WriteLine($"[+] {nodes.Count}/{count} nodes created.\n");
        return nodes;
    }

    // ── Phase 3 ──────────────────────────────────────────

    public async Task ActivateNodesAsync(List<NodeInfo> nodes, CancellationToken ct)
    {
        Console.WriteLine("[+] Activating nodes…");
        foreach (var node in nodes)
        {
            try
            {
                var resp = await _http.PutAsync($"nodes/{node.Id}/status?status=Active", null, ct);
                var mark = resp.IsSuccessStatusCode ? "✓" : $"✗ HTTP {(int)resp.StatusCode}";
                Console.WriteLine($"    {mark} {node.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ✗ {node.Name} → {ex.Message}");
            }
        }
        Console.WriteLine();
    }

    // ── Phase 4 ──────────────────────────────────────────

    public async Task RunTelemetryLoopAsync(
        List<NodeInfo> nodes, int durationSeconds, int intervalSeconds, CancellationToken externalCt)
    {
        using var timer = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timer.Token);
        var ct = linked.Token;

        Console.WriteLine($"[~] Generating telemetry every {intervalSeconds}s for {durationSeconds}s.");
        Console.WriteLine("[~] Press Ctrl+C to stop early.\n");

        PrintHeader();

        int tick = 0;
        while (!ct.IsCancellationRequested)
        {
            var results = new List<(string Name, double Latency, double Throughput)>();

            foreach (var node in nodes)
            {
                var (lat, tput) = GenerateTelemetry(node);
                try
                {
                    var req = new { LatencyMs = lat, ThroughputMbps = tput };
                    await _http.PostAsJsonAsync($"nodes/{node.Id}/telemetry", req, _json, ct);
                    results.Add((node.Name, lat, tput));
                }
                catch (OperationCanceledException) { goto done; }
                catch { results.Add((node.Name, lat, tput)); }
            }

            PrintRow(tick * intervalSeconds, results);

            try { await Task.Delay(intervalSeconds * 1000, ct); }
            catch (OperationCanceledException) { break; }
            tick++;
        }

        done:
        Console.WriteLine();
    }

    // ── Phase 5 ──────────────────────────────────────────

    public async Task CleanupAsync(List<NodeInfo> nodes)
    {
        Console.WriteLine($"[–] Cleaning up {nodes.Count} nodes…");
        int deleted = 0;
        foreach (var node in nodes)
        {
            try
            {
                var resp = await _http.DeleteAsync($"nodes/{node.Id}");
                if (resp.IsSuccessStatusCode)
                {
                    deleted++;
                    Console.WriteLine($"    ✓ Deleted {node.Name} ({node.Id})");
                }
                else
                {
                    Console.WriteLine($"    ✗ {node.Name} → HTTP {(int)resp.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ✗ {node.Name} → {ex.Message}");
            }
        }
        Console.WriteLine($"[–] Done: {deleted}/{nodes.Count} nodes removed.");
    }

    // ── Telemetry generation ─────────────────────────────

    // Produces realistic latency + throughput with:
    //   - Per-node stable base latency
    //   - Gaussian-like jitter
    //   - Occasional congestion spikes (10% chance)
    //   - Per-band throughput ceiling
    private static (double Latency, double Throughput) GenerateTelemetry(NodeInfo node)
    {
        // Jitter: sum two uniform samples ≈ triangular distribution
        var jitter = (Rng.NextDouble() + Rng.NextDouble() - 1.0) * 15;

        // 10% chance of a congestion spike
        var spike = Rng.NextDouble() < 0.10 ? Rng.NextDouble() * 50 : 0;

        var latency = Math.Clamp(node.BaseLatencyMs + jitter + spike, 1, 200);

        var (tMin, tMax) = BandThroughput[node.Band];
        // Inverse relationship: higher latency → lower throughput
        var load = latency / 200.0;
        var throughput = Math.Clamp(
            tMax - load * (tMax - tMin) + (Rng.NextDouble() - 0.5) * 40,
            tMin, tMax);

        return (Math.Round(latency, 2), Math.Round(throughput, 2));
    }

    // ── Console output ───────────────────────────────────

    private static void PrintHeader()
    {
        Console.WriteLine($"{"Time",7}  {"Node",-16} {"Band",-8} {"Latency (ms)",13} {"Throughput (Mbps)",18}");
        Console.WriteLine(new string('─', 68));
    }

    private static void PrintRow(int elapsed, List<(string Name, double Latency, double Throughput)> rows)
    {
        bool first = true;
        foreach (var (name, lat, tput) in rows)
        {
            var timeCol = first ? $"{elapsed,5}s" : "       ";
            Console.WriteLine($"{timeCol}  {name,-16} {"",-8} {lat,13:F1} {tput,18:F1}");
            first = false;
        }

        if (rows.Count > 1)
        {
            var avgLat  = rows.Average(r => r.Latency);
            var avgTput = rows.Average(r => r.Throughput);
            Console.WriteLine($"{"       "}  {"── avg ──",-16} {"",8} {avgLat,13:F1} {avgTput,18:F1}");
        }
        Console.WriteLine();
    }
}

// ── Domain types ─────────────────────────────────────────

sealed record NodeInfo(Guid Id, string Name, string Band, double BaseLatencyMs);

sealed record NodeDto(Guid Id, string Name, string FrequencyBand, string Status, DateTime CreatedAt);

// ── Argument parsing ──────────────────────────────────────

sealed class Args
{
    public string ApiUrl { get; init; } = "http://localhost:5000";
    public int NodeCount { get; init; } = 5;
    public int DurationSeconds { get; init; } = 60;
    public int IntervalSeconds { get; init; } = 5;
    public bool NoCleanup { get; init; }

    public static Args Parse(string[] argv)
    {
        var api = "http://localhost:5000";
        var count = 5;
        var dur = 60;
        var interval = 5;
        var noClean = false;

        for (int i = 0; i < argv.Length; i++)
        {
            switch (argv[i].ToLowerInvariant())
            {
                case "--api"      when i + 1 < argv.Length: api      = argv[++i]; break;
                case "--nodes"    when i + 1 < argv.Length: _ = int.TryParse(argv[++i], out count);    break;
                case "--duration" when i + 1 < argv.Length: _ = int.TryParse(argv[++i], out dur);      break;
                case "--interval" when i + 1 < argv.Length: _ = int.TryParse(argv[++i], out interval); break;
                case "--no-cleanup": noClean = true; break;
                case "--help": case "-h": PrintHelp(); Environment.Exit(0); break;
            }
        }

        return new Args { ApiUrl = api, NodeCount = count, DurationSeconds = dur,
                          IntervalSeconds = interval, NoCleanup = noClean };
    }

    static void PrintHelp()
    {
        Console.WriteLine("TelecomOps Simulator Agent\n");
        Console.WriteLine("Usage: dotnet run -- [options]\n");
        Console.WriteLine("  --api      <url>   API base URL        (default: http://localhost:5000)");
        Console.WriteLine("  --nodes    <n>     Nodes to create     (default: 5)");
        Console.WriteLine("  --duration <secs>  Run duration        (default: 60)");
        Console.WriteLine("  --interval <secs>  Telemetry interval  (default: 5)");
        Console.WriteLine("  --no-cleanup       Skip cleanup on exit");
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        Console.WriteLine("  --help, -h         Show this help message");                                                                                                                       }
}
