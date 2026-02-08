using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Refit;
using UI.Interfaces.Services;
using UI.Interfaces.SignalR;
using UI.Models.Chat;
using UI.Models.Organization;
using UI.Models.User;

namespace UI.Pages.Chat;

public partial class Chat : ComponentBase, IDisposable
{
    private readonly List<ChatDto> _chats = new();
    private readonly List<ChatMessageDto> _messages = new();
    private readonly HashSet<Guid> _typingUsers = new();
    private Guid _currentUserId;
    private Guid? _organizationId;
    private Guid? _activeChatId;
    private ChatDto? _activeChat;
    private bool _isLoadingChats;
    private bool _isLoadingMessages;
    private bool _isSending;
    private bool _hasMoreMessages;
    private int _currentPage = 1;
    private const int PageSize = 25;
    private string _newMessage = string.Empty;
    private bool _isCreateChatModalVisible;
    private bool _isCreatingChat;
    private bool _isStartingCall;
    private CreateChatRequestDto _createChatRequest = new();
    private readonly List<OrganizationSummaryDto> _organizations = new();
    private readonly List<UserDto> _allUsers = new();
    private readonly List<UserDto> _selectedUsers = new();
    private readonly List<UserDto> _searchResults = new();
    private string _searchText = string.Empty;
    private bool _isInitialized;
    private CancellationTokenSource? _typingCts;
    private bool _isTyping;
    private DateTime? _lastReadAt;
    private static readonly TimeSpan TypingTimeout = TimeSpan.FromSeconds(2.5);
    private readonly List<AttachmentMemory> _pendingAttachments = new();
    private readonly List<string> _pendingAttachmentNames = new();
    private string _currentUserName = string.Empty;
    private bool _isManageChatModalVisible;
    private bool _isUpdatingChat;
    private string _manageChatName = string.Empty;
    private readonly List<UserDto> _manageSelectedUsers = new();
    private readonly List<UserDto> _manageSearchResults = new();
    private string _manageSearchText = string.Empty;
    private readonly HashSet<Guid> _membersToRemove = new();
    private bool _isClearChatModalVisible;
    private bool _isClearingChatHistory;
    private bool _isDeleteChatModalVisible;
    private bool _isDeletingChat;

    private Guid? OrganizationId
    {
        get => _organizationId;
        set
        {
            if (_organizationId == value)
                return;

            _organizationId = value;

            if (_isInitialized)
            {
                _ = LoadChatsAsync();
            }
        }
    }

    private bool CanManageActiveChat()
    {
        if (_activeChat?.Type != ChatType.Group)
            return false;

        return _activeChat.Members.Any(member => member.UserId == _currentUserId && member.Role == ChatMemberRole.Owner);
    }

    private bool CanClearActiveChat()
    {
        if (_activeChat == null)
            return false;

        if (_activeChat.Type == ChatType.Private)
            return true;

        return CanManageActiveChat();
    }

    private bool CanDeleteActiveChat()
    {
        if (_activeChat == null)
            return false;

        if (_activeChat.Type == ChatType.Private)
            return true;

        return CanManageActiveChat();
    }

    private void OpenManageChatModal()
    {
        if (!CanManageActiveChat() || _activeChat == null)
            return;

        ResetManageChatForm();
        _isManageChatModalVisible = true;
    }

    private void CloseManageChatModal()
    {
        _isManageChatModalVisible = false;
    }

    private void ResetManageChatForm()
    {
        _manageChatName = _activeChat?.Name ?? string.Empty;
        _manageSearchText = string.Empty;
        _manageSearchResults.Clear();
        _manageSelectedUsers.Clear();
        _membersToRemove.Clear();
    }

    private IEnumerable<ChatMemberDto> GetActiveChatMembers()
    {
        if (_activeChat == null)
            return Array.Empty<ChatMemberDto>();

        return _activeChat.Members.Where(member => !_membersToRemove.Contains(member.UserId));
    }

    private IEnumerable<ChatMemberDto> GetRemovedMembers()
    {
        if (_activeChat == null)
            return Array.Empty<ChatMemberDto>();

        return _activeChat.Members.Where(member => _membersToRemove.Contains(member.UserId));
    }

    private bool CanRemoveMember(ChatMemberDto member)
    {
        if (_activeChat?.Type != ChatType.Group)
            return false;

        if (member.UserId == _currentUserId)
            return false;

        return true;
    }

    private void MarkMemberForRemoval(ChatMemberDto member)
    {
        if (!CanRemoveMember(member))
            return;

        _membersToRemove.Add(member.UserId);
    }

    private void UnmarkMemberForRemoval(ChatMemberDto member)
    {
        _membersToRemove.Remove(member.UserId);
    }

    private void OnManageSearchInput(ChangeEventArgs e)
    {
        var searchText = e.Value?.ToString() ?? string.Empty;
        _manageSearchText = searchText;
        OnManageSearch(searchText);
    }

    private void OnManageSearch(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _manageSearchResults.Clear();
            return;
        }

        var lowerSearchText = searchText.ToLowerInvariant();
        var selectedIds = _manageSelectedUsers.Select(u => u.Id).ToHashSet();
        var existingIds = _activeChat?.Members.Select(member => member.UserId).ToHashSet() ?? new HashSet<Guid>();

        _manageSearchResults.Clear();
        _manageSearchResults.AddRange(_allUsers
            .Where(u => u.Email.ToLowerInvariant().Contains(lowerSearchText) || u.Username.ToLowerInvariant().Contains(lowerSearchText))
            .Where(u => !selectedIds.Contains(u.Id))
            .Where(u => !existingIds.Contains(u.Id))
            .Where(u => u.Id != _currentUserId)
            .Where(u => !_organizationId.HasValue || u.Organizations == null || !u.Organizations.Any() || u.Organizations.Any(o => o.Id == _organizationId.Value))
            .Take(10));
    }

    private void SelectManageUser(UserDto user)
    {
        if (_manageSelectedUsers.Any(u => u.Id == user.Id))
            return;

        _manageSelectedUsers.Add(user);
        _manageSearchText = string.Empty;
        _manageSearchResults.Clear();
        StateHasChanged();
    }

    private void RemoveManageUser(UserDto user)
    {
        _manageSelectedUsers.Remove(user);
        StateHasChanged();
    }

    private async Task SaveChatManagementAsync()
    {
        if (_activeChat == null || !CanManageActiveChat())
            return;

        _isUpdatingChat = true;
        StateHasChanged();

        try
        {
            if (!string.Equals(_manageChatName, _activeChat.Name, StringComparison.Ordinal))
            {
                var name = _manageChatName?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageService.Error("Chat name cannot be empty.");
                    return;
                }

                await ChatService.UpdateChatNameAsync(_activeChat.Id, new UpdateChatNameRequestDto
                {
                    UserId = _currentUserId,
                    Name = name
                });
            }

            if (_manageSelectedUsers.Any())
            {
                await ChatService.AddChatMembersAsync(_activeChat.Id, new UpdateChatMembersRequestDto
                {
                    UserId = _currentUserId,
                    MemberIds = _manageSelectedUsers.Select(user => user.Id).ToList()
                });
            }

            if (_membersToRemove.Any())
            {
                await ChatService.RemoveChatMembersAsync(_activeChat.Id, new UpdateChatMembersRequestDto
                {
                    UserId = _currentUserId,
                    MemberIds = _membersToRemove.ToList()
                });
            }

            _isManageChatModalVisible = false;
            await LoadChatsAsync();
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to update chat: {ex.Message}");
        }
        finally
        {
            _isUpdatingChat = false;
            StateHasChanged();
        }
    }

    private void OpenClearChatModal()
    {
        if (!CanClearActiveChat())
            return;

        _isClearChatModalVisible = true;
    }

    private void CloseClearChatModal()
    {
        _isClearChatModalVisible = false;
    }

    private void OpenDeleteChatModal()
    {
        if (!CanDeleteActiveChat())
            return;

        _isDeleteChatModalVisible = true;
    }

    private void CloseDeleteChatModal()
    {
        _isDeleteChatModalVisible = false;
    }

    private async Task ClearChatHistoryAsync()
    {
        if (_activeChatId == null || _currentUserId == Guid.Empty)
            return;

        _isClearingChatHistory = true;
        StateHasChanged();

        try
        {
            await ChatService.ClearChatHistoryAsync(_activeChatId.Value, _currentUserId);
            _messages.Clear();
            if (_activeChat != null)
            {
                _activeChat.LastMessage = null;
            }

            var chat = _chats.FirstOrDefault(item => item.Id == _activeChatId.Value);
            if (chat != null)
            {
                chat.LastMessage = null;
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to clear chat history: {ex.Message}");
        }
        finally
        {
            _isClearingChatHistory = false;
            _isClearChatModalVisible = false;
            StateHasChanged();
        }
    }

    private async Task DeleteChatAsync()
    {
        if (_activeChatId == null || _currentUserId == Guid.Empty)
            return;

        _isDeletingChat = true;
        StateHasChanged();

        try
        {
            await ChatService.DeleteChatAsync(_activeChatId.Value, _currentUserId);
            await ChatSignalRService.LeaveChatGroupAsync(_activeChatId.Value.ToString());
            var chatId = _activeChatId.Value;
            _chats.RemoveAll(chat => chat.Id == chatId);
            _messages.Clear();
            _activeChatId = null;
            _activeChat = null;
            _isDeleteChatModalVisible = false;
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to delete chat: {ex.Message}");
        }
        finally
        {
            _isDeletingChat = false;
            StateHasChanged();
        }
    }

    [Inject] public IChatSystemService ChatService { get; set; } = default!;
    [Inject] public IChatSignalRService ChatSignalRService { get; set; } = default!;
    [Inject] public IAuthService AuthService { get; set; } = default!;
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public MessageService MessageService { get; set; } = default!;
    [Inject] public IAttachmentService AttachmentService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private bool _canLoadChats => _currentUserId != Guid.Empty && _organizationId.HasValue;

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUserAsync();
        await ConnectSignalRAsync();
        _isInitialized = true;

        if (_organizationId.HasValue)
        {
            await LoadChatsAsync();
        }
    }

    private async Task LoadCurrentUserAsync()
    {
        var user = await AuthService.GetCurrentUserAsync();
        if (user != null && user.Id != Guid.Empty)
        {
            _currentUserId = user.Id;
            _currentUserName = user.Username;
            _organizations.Clear();
            _organizations.AddRange(user.Organizations);

            if (!_organizationId.HasValue && _organizations.Count > 0)
            {
                _organizationId = _organizations[0].Id;
            }
        }

        await LoadAllUsersAsync();
    }

    private async Task ConnectSignalRAsync()
    {
        if (_currentUserId == Guid.Empty)
            return;

        if (!ChatSignalRService.IsConnected)
        {
            await ChatSignalRService.ConnectAsync();
            await ChatSignalRService.JoinUserGroupAsync(_currentUserId.ToString());
        }

        ChatSignalRService.OnChatCreated(HandleChatUpsert);
        ChatSignalRService.OnChatUpdated(HandleChatUpsert);
        ChatSignalRService.OnChatMessageReceived(HandleChatMessage);
        ChatSignalRService.OnUserTyping(HandleUserTyping);
        ChatSignalRService.OnUserStoppedTyping(HandleUserStoppedTyping);
    }

    private async Task LoadChatsAsync()
    {
        if (_currentUserId == Guid.Empty)
        {
            MessageService.Error("User not found.");
            return;
        }

        if (!_organizationId.HasValue)
        {
            MessageService.Error("Select an organization.");
            return;
        }

        var organizationId = _organizationId.Value;
        _isLoadingChats = true;
        StateHasChanged();

        try
        {
            var result = await ChatService.GetChatsAsync(_currentUserId, organizationId);
            _chats.Clear();
            foreach (var chat in result.OrderByDescending(chat => chat.UpdatedAt))
            {
                ApplyMemberNames(chat);
                _chats.Add(chat);
            }

            if (_activeChatId.HasValue && _chats.All(chat => chat.Id != _activeChatId.Value))
            {
                _activeChatId = null;
                _activeChat = null;
                _messages.Clear();
            }
            else if (_activeChat != null)
            {
                ApplyMemberNames(_activeChat);
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load chats: {ex.Message}");
        }
        finally
        {
            _isLoadingChats = false;
            StateHasChanged();
        }
    }

    private async Task SelectChatAsync(ChatDto chat)
    {
        if (_activeChatId == chat.Id)
            return;

        var previousChatId = _activeChatId;
        if (previousChatId.HasValue)
        {
            await ChatSignalRService.LeaveChatGroupAsync(previousChatId.Value.ToString());
        }

        await StopTypingAsync();
        _typingUsers.Clear();
        _lastReadAt = null;
        _activeChatId = chat.Id;
        _activeChat = chat;
        _currentPage = 1;
        _messages.Clear();
        _hasMoreMessages = false;
        _newMessage = string.Empty;
        ClearAttachments();

        await ChatSignalRService.JoinChatGroupAsync(chat.Id.ToString());
        await LoadMessagesAsync(reset: true);
    }

    private async Task LoadMessagesAsync(bool reset)
    {
        if (_activeChatId == null)
            return;

        _isLoadingMessages = true;
        StateHasChanged();

        try
        {
            var result = await ChatService.GetMessagesAsync(_activeChatId.Value, _currentUserId, _currentPage, PageSize);
            var messages = result.OrderBy(message => message.CreatedAt).ToList();

            if (reset)
            {
                _messages.Clear();
            }

            foreach (var message in messages)
            {
                if (_messages.All(existing => existing.Id != message.Id))
                {
                    _messages.Add(message);
                }
            }

            _messages.Sort((left, right) => left.CreatedAt.CompareTo(right.CreatedAt));
            _hasMoreMessages = messages.Count == PageSize;
            await MarkChatReadAsync();
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load messages: {ex.Message}");
        }
        finally
        {
            _isLoadingMessages = false;
            StateHasChanged();
        }
    }

    private async Task LoadMoreMessagesAsync()
    {
        if (_activeChatId == null || !_hasMoreMessages)
            return;

        _currentPage++;
        await LoadMessagesAsync(reset: false);
    }

    private async Task SendMessageAsync()
    {
        if (_activeChatId == null || (string.IsNullOrWhiteSpace(_newMessage) && !_pendingAttachments.Any()))
            return;

        _isSending = true;
        StateHasChanged();

        try
        {
            var request = new SendChatMessageRequestDto
            {
                SenderId = _currentUserId,
                Content = _newMessage.Trim()
            };

            var message = await ChatService.SendMessageAsync(_activeChatId.Value, request);
            _newMessage = string.Empty;

            if (_messages.All(existing => existing.Id != message.Id))
            {
                _messages.Add(message);
                _messages.Sort((left, right) => left.CreatedAt.CompareTo(right.CreatedAt));
            }

            UpdateChatLastMessage(message);
            if (_pendingAttachments.Any())
            {
                await UploadMessageAttachmentsAsync(message.Id);
            }

            ClearAttachments();
            await StopTypingAsync();
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to send message: {ex.Message}");
        }
        finally
        {
            _isSending = false;
            StateHasChanged();
        }
    }

    private async Task StartCallAsync()
    {
        if (_activeChatId == null || _currentUserId == Guid.Empty || _isStartingCall)
            return;

        _isStartingCall = true;
        StateHasChanged();

        try
        {
            var request = new StartChatCallRequestDto
            {
                SenderId = _currentUserId
            };

            var response = await ChatService.StartCallAsync(_activeChatId.Value, request);
            if (response.Message != null)
            {
                HandleChatMessage(response.Message);
            }

            if (!string.IsNullOrWhiteSpace(response.RoomUrl))
            {
                NavigateToCallRoom(response.RoomUrl);
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to start call: {ex.Message}");
        }
        finally
        {
            _isStartingCall = false;
            StateHasChanged();
        }
    }

    private Task JoinCallAsync(string roomUrl)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            return Task.CompletedTask;

        NavigateToCallRoom(roomUrl);
        return Task.CompletedTask;
    }

    private void NavigateToCallRoom(string roomUrl)
    {
        var callUrl = AppendUserName(roomUrl);
        if (string.IsNullOrWhiteSpace(callUrl))
            return;

        NavigationManager.NavigateTo(callUrl, forceLoad: true);
    }

    private string AppendUserName(string roomUrl)
    {
        if (string.IsNullOrWhiteSpace(roomUrl) || string.IsNullOrWhiteSpace(_currentUserName))
            return roomUrl;

        if (roomUrl.Contains("userName=", StringComparison.OrdinalIgnoreCase))
            return roomUrl;

        var separator = roomUrl.Contains('?') ? "&" : "?";
        var userName = Uri.EscapeDataString(_currentUserName);
        return $"{roomUrl}{separator}userName={userName}";
    }

    private async Task OnNewMessageChanged(string message)
    {
        _newMessage = message;

        if (_activeChatId == null || _currentUserId == Guid.Empty)
            return;

        if (string.IsNullOrWhiteSpace(message))
        {
            await StopTypingAsync();
            return;
        }

        await StartTypingAsync();
        ScheduleStopTyping();
    }

    private async Task OnAttachmentsSelected(InputFileChangeEventArgs e)
    {
        if (e.FileCount == 0)
            return;

        foreach (var file in e.GetMultipleFiles())
        {
            if (_pendingAttachments.Any(att => att.Name == file.Name))
                continue;

            using var stream = file.OpenReadStream(long.MaxValue);
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            _pendingAttachments.Add(new AttachmentMemory
            {
                Name = file.Name,
                Data = memory.ToArray(),
                ContentType = file.ContentType
            });
            _pendingAttachmentNames.Add(file.Name);
        }

        StateHasChanged();
    }

    private void RemoveAttachmentAt(int index)
    {
        if (index < 0 || index >= _pendingAttachmentNames.Count)
            return;

        var name = _pendingAttachmentNames[index];
        _pendingAttachmentNames.RemoveAt(index);
        var attachment = _pendingAttachments.FirstOrDefault(att => att.Name == name);
        if (attachment != null)
        {
            _pendingAttachments.Remove(attachment);
        }
        StateHasChanged();
    }

    private void ClearAttachments()
    {
        _pendingAttachments.Clear();
        _pendingAttachmentNames.Clear();
        StateHasChanged();
    }

    private async Task UploadMessageAttachmentsAsync(Guid messageId)
    {
        if (_activeChatId == null || _currentUserId == Guid.Empty)
            return;

        var uploadedAny = false;
        foreach (var attachment in _pendingAttachments)
        {
            try
            {
                using var stream = new MemoryStream(attachment.Data);
                await ChatService.UploadChatAttachmentAsync(
                    _activeChatId.Value,
                    messageId,
                    _currentUserId,
                    stream,
                    attachment.Name,
                    attachment.ContentType);
                uploadedAny = true;
            }
            catch (Exception ex)
            {
                MessageService.Error($"Failed to upload attachment '{attachment.Name}': {ex.Message}");
            }
        }

        if (uploadedAny)
        {
            MarkMessageHasAttachments(messageId);
        }
    }

    private void MarkMessageHasAttachments(Guid messageId)
    {
        var message = _messages.FirstOrDefault(item => item.Id == messageId);
        if (message == null)
            return;

        message.HasAttachments = true;
        StateHasChanged();
    }

    private void OpenCreateChat()
    {
        _createChatRequest = new CreateChatRequestDto
        {
            Type = ChatType.Private
        };
        _selectedUsers.Clear();
        _searchResults.Clear();
        _searchText = string.Empty;
        _isCreateChatModalVisible = true;
    }

    private void CloseCreateChat()
    {
        _isCreateChatModalVisible = false;
    }

    private async Task CreateChatAsync()
    {
        if (_organizationId == null)
        {
            MessageService.Error("Load chats first to set the organization id.");
            return;
        }

        if (_createChatRequest.Type == ChatType.Group && string.IsNullOrWhiteSpace(_createChatRequest.Name))
        {
            MessageService.Error("Group chat requires a name.");
            return;
        }

        var memberIds = _selectedUsers.Select(user => user.Id).ToList();

        if (_createChatRequest.Type == ChatType.Private && memberIds.Count != 1)
        {
            MessageService.Error("Private chat requires exactly one other member.");
            return;
        }

        if (_createChatRequest.Type == ChatType.Group && memberIds.Count == 0)
        {
            MessageService.Error("Provide at least one member.");
            return;
        }

        if (!memberIds.Contains(_currentUserId))
        {
            memberIds.Add(_currentUserId);
        }

        _createChatRequest.OrganizationId = _organizationId.Value;
        _createChatRequest.CreatedById = _currentUserId;
        _createChatRequest.MemberIds = memberIds;

        _isCreatingChat = true;
        StateHasChanged();

        try
        {
            await ChatService.CreateChatAsync(_createChatRequest);
            _isCreateChatModalVisible = false;
            await LoadChatsAsync();
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to create chat: {ex.Message}");
        }
        finally
        {
            _isCreatingChat = false;
            StateHasChanged();
        }
    }

    private async Task LoadAllUsersAsync()
    {
        try
        {
            if (await AuthService.IsAuthenticatedAsync())
            {
                _allUsers.Clear();
                _allUsers.AddRange(await UserService.GetAllUsersAsync());
            }
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to load users: {ex.Message}");
        }
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        var searchText = e.Value?.ToString() ?? string.Empty;
        _searchText = searchText;
        OnSearch(searchText);
    }

    private void OnSearch(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _searchResults.Clear();
            return;
        }

        var lowerSearchText = searchText.ToLowerInvariant();
        var selectedIds = _selectedUsers.Select(u => u.Id).ToHashSet();

        _searchResults.Clear();
        _searchResults.AddRange(_allUsers
            .Where(u => u.Email.ToLowerInvariant().Contains(lowerSearchText) || u.Username.ToLowerInvariant().Contains(lowerSearchText))
            .Where(u => !selectedIds.Contains(u.Id))
            .Where(u => u.Id != _currentUserId)
            .Where(u => !_organizationId.HasValue || u.Organizations == null || !u.Organizations.Any() || u.Organizations.Any(o => o.Id == _organizationId.Value))
            .Take(10));
    }

    private void SelectUser(UserDto user)
    {
        if (_selectedUsers.Any(u => u.Id == user.Id))
            return;

        _selectedUsers.Add(user);
        _searchText = string.Empty;
        _searchResults.Clear();
        StateHasChanged();
    }

    private void RemoveUser(UserDto user)
    {
        _selectedUsers.Remove(user);
        StateHasChanged();
    }

    private void HandleChatUpsert(ChatDto chat)
    {
        _ = InvokeAsync(() =>
        {
            var existingChat = _chats.FirstOrDefault(existing => existing.Id == chat.Id);
            if (existingChat != null && chat.LastMessage == null)
            {
                chat.LastMessage = existingChat.LastMessage;
            }

            ApplyMemberNames(chat);
            var index = _chats.FindIndex(existing => existing.Id == chat.Id);
            if (index >= 0)
            {
                _chats[index] = chat;
            }
            else
            {
                _chats.Insert(0, chat);
            }

            _chats.Sort((left, right) => right.UpdatedAt.CompareTo(left.UpdatedAt));

            if (_activeChatId == chat.Id)
            {
                _activeChat = chat;
            }

            StateHasChanged();
        });
    }

    private void ApplyMemberNames(ChatDto chat)
    {
        if (!_allUsers.Any())
            return;

        foreach (var member in chat.Members)
        {
            if (!string.IsNullOrWhiteSpace(member.UserName))
                continue;

            var user = _allUsers.FirstOrDefault(u => u.Id == member.UserId);
            if (user != null)
            {
                member.UserName = user.Username;
            }
        }
    }

    private void HandleChatMessage(ChatMessageDto message)
    {
        _ = InvokeAsync(() =>
        {
            UpdateChatLastMessage(message);

            if (_activeChatId == message.ChatId && _messages.All(existing => existing.Id != message.Id))
            {
                _messages.Add(message);
                _messages.Sort((left, right) => left.CreatedAt.CompareTo(right.CreatedAt));
                if (message.SenderId != _currentUserId)
                {
                    _ = MarkChatReadAsync();
                    if (!message.HasAttachments && !string.Equals(message.MessageType, "Call", StringComparison.OrdinalIgnoreCase))
                    {
                        _ = ProbeAttachmentsAsync(message);
                    }
                }
            }

            StateHasChanged();
        });
    }

    private async Task ProbeAttachmentsAsync(ChatMessageDto message)
    {
        if (message.Id == Guid.Empty || _currentUserId == Guid.Empty)
            return;

        const int maxAttempts = 3;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var attachments = await AttachmentService.GetAsync(message.Id, _currentUserId);
                if (attachments.Any())
                {
                    message.HasAttachments = true;
                    await InvokeAsync(StateHasChanged);
                    return;
                }
            }
            catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
            catch
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1.5));
        }
    }

    private void HandleUserTyping(ChatTypingEvent typingEvent)
    {
        if (_activeChatId != typingEvent.ChatId || typingEvent.UserId == _currentUserId)
            return;

        _typingUsers.Add(typingEvent.UserId);
        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleUserStoppedTyping(ChatTypingEvent typingEvent)
    {
        if (_activeChatId != typingEvent.ChatId || typingEvent.UserId == _currentUserId)
            return;

        _typingUsers.Remove(typingEvent.UserId);
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task StartTypingAsync()
    {
        if (_isTyping || _activeChatId == null)
            return;

        _isTyping = true;
        await ChatSignalRService.StartTypingAsync(_activeChatId.Value.ToString(), _currentUserId.ToString());
    }

    private void ScheduleStopTyping()
    {
        _typingCts?.Cancel();
        _typingCts = new CancellationTokenSource();
        var token = _typingCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TypingTimeout, token);
                if (!token.IsCancellationRequested)
                {
                    await InvokeAsync(StopTypingAsync);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }, token);
    }

    private async Task StopTypingAsync()
    {
        _typingCts?.Cancel();
        _typingCts = null;

        if (!_isTyping)
            return;

        _isTyping = false;
        if (_activeChatId == null)
            return;

        await ChatSignalRService.StopTypingAsync(_activeChatId.Value.ToString(), _currentUserId.ToString());
    }

    private IReadOnlyList<string> GetTypingUsers()
    {
        if (_activeChat == null)
            return Array.Empty<string>();

        return _typingUsers
            .Select(id => _activeChat.Members.FirstOrDefault(member => member.UserId == id)?.UserName ?? "Someone")
            .Distinct()
            .ToList();
    }

    private async Task MarkChatReadAsync()
    {
        if (_activeChatId == null)
            return;

        var readAt = DateTime.UtcNow;
        if (_lastReadAt.HasValue && readAt - _lastReadAt.Value < TimeSpan.FromSeconds(5))
            return;

        var request = new UpdateChatReadStatusRequestDto
        {
            UserId = _currentUserId,
            ReadAt = readAt
        };

        try
        {
            await ChatService.UpdateReadStatusAsync(_activeChatId.Value, request);
            _lastReadAt = readAt;
            UpdateLocalReadStatus(readAt);
        }
        catch (Exception ex)
        {
            MessageService.Error($"Failed to update read status: {ex.Message}");
        }
    }

    private void UpdateLocalReadStatus(DateTime readAt)
    {
        if (_activeChat == null)
            return;

        var member = _activeChat.Members.FirstOrDefault(m => m.UserId == _currentUserId);
        if (member != null)
        {
            member.LastReadAt = readAt;
        }

        var chat = _chats.FirstOrDefault(c => c.Id == _activeChat.Id);
        if (chat != null)
        {
            var chatMember = chat.Members.FirstOrDefault(m => m.UserId == _currentUserId);
            if (chatMember != null)
            {
                chatMember.LastReadAt = readAt;
            }
        }
    }

    private void UpdateChatLastMessage(ChatMessageDto message)
    {
        var chat = _chats.FirstOrDefault(existing => existing.Id == message.ChatId);
        if (chat == null)
            return;

        chat.LastMessage = new ChatMessagePreviewDto
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            MessageType = message.MessageType,
            Content = message.Content,
            TaskId = message.TaskId,
            CreatedAt = message.CreatedAt
        };
        chat.UpdatedAt = message.UpdatedAt;

        _chats.Sort((left, right) => right.UpdatedAt.CompareTo(left.UpdatedAt));
    }

    public void Dispose()
    {
        _ = StopTypingAsync();
        if (_activeChatId.HasValue)
        {
            _ = ChatSignalRService.LeaveChatGroupAsync(_activeChatId.Value.ToString());
        }

        if (_currentUserId != Guid.Empty)
        {
            _ = ChatSignalRService.LeaveUserGroupAsync(_currentUserId.ToString());
        }

        _ = ChatSignalRService.DisconnectAsync();
    }

    private class AttachmentMemory
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
    }
}
