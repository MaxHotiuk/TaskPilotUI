using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Chat;

namespace UI.Services;

public class ChatSystemService : IChatSystemService
{
    private readonly IChatSystemApi _chatApi;

    public ChatSystemService(IChatSystemApi chatApi)
    {
        _chatApi = chatApi;
    }

    public async Task<Guid> CreateChatAsync(CreateChatRequestDto request, CancellationToken cancellationToken = default)
    {
        return await _chatApi.CreateChatAsync(request, cancellationToken);
    }

    public async Task<IEnumerable<ChatDto>> GetChatsAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _chatApi.GetChatsByUserAsync(userId, organizationId, cancellationToken);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(
        Guid chatId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _chatApi.GetMessagesAsync(chatId, userId, page, pageSize, cancellationToken);
    }

    public async Task<ChatMessageDto> SendMessageAsync(
        Guid chatId,
        SendChatMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await _chatApi.SendMessageAsync(chatId, request, cancellationToken);
    }
}
