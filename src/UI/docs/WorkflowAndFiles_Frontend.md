# TaskPilot UI — Workflow Logic, API Integration & File Handling (Section 3.2)

## Overview

This document covers the **logic layer** of the TaskPilot Blazor WebAssembly frontend — specifically how the application integrates with the backend REST API using Refit, handles file attachments, and receives real-time state updates via SignalR. UI layout and component rendering are addressed separately in Section 3.6.

---

## 1. API Integration for Workflow

### 1.1 Refit Client Setup

All backend HTTP communication is performed through **Refit**-generated typed API clients. Each client interface is defined under `Interfaces/Api/` and registered in `Extensions/ServiceCollectionExtensions.cs` via `AddApiClientsAndServices`.

**Registration pattern (`ServiceCollectionExtensions.cs`):**

```csharp
var refitSettings = new RefitSettings
{
	ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	})
};

services.AddRefitClient<IBoardApi>(refitSettings)
	.ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
	.AddHttpMessageHandler<AuthenticationHandler>();

services.AddRefitClient<IBoardTaskApi>(refitSettings)
	.ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
	.AddHttpMessageHandler<AuthenticationHandler>();

// ... same pattern for all other API clients
```

Every client (except `ITaskPilotAuthApi`) passes through the `AuthenticationHandler`, which injects the Bearer token from `IAuthService` into each outgoing request.

**`AuthenticationHandler.cs`:**

```csharp
protected override async Task<HttpResponseMessage> SendAsync(
	HttpRequestMessage request, CancellationToken cancellationToken)
{
	var token = await _authService.GetAccessTokenAsync();
	if (!string.IsNullOrEmpty(token))
		request.Headers.Authorization =
			new AuthenticationHeaderValue("Bearer", token);

	return await base.SendAsync(request, cancellationToken);
}
```

### 1.2 Board Workflow API (`IBoardApi` / `BoardService`)

`IBoardApi` is the Refit interface for all board-level operations. `BoardService` wraps it and adds local-storage caching so that the UI remains functional during transient network failures.

**`IBoardApi` interface:**

```csharp
public interface IBoardApi
{
	[Get("/api/boards/{id}")]
	Task<BoardDto> GetByIdAsync(string id);

	[Post("/api/boards")]
	Task<string> CreateAsync([Body] CreateBoardRequest request);

	[Put("/api/boards/{id}")]
	Task UpdateAsync(string id, [Body] CreateBoardRequest request);

	[Delete("/api/boards/{id}")]
	Task DeleteAsync(string id);

	[Get("/api/boards/owner/search")]
	Task<IEnumerable<BoardSearchDto>> SearchBoardsRangeForOwnerAsync(
		[Query] Guid ownerId, [Query] Guid organizationId,
		[Query] string searchTerm, [Query] int page, [Query] int pageSize);

	[Post("/api/boards/{boardId}/archive")]
	Task ArchiveBoardAsync(string boardId, CancellationToken cancellationToken = default);

	[Post("/api/boards/{boardId}/dearchive")]
	Task DearchiveBoardAsync(string boardId, CancellationToken cancellationToken = default);
}
```

**Caching strategy in `BoardService`:**

```csharp
public async Task<List<BoardDto>> GetBoardsAsync(string userId)
{
	try
	{
		var boards = await _userApi.GetBoardsAsync(userId);
		// Write-through to localStorage on success
		await _localStorage.SetItemAsync($"cached_boards_{userId}", boards);
		return boards;
	}
	catch (ApiException)
	{
		// Fall back to localStorage on API failure
		var cachedBoards = await _localStorage
			.GetItemAsync<List<BoardDto>>($"cached_boards_{userId}");
		return cachedBoards ?? new List<BoardDto>();
	}
}
```

`GetWithStatsAsync` composes data from multiple services (tasks, members, auth) and caches the assembled `BoardWithStats` object under a per-board key (`cached_board_stats_{boardId}`).

### 1.3 Task Workflow API (`IBoardTaskApi` / `TaskService`)

`IBoardTaskApi` covers the full task lifecycle. `TaskService` is a thin wrapper that translates exceptions into user-readable messages.

**`IBoardTaskApi` interface:**

```csharp
public interface IBoardTaskApi
{
	[Get("/api/boards/{boardId}/tasks")]
	Task<List<TaskItemDto>> GetBoardTasksAsync(string boardId);

	[Get("/api/tasks/{taskId}")]
	Task<TaskItemDto> GetByIdAsync(string taskId);

	[Post("/api/tasks")]
	Task<string> CreateAsync([Body] CreateTaskRequest request);

	[Put("/api/tasks/{taskId}")]
	Task UpdateAsync(string taskId, [Body] UpdateTaskRequest request);

	[Delete("/api/tasks/{taskId}")]
	Task DeleteAsync(string taskId);

	[Post("/api/tasks/{taskId}/archive")]
	Task ArchiveAsync(Guid taskId);

	[Post("/api/tasks/{taskId}/restore")]
	Task RestoreAsync(Guid taskId);

	[Get("/api/tasks/archived")]
	Task<List<ArchivedTaskDto>> SearchArchivedRangeTaskItemsAsync(
		int page, int pageSize, string searchTerm, Guid boardId);

	[Get("/api/tasks/calendar")]
	Task<List<TaskCalendarItemDto>> GetForCalendarMonthAsync(
		Guid userId, DateTime dayInMonth);
}
```

**`TaskItemDto` model:**

```csharp
public class TaskItemDto
{
	public string Id { get; set; }
	public string BoardId { get; set; }
	public string Title { get; set; }
	public string? Description { get; set; }
	public int StateId { get; set; }
	public string? AssigneeId { get; set; }
	public int? TagId { get; set; }
	public int Priority { get; set; }   // 1=Low, 2=Medium, 3=High
	public string? DueDate { get; set; }
	public string CreatedAt { get; set; }
	public string UpdatedAt { get; set; }
}
```

### 1.4 Board State API (`IBoardStateApi` / `TaskStateService`)

Workflow columns (states) are managed via `IBoardStateApi`. The order of states can be swapped atomically using `SwapOrderAsync`, which drives the drag-and-drop column reordering feature.

```csharp
public interface IBoardStateApi
{
	[Get("/api/boards/{boardId}/states")]
	Task<List<StateDto>> GetBoardStatesAsync(string boardId);

	[Post("/api/boards/{boardId}/states")]
	Task<int> CreateAsync(string boardId, [Body] CreateStateRequest request);

	[Put("/api/states/{id}")]
	Task UpdateAsync(int id, [Body] UpdateStateRequest request);

	[Delete("/api/states/{id}")]
	Task DeleteAsync(int id);

	[Post("/api/boards/{boardId}/states/swap-order")]
	Task SwapOrderAsync(string boardId, [Body] SwapStateOrderRequest request);
}
```

### 1.5 Full API Client Inventory

| Refit Interface | Backend Hub | Primary Service Consumer |
|---|---|---|
| `IBoardApi` | `/api/boards` | `BoardService` |
| `IBoardTaskApi` | `/api/tasks`, `/api/boards/{id}/tasks` | `TaskService` |
| `IBoardStateApi` | `/api/boards/{id}/states`, `/api/states` | `TaskStateService` |
| `IBoardMemberApi` | `/api/boards/{id}/members` | `BoardMemberService` |
| `IAttachmentApi` | `/api/attachments` | `AttachmentService` |
| `IAvatarApi` | `/api/avatars` | `AvatarService` |
| `ICommentApi` | `/api/comments` | `CommentService` |
| `ITagApi` | `/api/tags` | `TagService` |
| `INotificationApi` | `/api/notifications` | `NotificationService` |
| `IUserApi` | `/api/users` | `UserService` |
| `IOrganizationApi` | `/api/organizations` | `OrganizationService` |
| `IInvitationApi` | `/api/invitations` | `InvitationService` |
| `IChatApi` / `IChatSystemApi` | `/api/chats` | `ChatService` / `ChatSystemService` |
| `IMeetingApi` / `IMeetingMemberApi` | `/api/meetings` | `MeetingService` / `MeetingMemberService` |
| `IGoogleCalendarApi` | `/api/google-calendar` | `GoogleCalendarService` |
| `ITaskPilotAuthApi` | `/api/auth` | `AuthService` (no auth handler) |

---

## 2. File Upload Logic

### 2.1 Architecture Overview

File uploads in TaskPilot follow a **server-proxied multipart** model. The frontend does **not** obtain SAS tokens or communicate with Azure Blob Storage directly. Instead, files are sent to the TaskPilot backend as multipart form data via the Refit `[Multipart]` attribute, and the backend is responsible for forwarding the content to Azure Blob Storage and returning the resulting metadata (URL, file name, etc.) to the client.

> **Note:** No client-side SAS token acquisition or direct Azure Blob Storage URL construction has been identified in the current frontend codebase. The frontend exclusively interacts with the backend's `/api/attachments` and `/api/avatars` endpoints.

### 2.2 Attachment Upload (`IAttachmentApi` / `AttachmentService`)

Attachments are tied to an `entityId` (a `Guid` representing a task or comment). The upload produces an `AttachmentDto` containing the publicly accessible `Url` returned by the backend.

**`IAttachmentApi` interface:**

```csharp
public interface IAttachmentApi
{
	[Get("/api/attachments/{entityId}")]
	Task<List<AttachmentDto>> GetAsync(Guid entityId, [Query] Guid userId);

	[Multipart]
	[Post("/api/attachments/{entityId}")]
	Task<AttachmentDto> UploadAsync(Guid entityId, [AliasAs("file")] StreamPart file);

	[Delete("/api/attachments/{fileName}")]
	Task DeleteAsync(string fileName);
}
```

**`AttachmentService.UploadAsync`:**

```csharp
public async Task<AttachmentDto> UploadAsync(
	Guid entityId, Stream fileStream, string fileName)
{
	// Wrap the raw stream in Refit's StreamPart for multipart encoding
	var streamPart = new StreamPart(fileStream, fileName);
	return await _attachmentApi.UploadAsync(entityId, streamPart);
}
```

**`AttachmentDto` returned by the backend:**

```csharp
public class AttachmentDto
{
	public Guid Id { get; set; }
	public string FileName { get; set; }
	public string Url { get; set; }          // Direct URL (served by backend or CDN)
	public Guid EntityId { get; set; }
	public DateTime UploadedAt { get; set; }
	public string? UploadedBy { get; set; }
	public string? ContentType { get; set; }
	public long? Size { get; set; }
}
```

**Upload sequence:**

```
Component (IBrowserFile)
		│
		│  OpenReadStream()
		▼
AttachmentService.UploadAsync(entityId, stream, fileName)
		│
		│  new StreamPart(stream, fileName)
		▼
IAttachmentApi  [Multipart] POST /api/attachments/{entityId}
  + AuthenticationHandler → Bearer token injected
		│
		▼
   Backend API  →  Azure Blob Storage
		│
		▼
   AttachmentDto (Url, FileName, ContentType, Size)
		│
		▼
   Component updates local state
```

### 2.3 Avatar Upload (`IAvatarApi` / `AvatarService`)

User profile pictures follow the same multipart pattern via `IAvatarApi`. The service exposes distinct `UploadAsync` (initial creation) and `UpdateAsync` (replacement) methods, both accepting a raw `Stream`.

```csharp
public async Task<AvatarDto> UploadAsync(
	Guid userId, Stream fileStream, string fileName)
{
	var streamPart = new StreamPart(fileStream, fileName);
	return await _avatarApi.UploadAsync(userId, streamPart);
}

public async Task<AvatarDto> UpdateAsync(
	Guid userId, Stream fileStream, string fileName)
{
	var streamPart = new StreamPart(fileStream, fileName);
	return await _avatarApi.UpdateAsync(userId, streamPart);
}
```

### 2.4 Client-Side Image Optimization

No client-side image resizing or compression logic has been implemented in the current frontend codebase. File streams are forwarded to the backend without pre-processing. Any image optimization (resizing, format conversion, compression) is expected to be handled server-side before storage in Azure Blob Storage.

---

## 3. Real-Time State Updates via SignalR

### 3.1 SignalR Service Architecture

The frontend uses three independent SignalR services, each connecting to a dedicated hub on the backend:

| Service | Interface | Hub URL | Purpose |
|---|---|---|---|
| `SignalRService` | `ISignalRService` | `/hubs/board` | Board and task live updates |
| `NotificationSignalRService` | `INotificationSignalRService` | `/hubs/notification` | In-app notification toasts |
| `ChatSignalRService` | `IChatSignalRService` | `/hubs/chat` | Real-time messaging, typing indicators |

All three services use `.WithAutomaticReconnect()` on the `HubConnectionBuilder`, ensuring transparent reconnection after transient network disruptions without any component-level retry logic.

All three services are registered in the DI container as **scoped** (or as a factory for `INotificationSignalRService`) in `ServiceCollectionExtensions.cs`.

### 3.2 Board & Task Updates (`SignalRService`)

`SignalRService` manages the connection lifecycle and group membership for board-scoped updates.

**Connection and group lifecycle (`BoardDetail.razor.cs`):**

```csharp
protected override async Task OnInitializedAsync()
{
	await LoadCurrentUser();
	await LoadBoardDetail();

	// Connect to the board hub and join the group for this board
	await SignalRService.ConnectAsync();
	await SignalRService.JoinBoardGroupAsync(BoardId);

	// Register handler: when the server pushes "BoardUpdated",
	// re-fetch the full board detail and trigger a re-render
	SignalRService.OnBoardUpdated(async payload =>
	{
		await InvokeAsync(async () => await LoadBoardDetail());
	});
}

public void Dispose()
{
	_ = SignalRService.LeaveBoardGroupAsync(BoardId);
	_ = SignalRService.DisconnectAsync();
}
```

**Handler registration in `SignalRService`:**

```csharp
public void OnBoardUpdated(Action<object> handler)
	=> _hubConnection?.On("BoardUpdated", handler);

public void OnTaskUpdated(Action<object> handler)
	=> _hubConnection?.On("TaskUpdated", handler);
```

**State update flow:**

```
Backend emits "BoardUpdated" on /hubs/board
		│
		▼
SignalRService.OnBoardUpdated callback fires
		│
		│  InvokeAsync (marshals to Blazor render thread)
		▼
BoardService.GetDetailAsync() — fresh HTTP fetch
		│
		▼
_boardDetail updated in component
		│
		▼
StateHasChanged() — Blazor re-renders affected sub-tree
```

The component also supports **group-level scoping**: `JoinTaskGroupAsync` / `LeaveTaskGroupAsync` allow subscribing only to updates for a specific open task (e.g., inside the `TaskDetailsModal`), reducing unnecessary re-renders.

### 3.3 Notification Toasts (`NotificationSignalRService`)

`NotificationSignalRService` connects to `/hubs/notification` and subscribes to the `ReceiveNotification` server event. On receipt, it uses AntDesign's `IMessageService` to display a contextual toast — no component re-render or local state mutation is required.

```csharp
private void SetupNotificationHandler()
{
	_hubConnection?.On<Notification>("ReceiveNotification", (notification) =>
	{
		ShowNotificationMessage(notification);
	});
}

private void ShowNotificationMessage(Notification notification)
{
	// Notification type drives the toast severity
	switch (notification.Type)
	{
		case NotificationType.AddedToBoard:
			_messageService.Info(config);      break;
		case NotificationType.AssignedToTask:
			_messageService.Warning(config);   break;
		case NotificationType.CommentedOnTask:
			_messageService.Success(config);   break;
		default:
			_messageService.Info(config);      break;
	}
}
```

Notification text is capped at 80 characters client-side before display. User-group targeting (i.e., only the intended recipient receives the notification) is managed server-side via `JoinUserGroupAsync`.

### 3.4 Chat Real-Time Updates (`ChatSignalRService`)

`ChatSignalRService` connects to `/hubs/chat` and exposes callbacks for message receipt, chat creation/update events, and typing indicators:

```csharp
public void OnChatMessageReceived(Action<ChatMessageDto> handler)
	=> _hubConnection?.On("ReceiveChatMessage", handler);

public void OnChatCreated(Action<ChatDto> handler)
	=> _hubConnection?.On("ChatCreated", handler);

public void OnChatUpdated(Action<ChatDto> handler)
	=> _hubConnection?.On("ChatUpdated", handler);

public void OnUserTyping(Action<ChatTypingEvent> handler)
	=> _hubConnection?.On("UserTyping", handler);

public void OnUserStoppedTyping(Action<ChatTypingEvent> handler)
	=> _hubConnection?.On("UserStoppedTyping", handler);
```

**Typing indicator server invocations:**

```csharp
public async Task StartTypingAsync(string chatId, string userId)
	=> await _hubConnection.InvokeAsync("StartTyping", chatId, userId);

public async Task StopTypingAsync(string chatId, string userId)
	=> await _hubConnection.InvokeAsync("StopTyping", chatId, userId);
```

Chat components subscribe to `OnChatMessageReceived` and append new `ChatMessageDto` objects directly to their local message list, calling `StateHasChanged()` to re-render without fetching the full message history again.

### 3.5 SignalR Connection State Summary

```
Application Start
	│
	├─ NotificationSignalRService.ConnectAsync()  →  /hubs/notification
	│       └─ JoinUserGroupAsync(userId)
	│
	├─ ChatSignalRService.ConnectAsync()          →  /hubs/chat
	│       └─ JoinUserGroupAsync(userId)
	│           └─ JoinChatGroupAsync(chatId) [per open chat]
	│
	└─ SignalRService.ConnectAsync()              →  /hubs/board
			└─ JoinBoardGroupAsync(boardId)   [on BoardDetail mount]
				└─ JoinTaskGroupAsync(taskId) [on task detail open, optional]
```

Each service is disposed through `IAsyncDisposable`, ensuring hub connections are cleanly closed when the component owning them is torn down.

---

## 4. Key Source Files

| File | Role |
|---|---|
| `Interfaces/Api/IBoardApi.cs` | Refit contract for board CRUD and archive operations |
| `Interfaces/Api/IBoardTaskApi.cs` | Refit contract for task lifecycle (create, update, archive, calendar) |
| `Interfaces/Api/IBoardStateApi.cs` | Refit contract for workflow column management |
| `Interfaces/Api/IAttachmentApi.cs` | Refit contract for multipart file upload/delete |
| `Interfaces/Api/IAvatarApi.cs` | Refit contract for user avatar upload/update |
| `Services/BoardService.cs` | Board logic with LocalStorage write-through cache |
| `Services/TaskService.cs` | Task CRUD delegating to `IBoardTaskApi` |
| `Services/AttachmentService.cs` | File upload wrapper using `StreamPart` |
| `Services/AvatarService.cs` | Avatar upload wrapper using `StreamPart` |
| `Services/SignalRService.cs` | Board hub client; board/task group subscription |
| `Services/NotificationSignalRService.cs` | Notification hub client; toast display via AntDesign |
| `Services/ChatSignalRService.cs` | Chat hub client; messaging and typing indicators |
| `Handlers/AuthenticationHandler.cs` | `DelegatingHandler` injecting Bearer tokens into all Refit requests |
| `Extensions/ServiceCollectionExtensions.cs` | DI registration for all Refit clients, services, and SignalR services |
