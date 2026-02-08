using UI.Models.Chat;
using UI.Models.Attachment;
using System.IO;

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
    Task<StartChatCallResponseDto> StartCallAsync(
        Guid chatId,
        StartChatCallRequestDto request,
        CancellationToken cancellationToken = default);
    Task UpdateReadStatusAsync(
        Guid chatId,
        UpdateChatReadStatusRequestDto request,
        CancellationToken cancellationToken = default);
    Task UpdateChatNameAsync(
        Guid chatId,
        UpdateChatNameRequestDto request,
        CancellationToken cancellationToken = default);
    Task AddChatMembersAsync(
        Guid chatId,
        UpdateChatMembersRequestDto request,
        CancellationToken cancellationToken = default);
    Task RemoveChatMembersAsync(
        Guid chatId,
        UpdateChatMembersRequestDto request,
        CancellationToken cancellationToken = default);
    Task ClearChatHistoryAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteChatAsync(Guid chatId, Guid userId, CancellationToken cancellationToken = default);
    Task<AttachmentDto> UploadChatAttachmentAsync(
        Guid chatId,
        Guid messageId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default);
}
