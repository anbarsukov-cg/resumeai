using LangChain.Databases;
using LangChain.Databases.Postgres;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using Microsoft.EntityFrameworkCore;
using ResumeAI.WebAPI.Data;

namespace ResumeAI.WebAPI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLanguageModel(this IServiceCollection services)
    {
        services.AddTransient<OpenAiProvider>(s =>
        {
            var config = s.GetRequiredService<IConfiguration>();
            var apiKey = config.GetSection("OpenAI").Get<OpenAiConfiguration>()?.ApiKey ?? throw new Exception("OpenAI Api Key is Required");
            return new OpenAiProvider(apiKey);
        });
        services.AddTransient<IChatModel>(s => new Gpt35TurboModel(s.GetRequiredService<OpenAiProvider>()).UseConsoleForDebug());

        services.AddTransient<IEmbeddingModel>(
            s => new TextEmbeddingAda002Model(s.GetRequiredService<OpenAiProvider>()));
        
        return services;
    }
    
    public static IServiceCollection AddPostgres(this IServiceCollection services)
    {
        services.AddDbContext<ResumeContext>(((provider, builder) =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            builder.UseNpgsql(config.GetConnectionString("default"), o => o.UseVector());
        }));

        services.AddScoped<IVectorDatabase>(sc =>
        {
            var config = sc.GetRequiredService<IConfiguration>();
            return new PostgresVectorDatabase(config.GetConnectionString("default")!);
        });
        return services;
    }
}