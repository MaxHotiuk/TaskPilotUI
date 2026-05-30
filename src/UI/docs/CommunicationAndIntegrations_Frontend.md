# 3.3 Communication and Integrations — Frontend Logic Layer

This document covers the real-time communication infrastructure and external service integration logic present in the TaskPilot UI project. It is scoped exclusively to the service and logic layer. UI layout and Razor component markup are covered in section 3.6.

---

## Table of Contents

1. [SignalR Client Services](#1-signalr-client-services)
   - 1.1 [ISignalRService / SignalRService (Board Hub)](#11-isignalrservice--signalrservice-board-hub)
   - 1.2 [IChatSignalRService / ChatSignalRService (Chat Hub)](#12-ichatSignalRservice--chatsignalrservice-chat-hub)
   - 1.3 [INotificationSignalRService / NotificationSignalRService (Notification Hub)](#13-inotificationsignalrservice--notificationsignalrservice-notification-hub)
2. [Real-Time State Management](#2-real-time-state-management)
   - 2.1 [Handler Registration Pattern](#21-handler-registration-pattern)
   - 2.2 [Notification Display via Ant Design](#22-notification-display-via-ant-design)
   - 2.3 [Layout-Level Bootstrapping](#23-layout-level-bootstrapping)
3. [External Integrations Logic](#3-external-integrations-logic)
   - 3.1 [Video Calls via WebRTC / SignalR Signalling (Board Meetings)](#31-video-calls-via-webrtc--signalr-signalling-board-meetings)
   - 3.2 [Video Calls via Daily.co Room URL (Chat Calls)](#32-video-calls-via-dailyco-room-url-chat-calls)
   - 3.3 [Google Calendar Synchronization](#33-google-calendar-synchronization)

---

## 1. SignalR Client Services

All three SignalR services follow the same structural contract:

- A private `HubConnection?` field manages the underlying connection lifetime.
- `ConnectAsync()` builds and starts the connection only if it is not already active.
- `WithAutomaticReconnect()` instructs the client to attempt reconnection with the default back-off policy (0 s, 2 s, 10 s, 30 s).
- The hub URL is assembled from `Api:BaseUrl` in `appsettings.json`, making the services environment-agnostic.
- Each service implements `IAsyncDisposable` so Blazor's DI container can properly tear down connections when a component or the app disposes.

### 1.1 `ISignalRService` / `SignalRService` (Board Hub)

**Interface contract** (`Interfaces/SignalR/ISignalRService.cs`):

```csharp
public interface ISignalRService : IAsyncDisposable
{
	Task ConnectAsync();
	Task DisconnectAsync();
	Task JoinBoardGroupAsync(string boardId);
	Task LeaveBoardGroupAsync(string boardId);
	Task JoinTaskGroupAsync(string taskId);
	Task LeaveTaskGroupAsync(string taskId);
	void OnBoardUpdated(Action<object> handler);
	void OnTaskUpdated(Action<object> handler);
	bool IsConnected { get; }
}
```

**Implementation** (`Services/SignalRService.cs`):

```csharp
public class SignalRService : ISignalRService
{
	private readonly string _apiBaseUrl;
	private HubConnection? _hubConnection;

	public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	public SignalRService(NavigationManager navigationManager, ILogger<SignalRService> logger, IConfiguration configuration)
	{
		_apiBaseUrl = configuration["Api:BaseUrl"]
			?? throw new InvalidOperationException("API Base URL is not configured.");
	}

	public async Task ConnectAsync()
	{
		// Guard: do not reconnect if already connected or connecting
		if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
			return;

		var hubUrl = $"{_apiBaseUrl.TrimEnd('/')}/hubs/board";

		_hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUrl)
			.WithAutomaticReconnect()   // default back-off: 0s, 2s, 10s, 30s
			.Build();

		await _hubConnection.StartAsync();
	}

	// Hub group management — maps to server-side group methods
	public async Task JoinBoardGroupAsync(string boardId) =>
		await _hubConnection!.InvokeAsync("JoinBoardGroup", boardId);

	public async Task JoinTaskGroupAsync(string taskId) =>
		await _hubConnection!.InvokeAsync("JoinTaskGroup", taskId);

	// Event handler registration — components call these to subscribe
	public void OnBoardUpdated(Action<object> handler) =>
		_hubConnection?.On("BoardUpdated", handler);

	public void OnTaskUpdated(Action<object> handler) =>
		_hubConnection?.On("TaskUpdated", handler);

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection != null)
		{
			await _hubConnection.DisposeAsync();
			_hubConnection = null;
		}
	}
}
```

**Hub endpoint:** `{Api:BaseUrl}/hubs/board`

The board hub enables real-time collaboration: when any team member modifies a board or task, the server broadcasts to the relevant group and all subscribed clients update their local state without reloading.

---

### 1.2 `IChatSignalRService` / `ChatSignalRService` (Chat Hub)

**Interface contract** (`Interfaces/SignalR/IChatSignalRService.cs`):

```csharp
public interface IChatSignalRService : IAsyncDisposable
{
	Task ConnectAsync();
	Task DisconnectAsync();
	Task JoinUserGroupAsync(string userId);
	Task LeaveUserGroupAsync(string userId);
	Task JoinChatGroupAsync(string chatId);
	Task LeaveChatGroupAsync(string chatId);
	Task StartTypingAsync(string chatId, string userId);
	Task StopTypingAsync(string chatId, string userId);
	void OnChatCreated(Action<ChatDto> handler);
	void OnChatUpdated(Action<ChatDto> handler);
	void OnChatMessageReceived(Action<ChatMessageDto> handler);
	void OnUserTyping(Action<ChatTypingEvent> handler);
	void OnUserStoppedTyping(Action<ChatTypingEvent> handler);
	bool IsConnected { get; }
}
```

**Implementation** (`Services/ChatSignalRService.cs`):

```csharp
public class ChatSignalRService : IChatSignalRService
{
	private readonly string _apiBaseUrl;
	private HubConnection? _hubConnection;

	public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	public async Task ConnectAsync()
	{
		if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
			return;

		var hubUrl = $"{_apiBaseUrl.TrimEnd('/')}/hubs/chat";

		_hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUrl)
			.WithAutomaticReconnect()
			.Build();

		await _hubConnection.StartAsync();
	}

	// Client → Server: typing indicators
	public async Task StartTypingAsync(string chatId, string userId) =>
		await _hubConnection!.InvokeAsync("StartTyping", chatId, userId);

	public async Task StopTypingAsync(string chatId, string userId) =>
		await _hubConnection!.InvokeAsync("StopTyping", chatId, userId);

	// Server → Client: message and event handlers
	public void OnChatMessageReceived(Action<ChatMessageDto> handler) =>
		_hubConnection?.On("ReceiveChatMessage", handler);

	public void OnChatCreated(Action<ChatDto> handler) =>
		_hubConnection?.On("ChatCreated", handler);

	public void OnChatUpdated(Action<ChatDto> handler) =>
		_hubConnection?.On("ChatUpdated", handler);

	public void OnUserTyping(Action<ChatTypingEvent> handler) =>
		_hubConnection?.On("UserTyping", handler);

	public void OnUserStoppedTyping(Action<ChatTypingEvent> handler) =>
		_hubConnection?.On("UserStoppedTyping", handler);
}
```

**Hub endpoint:** `{Api:BaseUrl}/hubs/chat`

The chat service distinguishes two scoping levels:
- **User groups** (`JoinUserGroupAsync`) — used so the server can push events like new chat creation directly to a specific user regardless of which chat room they currently have open.
- **Chat groups** (`JoinChatGroupAsync`) — used to scope real-time message delivery and typing indicators to a specific conversation.

---

### 1.3 `INotificationSignalRService` / `NotificationSignalRService` (Notification Hub)

**Interface contract** (`Interfaces/SignalR/INotificationSignalRService.cs`):

```csharp
public interface INotificationSignalRService : IAsyncDisposable
{
	Task ConnectAsync();
	Task DisconnectAsync();
	Task JoinUserGroupAsync(string userId);
	Task LeaveUserGroupAsync(string userId);
	void OnNotificationReceived(Action<object> handler);
	bool IsConnected { get; }
}
```

**Implementation** (`Services/NotificationSignalRService.cs`):

This service has an additional responsibility: it directly renders Ant Design toast messages when a notification arrives, removing the need for components to listen at all for basic pop-up feedback.

```csharp
public class NotificationSignalRService : INotificationSignalRService
{
	private readonly IMessageService _messageService; // Ant Design toast service
	private HubConnection? _hubConnection;

	public async Task ConnectAsync()
	{
		if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
			return;

		var hubUrl = $"{_apiBaseUrl.TrimEnd('/')}/hubs/notification";

		_hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUrl)
			.WithAutomaticReconnect()
			.Build();

		// Register the internal handler BEFORE starting the connection
		SetupNotificationHandler();

		await _hubConnection.StartAsync();
	}

	private void SetupNotificationHandler()
	{
		_hubConnection?.On<Notification>("ReceiveNotification", (notification) =>
		{
			ShowNotificationMessage(notification);
		});
	}

	private void ShowNotificationMessage(Notification notification)
	{
		var config = new MessageConfig
		{
			Content = $"🔔 {TruncateText(notification.Text, 80)}",
			Duration = 5,
			Icon = GetNotificationIcon(notification.Type)
		};

		// Route to the correct severity based on notification type
		switch (notification.Type)
		{
			case NotificationType.AddedToBoard:
				_messageService.Info(config);
				break;
			case NotificationType.AssignedToTask:
				_messageService.Warning(config);
				break;
			case NotificationType.CommentedOnTask:
				_messageService.Success(config);
				break;
			default:
				_messageService.Info(config);
				break;
		}
	}
}
```

**Hub endpoint:** `{Api:BaseUrl}/hubs/notification`

> **Design note:** `SetupNotificationHandler()` is called before `StartAsync()`. This ensures the `ReceiveNotification` subscription is active from the first message the server sends, preventing any race condition between connection establishment and handler registration.

---

## 2. Real-Time State Management

### 2.1 Handler Registration Pattern

Blazor components do not poll for updates. Instead, they register delegates with the SignalR service in `OnInitializedAsync`. When the server pushes a message the hub client fires the registered delegate, which mutates local component state and calls `StateHasChanged()` to trigger a re-render.

**Example — Chat component** (`Pages/Chat/Chat.razor.cs`):

```csharp
protected override async Task OnInitializedAsync()
{
	await LoadCurrentUserAsync();
	await ConnectSignalRAsync();  // connect + register all handlers
	_isInitialized = true;

	if (_organizationId.HasValue)
		await LoadChatsAsync();
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

	// Register handlers — each delegate mutates local state + triggers re-render
	ChatSignalRService.OnChatCreated(HandleChatUpsert);
	ChatSignalRService.OnChatUpdated(HandleChatUpsert);
	ChatSignalRService.OnChatMessageReceived(HandleChatMessage);
	ChatSignalRService.OnUserTyping(HandleUserTyping);
	ChatSignalRService.OnUserStoppedTyping(HandleUserStoppedTyping);
}
```

**Key handler events and their server-side SignalR method names:**

| Service | Server Method | Client Handler Method | Trigger |
|---|---|---|---|
| `ChatSignalRService` | `ReceiveChatMessage` | `OnChatMessageReceived` | New message sent to a chat |
| `ChatSignalRService` | `ChatCreated` | `OnChatCreated` | A new chat is created for the user |
| `ChatSignalRService` | `ChatUpdated` | `OnChatUpdated` | Chat metadata (name/members) changes |
| `ChatSignalRService` | `UserTyping` | `OnUserTyping` | Another member starts typing |
| `ChatSignalRService` | `UserStoppedTyping` | `OnUserStoppedTyping` | Another member stops typing |
| `SignalRService` | `BoardUpdated` | `OnBoardUpdated` | Board data changes (columns, cards) |
| `SignalRService` | `TaskUpdated` | `OnTaskUpdated` | An individual task is updated |
| `NotificationSignalRService` | `ReceiveNotification` | `OnNotificationReceived` | Any server-side notification event |

### 2.2 Notification Display via Ant Design

`NotificationSignalRService` uses a `RenderFragment` factory pattern to attach type-specific Ant Design `<Icon>` components to the toast message:

```csharp
private RenderFragment GetNotificationIcon(NotificationType type)
{
	return type switch
	{
		NotificationType.AddedToBoard => builder =>
		{
			builder.OpenComponent<Icon>(0);
			builder.AddAttribute(1, "Type", "team");
			builder.CloseComponent();
		},
		NotificationType.AssignedToTask => builder =>
		{
			builder.OpenComponent<Icon>(0);
			builder.AddAttribute(1, "Type", "user");
			builder.CloseComponent();
		},
		NotificationType.CommentedOnTask => builder =>
		{
			builder.OpenComponent<Icon>(0);
			builder.AddAttribute(1, "Type", "message");
			builder.CloseComponent();
		},
		_ => builder =>
		{
			builder.OpenComponent<Icon>(0);
			builder.AddAttribute(1, "Type", "notification");
			builder.CloseComponent();
		}
	};
}
```

This approach constructs the icon purely in C# using the Blazor render tree API, avoiding any `.razor` markup in the service layer.

### 2.3 Layout-Level Bootstrapping

Both `NotificationSignalRService` and `ChatSignalRService` are connected at the layout level (`Layouts/BasicLayout.razor.cs`) via `EnsureSignalRConnectionsAsync()`. This means the connections are established as soon as the authenticated shell renders — not lazily on first page visit:

```csharp
private async Task EnsureSignalRConnectionsAsync()
{
	if (_isConnectingSignalR) return;
	_isConnectingSignalR = true;

	var currentUser = await AuthService.GetCurrentUserAsync();
	if (currentUser == null || currentUser.Id == Guid.Empty)
	{
		// Disconnect and clean up if user is not authenticated
		await NotificationSignalRService.DisconnectAsync();
		await ChatSignalRService.DisconnectAsync();
		return;
	}

	_currentUserId = currentUser.Id;

	if (!_notificationHandlersRegistered)
	{
		NotificationSignalRService.OnNotificationReceived(HandleNotification);
		_notificationHandlersRegistered = true;
	}

	// Connect notification and chat hubs
	// ...
}
```

The `IsConnected` guard on every service prevents double-connection when the same service instance is later accessed by a feature component (e.g., `Chat.razor.cs`).

---

## 3. External Integrations Logic

### 3.1 Video Calls via WebRTC / SignalR Signalling (Board Meetings)

Board-level video meetings use a custom WebRTC signalling layer built on top of SignalR. The SignalR hub at `/webrtc` acts purely as a signalling server — it relays WebRTC offer/answer/ICE-candidate messages between peers. The actual media (audio/video/screen) flows peer-to-peer via the browser's WebRTC APIs.

**JavaScript interop** (`wwwroot/js/boardcall.js`):

The Blazor component (`BoardCallPage.razor.cs`) calls into a JavaScript module, `BoardCallInterop`, via `IJSRuntime`:

```csharp
// BoardCallPage.razor.cs — OnAfterRenderAsync (firstRender)
_objRef = DotNetObjectReference.Create(this);
await JS.InvokeVoidAsync("BoardCallInterop.init", BoardId, "localVideo", _objRef, user.Id);
```

Inside `boardcall.js`, `init` builds a SignalR connection to the WebRTC hub and acquires the local media stream:

```javascript
window.BoardCallInterop = {
	init: function (board, localVideoId, dotNetRef, realUserId) {
		boardId = board;
		dotNetObjRef = dotNetRef;
		userId = realUserId;

		// SignalR connection used exclusively as a WebRTC signalling channel
		srConnection = new signalR.HubConnectionBuilder()
			.withUrl("http://localhost:5071/webrtc")
			.configureLogging(signalR.LogLevel.Information)
			.build();

		srConnection.onclose(startSignalR);       // auto-reconnect
		srConnection.on("Receive", onSignalReceived); // handle SDP/ICE

		navigator.mediaDevices.getUserMedia({ video: true, audio: true })
			.then(stream => {
				localStream = stream;
				document.getElementById(localVideoId).srcObject = stream;
				startSignalR();
			});
	},

	startCall: function () {
		inCall = true;
		ensureJoinedBoardGroup().then(() => {
			// Announce presence to all peers in the board group
			sendSignal({ type: 'user-joined', userId, displayName: `...`, board: boardId });
			sendSignal({ type: 'request-users', userId, board: boardId });
		});
	}
};
```

**Callback bridge — JS → Blazor:**

The JavaScript module calls back into the C# component via `DotNetObjectReference` to update Blazor state:

```csharp
[JSInvokable]
public Task OnWebRtcConnected()
{
	_connectionReady = true;
	InvokeAsync(StateHasChanged);
	return Task.CompletedTask;
}

[JSInvokable]
public async Task AddRemoteUser(string userId, string displayName)
{
	// Resolve display name from the UserService API if needed
	var user = await UserService.GetByIdAsync(userId);
	_remoteUsers.Add(new RemoteUser { UserId = userId, DisplayName = user?.Username ?? displayName, ... });
	await InvokeAsync(StateHasChanged);
}

[JSInvokable]
public Task RemoveRemoteUser(string userId) { ... }

[JSInvokable]
public Task UpdateUserConnectionStatus(string userId, string status) { ... }

[JSInvokable]
public Task UpdateUserScreenShareStatus(string userId, bool isScreenSharing) { ... }
```

**Access control:** Before rendering the call page, `BoardCallPage` validates that the authenticated user is a member of the specified meeting:

```csharp
var meetingMembers = await MeetingMemberService.GetMeetingMembersByMeetingIdAsync(meetingGuid);
if (!meetingMembers.Any(m => m.UserId.ToString() == user!.Id.ToString()))
{
	_forbidden = true;
	NavigationManager.NavigateTo("/forbidden");
}
```

---

### 3.2 Video Calls via Daily.co Room URL (Chat Calls)

Chat-based calls follow a different integration model. The frontend asks the backend to provision a Daily.co room and returns a `RoomUrl`. The client then navigates to that URL (which renders the Daily.co in-browser experience).

**DTOs** (`Models/Chat/`):

```csharp
// Request sent to backend to start a call in a chat
public class StartChatCallRequestDto
{
	public Guid SenderId { get; set; }
}

// Response from backend containing the provisioned room URL
public class StartChatCallResponseDto
{
	public string RoomUrl { get; set; } = string.Empty;
	public ChatMessageDto? Message { get; set; }  // system message posted to the chat
}
```

**Service call** (`Services/ChatSystemService.cs`):

```csharp
public async Task<StartChatCallResponseDto> StartCallAsync(
	Guid chatId,
	StartChatCallRequestDto request,
	CancellationToken cancellationToken = default)
{
	return await _chatApi.StartCallAsync(chatId, request, cancellationToken);
}
```

**Component logic** (`Pages/Chat/Chat.razor.cs`):

```csharp
private async Task StartCallAsync()
{
	if (_activeChatId == null || _currentUserId == Guid.Empty || _isStartingCall)
		return;

	_isStartingCall = true;
	StateHasChanged();

	try
	{
		var request = new StartChatCallRequestDto { SenderId = _currentUserId };

		// Backend provisions the Daily.co room and returns its URL
		var response = await ChatService.StartCallAsync(_activeChatId.Value, request);

		// The backend also posts a system message to the chat with the room URL,
		// so other participants can join — handled via SignalR in HandleChatMessage
		if (response.Message != null)
			HandleChatMessage(response.Message);

		// Navigate the current user directly into the call room
		if (!string.IsNullOrWhiteSpace(response.RoomUrl))
			NavigateToCallRoom(response.RoomUrl);
	}
	finally
	{
		_isStartingCall = false;
		StateHasChanged();
	}
}

private void NavigateToCallRoom(string roomUrl)
{
	// Append the current user's display name as a query parameter
	var separator = roomUrl.Contains('?') ? "&" : "?";
	var callUrl = $"{roomUrl}{separator}userName={Uri.EscapeDataString(_currentUserName)}";
	NavigationManager.NavigateTo(callUrl, forceLoad: true);
}
```

**Flow summary:**

```
Chat component
	│
	├── POST /api/chats/{chatId}/call  (StartChatCallRequestDto)
	│       ↓ Backend creates Daily.co room
	│       ↓ Broadcasts system message to chat group via SignalR
	│       ↓ Returns StartChatCallResponseDto { RoomUrl, Message }
	│
	├── HandleChatMessage(response.Message)   → updates chat UI via StateHasChanged
	└── NavigationManager.NavigateTo(roomUrl) → navigates initiator to call room
												(other users receive the URL via SignalR chat message)
```

---

### 3.3 Google Calendar Synchronization

Google Calendar integration follows a standard OAuth 2.0 authorization code flow. All OAuth token management is handled server-side; the frontend only initiates the flow and triggers synchronization.

**Refit API interface** (`Interfaces/Api/IGoogleCalendarApi.cs`):

```csharp
public interface IGoogleCalendarApi
{
	// Step 1: Obtain the Google OAuth authorization URL from the backend
	[Get("/api/users/{userId}/google-calendar/auth-url")]
	Task<AuthUrlResponseDto> GetAuthorizationUrlAsync(Guid userId);

	// Step 2: Exchange the authorization code (returned by Google OAuth redirect)
	[Post("/api/google-calendar/connect")]
	Task ConnectAsync([Body] ConnectGoogleCalendarRequest request);

	// Step 3: Trigger task-to-calendar synchronization for a given month
	[Post("/api/users/{userId}/google-calendar/sync")]
	Task SyncCalendarAsync(Guid userId, [Body] SyncCalendarRequestDto dto);
}
```

**DTOs:**

```csharp
public class AuthUrlResponseDto
{
	public string Url { get; set; } = string.Empty; // Google OAuth consent screen URL
}

public class ConnectGoogleCalendarRequest
{
	public string Code { get; set; } = string.Empty; // Authorization code from OAuth callback
}

public class SyncCalendarRequestDto
{
	public DateTime Month { get; set; } // The target month to synchronize tasks into
}
```

**Service layer** (`Services/GoogleCalendarService.cs`):

```csharp
public class GoogleCalendarService : IGoogleCalendarService
{
	private readonly IGoogleCalendarApi _googleCalendarApi;

	// Retrieves the Google OAuth consent screen URL from the backend.
	// The backend constructs this URL using its own Google client credentials.
	public async Task<string> GetAuthorizationUrlAsync(Guid userId)
	{
		var response = await _googleCalendarApi.GetAuthorizationUrlAsync(userId);
		return response.Url;
	}

	// Sends the authorization code obtained from the OAuth redirect back to the backend.
	// The backend exchanges this code for access/refresh tokens and stores them server-side.
	public async Task ConnectAsync(string code)
	{
		await _googleCalendarApi.ConnectAsync(new ConnectGoogleCalendarRequest { Code = code });
	}

	// Instructs the backend to synchronize the user's TaskPilot tasks
	// into Google Calendar for the specified month.
	public async Task SyncCalendarAsync(Guid userId, DateTime month)
	{
		await _googleCalendarApi.SyncCalendarAsync(userId, new SyncCalendarRequestDto { Month = month });
	}
}
```

**OAuth flow from the frontend perspective:**

```
1. User clicks "Connect Google Calendar"
		│
		├── GetAuthorizationUrlAsync(userId)
		│       → GET /api/users/{userId}/google-calendar/auth-url
		│       ← { Url: "https://accounts.google.com/o/oauth2/auth?..." }
		│
		└── NavigationManager.NavigateTo(url, forceLoad: true)
				→ User redirected to Google consent screen
				→ After consent, Google redirects back to the app callback URL
						│
						└── ConnectAsync(code)
								→ POST /api/google-calendar/connect  { Code: "..." }
								← Backend stores tokens, returns 200

2. User clicks "Sync Calendar" for a selected month
		│
		└── SyncCalendarAsync(userId, selectedMonth)
				→ POST /api/users/{userId}/google-calendar/sync  { Month: "2025-06-01" }
				← Backend creates/updates Google Calendar events from TaskPilot tasks
```

> **Security note:** The frontend never handles Google OAuth tokens directly. The authorization code is passed immediately to the backend, which performs the token exchange. All subsequent API calls to Google are made server-side using stored refresh tokens.

---

*End of Section 3.3 — Communication and Integrations (Frontend Logic Layer)*
