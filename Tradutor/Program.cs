using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

IChatClient chatClient =
    new ChatClient(
        "gpt-4o-mini",
        new ApiKeyCredential("YOUR_GITHUB_API_KEY"),
        new OpenAIClientOptions
        {
            Endpoint = new Uri("https://models.github.ai/inference")
        })
    .AsIChatClient();

// French Translator Agent
AIAgent frenchAgent = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions
    {
        Name = "FrenchAgent",
        Instructions = "You are a translation assistant that translates the provided text to French."
    });

// Spanish Translator Agent
AIAgent spanishAgent = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions
    {
        Name = "SpanishAgent",
        Instructions = "You are a translation assistant that translates the provided text to Spanish."
    });
    
// Quality Reviewer Agent
string qualityReviewerAgentInstructions = """
You are a multilingual translation quality reviewer.
Check the translations for grammar accuracy, tone consistency, and cultural fit
compared to the original English text.

Give a brief summary with a quality rating (Excellent / Good / Needs Review).

Example output:
Quality: Excellent
Feedback: Accurate translation, friendly tone preserved, minor punctuation tweaks only.
""";

AIAgent qualityReviewerAgent = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions
    {
        Name = "QualityReviewerAgent",
        Instructions = qualityReviewerAgentInstructions
    });
    
// Summary Agent
string summaryAgentInstructions = """
You are a localization summary assistant.
Summarize the translation results below.
For each language, list:
- Translation quality
- Tone feedback
- Any corrections made

Then, provide an overall summary in 3–5 lines.

Example output:
=== Localization Summary ===
French: Excellent (minor punctuation fixes)
Spanish: Good (tone consistent)
All translations reviewed successfully.
""";

AIAgent summaryAgent = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions
    {
        Name = "SummaryAgent",
        Instructions = summaryAgentInstructions
    });
    
AIAgent workflowAgent = await AgentWorkflowBuilder
    .BuildSequential(frenchAgent, spanishAgent, qualityReviewerAgent, summaryAgent)
    .AsAgentAsync();

Console.Write("\nYou: ");
string userInput = Console.ReadLine() ?? string.Empty;

AgentRunResponse response = await workflowAgent.RunAsync(userInput);

Console.WriteLine();

foreach (var message in response.Messages)
{
    Console.WriteLine($"{message.AuthorName}: ");
   
    Console.WriteLine(message.Text);
    Console.WriteLine();
}