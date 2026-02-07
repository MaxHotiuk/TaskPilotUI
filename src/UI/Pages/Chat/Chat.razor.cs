using Microsoft.AspNetCore.Components;
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
    private CreateChatRequestDto _createChatRequest = new();
    private readonly List<OrganizationSummaryDto> _organizations = new();
    private readonly List<UserDto> _allUsers = new();
    private readonly List<UserDto> _selectedUsers = new();
    private readonly List<UserDto> _searchResults = new();
    private string _searchText = string.Empty;
    private bool _isInitialized;

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

    [Inject] public IChatSystemService ChatService { get; set; } = default!;
    [Inject] public IChatSignalRService ChatSignalRService { get; set; } = default!;
    [Inject] public IAuthService AuthService { get; set; } = default!;
    [Inject] public IUserService UserService { get; set; } = default!;
    [Inject] public MessageService MessageService { get; set; } = default!;

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
            _chats.AddRange(result.OrderByDescending(chat => chat.UpdatedAt));

            if (_activeChatId.HasValue && _chats.All(chat => chat.Id != _activeChatId.Value))
            {
                _activeChatId = null;
                _activeChat = null;
                _messages.Clear();
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

        _activeChatId = chat.Id;
        _activeChat = chat;
        _currentPage = 1;
        _messages.Clear();
        _hasMoreMessages = false;
        _newMessage = string.Empty;

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
        if (_activeChatId == null || string.IsNullOrWhiteSpace(_newMessage))
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

    private Task OnNewMessageChanged(string message)
    {
        _newMessage = message;
        return Task.CompletedTask;
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

    private void HandleChatMessage(ChatMessageDto message)
    {
        _ = InvokeAsync(() =>
        {
            UpdateChatLastMessage(message);

            if (_activeChatId == message.ChatId && _messages.All(existing => existing.Id != message.Id))
            {
                _messages.Add(message);
                _messages.Sort((left, right) => left.CreatedAt.CompareTo(right.CreatedAt));
            }

            StateHasChanged();
        });
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
            Content = message.Content,
            CreatedAt = message.CreatedAt
        };
        chat.UpdatedAt = message.UpdatedAt;

        _chats.Sort((left, right) => right.UpdatedAt.CompareTo(left.UpdatedAt));
    }

    public void Dispose()
    {
    }
}
