var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString("openai");
var huggingface = builder.AddConnectionString("huggingface");

var webApp = builder.AddProject<Projects.AIWebChat_HF_Web>("aichatweb-app");
webApp.WithReference(openai);
webApp.WithReference(huggingface);

builder.Build().Run();
