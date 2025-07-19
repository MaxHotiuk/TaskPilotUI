using Refit;
using UI.Models.Chat;
using UI.Models.Task;

namespace UI.Interfaces.Api;

public interface IChatApi
{
    [Post("/api/chat/ask")]
    Task<ChatResponse> AskAsync([Body] ChatRequest request);
}
