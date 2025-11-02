var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume();
//var chat = ollama.AddModel("chat", "llama3.2");
var chat = ollama.AddModel("chat", "mistral-nemo");
var embeddings = ollama.AddModel("embeddings", "all-minilm");

// add huggingface connection string
var huggingface = builder.AddConnectionString("huggingface");


var webApp = builder.AddProject<Projects.AIWebChat_HF_Ollama_Web>("aichatweb-app");
webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WaitFor(chat)
    .WaitFor(embeddings)
    .WithReference(huggingface);

builder.Build().Run();
