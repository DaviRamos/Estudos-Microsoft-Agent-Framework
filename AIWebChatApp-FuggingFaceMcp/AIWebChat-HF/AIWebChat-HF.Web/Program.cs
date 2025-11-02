using AIWebChat_HF.Web.Components;
using AIWebChat_HF.Web.Services;
using AIWebChat_HF.Web.Services.Ingestion;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var openai = builder.AddAzureOpenAIClient("openai");
openai.AddChatClient("gpt-4o-mini")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-aiwebchat-hf-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-aiwebchat-hf-documents", vectorStoreConnectionString);
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

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
