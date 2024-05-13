using System.Text;
using System.Text.Json;
using LangChain.Databases;
using LangChain.Databases.Postgres;
using LangChain.DocumentLoaders;
using LangChain.Extensions;
using LangChain.Providers;
using LangChain.Splitters.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ResumeAI.WebAPI.Data;
using ResumeAI.WebAPI.Models;
using UglyToad.PdfPig;
using static LangChain.Chains.Chain;

namespace ResumeAI.WebAPI.Services;

public class ResumeService
{
    private static string _pretifyTemplate = @"
    You are a helpful tool that can prettify text. You will be provided a single line of text with the result of parsing of a resume in PDF format.
    Your task is to split this text into paragraphs where appropriate and remove unreadable symbols. 
    Do not add any text or additional symbols to the input, including markdown. Your response must contain only formatted text.

    Text to format:
--------
{input}
--------
";

    private static string _extractTemplate = """
You are a helpful tool that extracts requested information from resume in JSON format. You will be provided a text of a resume. Your task is to extract required information from it and provide it in the json format.
This json must contain following fields:
- 'CandidateName' : The name of the candidate. If resume does not contain a name, please generate a creative name. Please avoid too common names, like "John Doe".
- 'Profession': The main profession of the candidate
- 'YearsOfCommercialExperience' : a number of years of commercial experience of the candidate. This field must be a single integer.
- 'Skills': an array of up to 5 key skills of the candidate.
- 'Summary': short (up to 5 sentences) summary of candidate's skills and professional experience.
If you are unable to get any of the information from the provided resume, specify 'null' in the respective field. Do not add any fields except for listed above. Do not add any information that was not specified in the original resume.
Your response must contain only a valid json in machine-readable format with all required fields. Do not add any extra characters to the response, including new line characters. Please make sure that resulting json do not contain trailing coma.

Provided resume:
---------
{input}
---------
""";
    
    
    private readonly IChatModel _model;
    private readonly ResumeContext _context;
    private readonly IVectorDatabase _vectorStore;
    private readonly IEmbeddingModel _embeddingModel;

    public ResumeService(IChatModel model, ResumeContext context, IVectorDatabase vectorStore, IEmbeddingModel embeddingModel)
    {
        _model = model;
        _context = context;
        _vectorStore = vectorStore;
        _embeddingModel = embeddingModel;
    }

    public async Task<ResumeDetailsDto> ImportResumeFromStream(Stream pdfStream, CancellationToken token = default)
    {
        var content = await GetResumeContent(pdfStream, token);

        ResumeDetailsDto parsed = await ExtractResumeDetails(content, token);
        var entity = new ResumeEntity()
        {
            CandidateName = parsed.CandidateName,
            Profession = parsed.Profession,
            Skills = parsed.Skills,
            Summary = parsed.Summary,
            YearsOfCommercialExperience = parsed.YearsOfCommercialExperience
        };
        var entry = _context.Resume.Add(entity);
        await _context.SaveChangesAsync(token);

        parsed.Id = entry.Entity.Id;

        await IndexToVectorStore(content, entry, token);

        return parsed;
    }

    private async Task IndexToVectorStore(string content, EntityEntry<ResumeEntity> entry, CancellationToken token)
    {
        var splitDocs = new RecursiveCharacterTextSplitter()
            .SplitText(content)
            .Select((chunk, number) => new Document(chunk, new Dictionary<string, object>()
            {
                { "resumeId", entry.Entity.Id.ToString() },
                { "page", number + 1 }
            })).ToList();

        var vectorCollection = await _vectorStore.GetOrCreateCollectionAsync(
            collectionName: "langchain",
            dimensions: 1536, 
            cancellationToken: token);

        await vectorCollection.AddDocumentsAsync(
            embeddingModel: _embeddingModel,
            documents: splitDocs,
            cancellationToken: token
        );
    }

    private async Task<ResumeDetailsDto> ExtractResumeDetails(string resumeContent, CancellationToken token)
    {
        var chain = Set(resumeContent, "input")
                    | Template(_extractTemplate)
                    | LLM(_model);
        var json = await chain.RunAsync("text", token) ?? throw new Exception("Failed to parse resume");

        return JsonSerializer.Deserialize<ResumeDetailsDto>(json, new JsonSerializerOptions() {AllowTrailingCommas = true}) ?? throw new Exception($"Failed to deserialize LLM output: {json}");
    }

    private async Task<string> GetResumeContent(Stream pdfStream, CancellationToken token)
    {
        using var pdfDocument = PdfDocument.Open(pdfStream);
        
        var resumeText = pdfDocument.GetPages().Aggregate(new StringBuilder(), (s, p) => s.AppendLine(p.Text)).ToString();

        var chain = Set(resumeText, "input")
                    | Template(_pretifyTemplate)
                    | LLM(_model);

        return await chain.RunAsync("text", token) ?? throw new Exception("Failed to parse resume");
    }

    public async Task<List<ResumeDetailsDto>> GetAllAsync(CancellationToken token)
    {
        var entities = await _context.Resume.ToListAsync(token);

        return entities.Select(ToResumeDto).ToList();
    }
    
    public async Task<ResumeDetailsDto?> GetByIdAsync(Guid id, CancellationToken token)
    {
        var entity = await _context.Resume
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync(token);

        return entity == null ? null : ToResumeDto(entity);
    }

    private ResumeDetailsDto ToResumeDto(ResumeEntity entity)
    {
        return new ResumeDetailsDto
        {
            CandidateName = entity.CandidateName,
            Id = entity.Id,
            Profession = entity.Profession,
            Skills = entity.Skills,
            Summary = entity.Summary,
            YearsOfCommercialExperience = entity.YearsOfCommercialExperience
        };
    }
}