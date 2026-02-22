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

    [Patch("/api/chats/{chatId}/name")]
    Task UpdateChatNameAsync(
        Guid chatId,
        [Body] UpdateChatNameRequestDto request,
        CancellationToken cancellationToken = default);

    [Post("/api/chats/{chatId}/members")]
    Task AddChatMembersAsync(
        Guid chatId,
        [Body] UpdateChatMembersRequestDto request,
        CancellationToken cancellationToken = default);

    [Post("/api/chats/{chatId}/members/remove")]
    Task RemoveChatMembersAsync(
        Guid chatId,
        [Body] UpdateChatMembersRequestDto request,
        CancellationToken cancellationToken = default);

    [Delete("/api/chats/{chatId}/messages")]
    Task ClearChatHistoryAsync(
        Guid chatId,
        Guid userId,
        CancellationToken cancellationToken = default);

    [Delete("/api/chats/{chatId}")]
    Task DeleteChatAsync(
        Guid chatId,
        Guid userId,
        CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/api/chats/{chatId}/messages/{messageId}/attachments?userId={userId}")]
    Task<AttachmentDto> UploadChatAttachmentAsync(
        Guid chatId,
        Guid messageId,
        [AliasAs("userId")] Guid userId,
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);

    [Get("/api/chats/{chatId}/avatar")]
    Task<ChatAvatarDto> GetChatAvatarAsync(
        Guid chatId,
        [AliasAs("userId")] Guid userId,
        CancellationToken cancellationToken = default);

    [Multipart]
    [Post("/api/chats/{chatId}/avatar?userId={userId}")]
    Task<ChatAvatarDto> UploadChatAvatarAsync(
        Guid chatId,
        [AliasAs("userId")] Guid userId,
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);

    [Multipart]
    [Put("/api/chats/{chatId}/avatar?userId={userId}")]
    Task<ChatAvatarDto> UpdateChatAvatarAsync(
        Guid chatId,
        [AliasAs("userId")] Guid userId,
        [AliasAs("file")] StreamPart file,
        CancellationToken cancellationToken = default);
}
