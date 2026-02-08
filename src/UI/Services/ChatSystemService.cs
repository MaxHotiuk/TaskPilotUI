using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Models.Chat;
using UI.Models.Attachment;
using Refit;

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

    public async Task<StartChatCallResponseDto> StartCallAsync(
        Guid chatId,
        StartChatCallRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await _chatApi.StartCallAsync(chatId, request, cancellationToken);
    }

    public async Task UpdateReadStatusAsync(
        Guid chatId,
        UpdateChatReadStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _chatApi.UpdateReadStatusAsync(chatId, request, cancellationToken);
    }

    public async Task<AttachmentDto> UploadChatAttachmentAsync(
        Guid chatId,
        Guid messageId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var streamPart = string.IsNullOrWhiteSpace(contentType)
            ? new StreamPart(fileStream, fileName)
            : new StreamPart(fileStream, fileName, contentType);

        return await _chatApi.UploadChatAttachmentAsync(chatId, messageId, userId, streamPart, cancellationToken);
    }
}
