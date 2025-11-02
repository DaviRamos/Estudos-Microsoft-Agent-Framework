using ModelContextProtocol.Client;

namespace AIWebChatApp_HFMcp.Services;

public class HuggingFaceMcpClientFactory
{
    private readonly string _hfAccessToken;
    private McpClient? _client;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    public HuggingFaceMcpClientFactory(IConfiguration configuration)
    {
        _hfAccessToken = configuration["HF_ACCESS_TOKEN"] 
            ?? throw new InvalidOperationException("Missing configuration: HF_ACCESS_TOKEN");
    }

    public async Task<McpClient> GetClientAsync()
    {
        if (_client != null)
        {
            return _client;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_client != null)
            {
                return _client;
            }

            var hfHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {_hfAccessToken}" }
            };

            var clientTransport = new HttpClientTransport(
                new()
                {
                    Name = "HF Server",
                    Endpoint = new Uri("https://huggingface.co/mcp"),
                    AdditionalHeaders = hfHeaders
                });

            _client = await McpClient.CreateAsync(clientTransport);
            return _client;
        }
        finally
        {
            _initializationLock.Release();
        }
    }
}
