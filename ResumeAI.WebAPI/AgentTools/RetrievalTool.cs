using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.DocumentLoaders;
using LangChain.Prompts;
using LangChain.Providers;
using LangChain.Retrievers;
using static LangChain.Chains.Chain;

namespace ResumeAI.WebAPI.AgentTools;

public class RetrievalTool : AgentTool
{
    private readonly string _promptTemplate = @"
You are a helpful assistant that helps to remove documents that are not relevant for the provided request to user request. 
You will be provided a query from user and a list of documents. You remove all the documents that are not relevant for the user request. Do not remove documents that are relevant for the user request.
Documents will be separated by a series of dash characters like this: ----------
Your response must contain all selected documents exactly as they were provided to you. Do not add or remove any symbols.

Query: {input}

Documents:
----------
{docs}
----------
";
    
    private const string _defaultDocumentTemplate = "{page_content}";
    private readonly BaseRetriever _retriever;
    private readonly IChatModel _model;

    private string _documentTemplate;

    public RetrievalTool(BaseRetriever retriever, IChatModel model, string name, string? description = null, string? documentTemplate = null) : base(name, description)
    {
        _retriever = retriever;
        _model = model;

        _documentTemplate = documentTemplate ?? _defaultDocumentTemplate;
    }

    public override async Task<string> ToolTask(string input, CancellationToken token = default)
    {
        var docs = await _retriever.GetRelevantDocumentsAsync(input, cancellationToken: token);

        var filteredDocs = await RemoveIrrelevantDocuments(input, docs, token);
        
        return filteredDocs;
    }

    private async Task<string> RemoveIrrelevantDocuments(string input, IReadOnlyCollection<Document> docs, CancellationToken token)
    {
        var stuffedDocs = string.Join("----------", docs.Select(FormatDocument));
        
        var chain = Set(input, "input")
                    | Set(stuffedDocs, "docs")
                    | Template(_promptTemplate)
                    | LLM(_model);

        var llmOutput = await chain.RunAsync("text", token);
        return llmOutput ?? string.Empty;
    }

    private string FormatDocument(Document doc)
    {
        return PromptTemplate.InterpolateFString(_documentTemplate, GetDocumentValues(doc));
    }

    private Dictionary<string,object> GetDocumentValues(Document doc)
    {
        var res = new Dictionary<string, object>();
        res.Add("page_content", doc.PageContent);
        foreach (var metadataKey in doc.Metadata.Keys)
        {
            res.Add(metadataKey, doc.Metadata[metadataKey]);
        }

        return res;
    }
}