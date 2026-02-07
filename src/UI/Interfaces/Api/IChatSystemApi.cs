using Refit;
using UI.Models.Chat;

namespace UI.Interfaces.Api;

public interface IChatSystemApi
{
    [Post("/api/chats")]
    Task<Guid> CreateChatAsync([Body] CreateChatRequestDto request, CancellationToken cancellationToken = default);

    [Get("/api/users/{userId}/chats")]
    Task<IEnumerable<ChatDto>> GetChatsByUserAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    [Get("/api/chats/{chatId}/messages")]
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(
        Guid chatId,
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    [Post("/api/chats/{chatId}/messages")]
    Task<ChatMessageDto> SendMessageAsync(
        Guid chatId,
        [Body] SendChatMessageRequestDto request,
        CancellationToken cancellationToken = default);
}
