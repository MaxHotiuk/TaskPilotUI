using UI.Models.Chat;
using UI.Interfaces.Services;
using UI.Interfaces.Api;

namespace UI.Services;

public class ChatService : IChatService
{
    private readonly IChatApi _chatApi;

    public ChatService(IChatApi chatApi)
    {
        _chatApi = chatApi;
    }

    public async Task<ChatResponse> AskAsync(ChatRequest request)
    {
        return await _chatApi.AskAsync(request);
    }
}
