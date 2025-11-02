using AIWebChat_HF_Ollama.Web.Components;
using AIWebChat_HF_Ollama.Web.Services;
using AIWebChat_HF_Ollama.Web.Services.Ingestion;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.AddOllamaApiClient("chat")
    .AddChatClient()
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
builder.AddOllamaApiClient("embeddings")
    .AddEmbeddingGenerator();

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-aiwebchat-hf-ollama-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-aiwebchat-hf-ollama-documents", vectorStoreConnectionString);
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
// Applies robust HTTP resilience settings for all HttpClients in the Web project,
// not across the entire solution. It's aimed at supporting Ollama scenarios due
// to its self-hosted nature and potentially slow responses.
// Remove this if you want to use the global or a different HTTP resilience policy instead.
builder.Services.AddOllamaResilienceHandler();

// add mcp client to connect to Hugging Face MCP Server from the GetHuggingFaceMcpClient method
// register the tools as a singleton so that they can be reused across requests
builder.Services.AddKeyedSingleton<McpClient>("HuggingFaceMCP", (sp, _) =>
{
    // read the hfAccessToken from the configuration
    var hfAccessToken = builder.Configuration.GetConnectionString("huggingface");

    // create MCP Client using Hugging Face endpoint
    var hfHeaders = new Dictionary<string, string>
{
    { "Authorization", $"Bearer {hfAccessToken}" }
};
    var clientTransport = new HttpClientTransport(
        new()
        {
            Name = "HF Server",
            Endpoint = new Uri("https://huggingface.co/mcp"),
            AdditionalHeaders = hfHeaders
        });
    var client = McpClient.CreateAsync(clientTransport).GetAwaiter().GetResult();
    return client;
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
