using Refit;
using UI.Models.Chat;
using UI.Models.Attachment;

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

    [Post("/api/chats/{chatId}/calls")]
    Task<StartChatCallResponseDto> StartCallAsync(
        Guid chatId,
        [Body] StartChatCallRequestDto request,
        CancellationToken cancellationToken = default);

    [Patch("/api/chats/{chatId}/read")]
    Task UpdateReadStatusAsync(
        Guid chatId,
        [Body] UpdateChatReadStatusRequestDto request,
        CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/api/chats/{chatId}/messages/{messageId}/attachments?userId={userId}")]
    Task<AttachmentDto> UploadChatAttachmentAsync(
        Guid chatId,
        Guid messageId,
        [AliasAs("userId")] Guid userId,
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);
}
