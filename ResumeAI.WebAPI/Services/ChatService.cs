using LangChain.Chains.StackableChains.Agents;
using LangChain.Databases;
using LangChain.Memory;
using LangChain.Providers;
using LangChain.Retrievers;
using ResumeAI.WebAPI.AgentTools;
using static LangChain.Chains.Chain;


namespace ResumeAI.WebAPI.Services;

public class ChatService
{
    private readonly string _agentTemplate = """
You are a helpful AI that can help User with their questions on the candidate selection and always responds in a requested format.

You have access to the following tools:

{tools}

Previous conversation history with User:

{conversation_history}

Your response MUST follow the the following format:

User: the message from User you must answer
Thought: you should always think about what to do.
Action: the tool name, should be one of [{tool_names}]
Action Input: the input to the tool
Observation: the result of the tool
(this Thought/Action/Action Input/Observation can repeat multiple times)
Thought: I now know the final answer
(no actions before final answer)
Final Answer: the final answer to the original input message. ALWAYS put response to user here.
Always add [END] after final answer.

Only use tools if you need additional information. Otherwise just type "Final Answer: " and provide the final answer.
Your response always MUST end with either Final Answer or Action sections. Never end your response with the Thought section.

Begin!

User: {input}
Thought:{history}
""";

    private readonly string _documentTemplate = @"
Candidate {candidate_name}
{page_content}
";
    
    
    private readonly VectorStoreCollectionProvider _collectionProvider;
    private readonly IEmbeddingModel _embeddingModel;
    private readonly IChatModel _model;
    private readonly IMessageHistoryStore _messageHistoryStore;
    private readonly MessageFormatter _messageFormatter;

    public ChatService(
        VectorStoreCollectionProvider collectionProvider,
        IEmbeddingModel embeddingModel,
        IChatModel model,
        IMessageHistoryStore messageHistoryStore)
    {
        _collectionProvider = collectionProvider;
        _embeddingModel = embeddingModel;
        _model = model;
        _messageHistoryStore = messageHistoryStore;

        _messageFormatter = new MessageFormatter
        {
            AiPrefix = "AI",
            HumanPrefix = "User",
            SystemPrefix = "System"
        };
        
    }

    public async Task<string> GenerateResponseAsync(Guid chatId, string prompt, CancellationToken token = default)
    {
        var chatHistory = await _messageHistoryStore.GetByChatId(chatId);
        var memory = new ConversationBufferMemory(chatHistory) { Formatter = _messageFormatter };

        var agentChain = await InitAgent(memory, token);

        var chain = Set(prompt, "query")
                    | agentChain
                    | UpdateMemory(memory, "query");

        var response = await chain.RunAsync("text", token);

        return response ?? "I am sorry, I cannot answer this question. Let's talk about something else?";
    }

    private async Task<ReActAgentExecutorChain> InitAgent(ConversationBufferMemory memory, CancellationToken token)
    {
        var collection = await _collectionProvider.GetDefaultCollectionAsync(token);
        var retriever = new VectorStoreRetriever(
            collection,
            _embeddingModel,
            searchType: VectorSearchType.SimilarityScoreThreshold,
            scoreThreshold: 0.45f
        );
        
        var retrieverTool = new RetrievalTool(
            retriever: retriever,
            model: _model,
            name: "candidate_retrieval_tool",
            description:
            "Search for information about available candidates. For any questions about available candidates, you must use this tool",
            documentTemplate: _documentTemplate);
        
        var agentChain = ReActAgentExecutor(
                model: _model,
                reActPrompt: _agentTemplate.Replace("{conversation_history}",
                    memory.LoadMemoryVariables(null).Value["history"] as string),
                inputKey: "query"
            )
            .UseTool(retrieverTool);
        return agentChain;
    }
}