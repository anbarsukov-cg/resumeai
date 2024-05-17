using System.Collections.Concurrent;
using LangChain.Memory;

namespace ResumeAI.WebAPI.Services;

public interface IMessageHistoryStore
{
    Task<ChatMessageHistory> GetByChatId(Guid chatId);
}

public class InMemoryMessageHistoryStore : IMessageHistoryStore
{
    private readonly ConcurrentDictionary<Guid, ChatMessageHistory>
        _store = new ConcurrentDictionary<Guid, ChatMessageHistory>();

    public Task<ChatMessageHistory> GetByChatId(Guid chatId) =>
        Task.FromResult(_store.GetOrAdd(chatId, _ => new ChatMessageHistory()));
}