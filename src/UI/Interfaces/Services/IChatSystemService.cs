using UI.Models.Chat;

namespace UI.Interfaces.Services;

public interface IChatSystemService
{
    Task<Guid> CreateChatAsync(CreateChatRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatDto>> GetChatsAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(
        Guid chatId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<ChatMessageDto> SendMessageAsync(
        Guid chatId,
        SendChatMessageRequestDto request,
        CancellationToken cancellationToken = default);
}
