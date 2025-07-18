using UI.Models.Chat;

namespace UI.Interfaces.Services;

public interface IChatService
{
    Task<ChatResponse> AskAsync(ChatRequest request);
}
