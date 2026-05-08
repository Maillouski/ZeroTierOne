using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZeroTierTray;

public sealed class ZeroTierClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _token;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private ZeroTierClient(HttpClient http, string token)
    {
        _http = http;
        _token = token;
    }

    public static async Task<ZeroTierClient> CreateAsync()
    {
        var (port, token) = ResolvePortAndToken();
        var http = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
            Timeout = TimeSpan.FromSeconds(5),
        };
        http.DefaultRequestHeaders.Add("X-ZT1-Auth", token);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Quick reachability probe
        try
        {
            using var resp = await http.GetAsync("status");
            resp.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            http.Dispose();
            throw new InvalidOperationException(
                $"Cannot reach ZeroTier service on port {port}. Is the 'ZeroTierOneService' running?", ex);
        }

        return new ZeroTierClient(http, token);
    }

    private static (int port, string token) ResolvePortAndToken()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ZeroTier", "One"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZeroTier", "One"),
        };

        string? portText = null, tokenText = null;
        string? lastTried = null;

        foreach (var dir in candidates)
        {
            lastTried = dir;
            var portFile = Path.Combine(dir, "zerotier-one.port");
            var tokenFile = Path.Combine(dir, "authtoken.secret");
            if (portText == null && File.Exists(portFile))
            {
                try { portText = File.ReadAllText(portFile).Trim(); } catch { }
            }
            if (tokenText == null && File.Exists(tokenFile))
            {
                try { tokenText = File.ReadAllText(tokenFile).Trim(); } catch { }
            }
            if (portText != null && tokenText != null) break;
        }

        if (string.IsNullOrEmpty(portText))
            portText = "9993"; // ZT_DEFAULT_PORT

        if (string.IsNullOrEmpty(tokenText))
            throw new InvalidOperationException(
                $"authtoken.secret not found or unreadable. Run the tray as Administrator, " +
                $"or copy the file from C:\\ProgramData\\ZeroTier\\One\\authtoken.secret " +
                $"to %LOCALAPPDATA%\\ZeroTier\\One\\authtoken.secret. (Looked in: {lastTried})");

        if (!int.TryParse(portText, out var port) || port <= 0 || port > 65535)
            throw new InvalidOperationException($"Invalid port value: '{portText}'");

        return (port, tokenText);
    }

    public async Task<NodeStatus?> GetStatusAsync()
    {
        using var resp = await _http.GetAsync("status");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<NodeStatus>(JsonOpts);
    }

    public async Task<NetworkInfo[]> ListNetworksAsync()
    {
        using var resp = await _http.GetAsync("network");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<NetworkInfo[]>(JsonOpts) ?? Array.Empty<NetworkInfo>();
    }

    public async Task JoinAsync(string networkId)
    {
        // Empty body POST creates / joins the network
        using var resp = await _http.PostAsync($"network/{networkId}",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
    }

    public async Task LeaveAsync(string networkId)
    {
        using var resp = await _http.DeleteAsync($"network/{networkId}");
        resp.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}

public sealed class NodeStatus
{
    public string? Address { get; set; }
    public string? Version { get; set; }
    public bool Online { get; set; }
    [JsonPropertyName("publicIdentity")] public string? PublicIdentity { get; set; }
}

public sealed class NetworkInfo
{
    [JsonPropertyName("nwid")] public string? Nwid { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    [JsonPropertyName("assignedAddresses")] public string[]? AssignedAddresses { get; set; }
}
