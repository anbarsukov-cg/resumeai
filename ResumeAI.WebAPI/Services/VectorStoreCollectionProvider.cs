using LangChain.Databases;

namespace ResumeAI.WebAPI.Services;

public class VectorStoreCollectionProvider
{
    private IVectorDatabase _vectorDatabase;

    public VectorStoreCollectionProvider(IVectorDatabase vectorDatabase)
    {
        _vectorDatabase = vectorDatabase;
    }

    public Task<IVectorCollection> GetDefaultCollectionAsync(CancellationToken token = default) =>
        _vectorDatabase.GetOrCreateCollectionAsync(
            collectionName: "langchain",
            dimensions: 1536,
            cancellationToken: token);
}