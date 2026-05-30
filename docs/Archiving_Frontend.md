# Section 3.5 — Asynchronous Data Archiving: Logic Layer

This document describes the **logic layer** of the asynchronous data archiving feature in the TaskPilot Blazor WebAssembly frontend. It covers the Refit API client interfaces, the client-side service wrappers, and the state-management strategy that keeps active and archived items strictly separated. UI markup, layout, and styling are covered separately in Section 3.6.

---

## 1. Refit API Integration

TaskPilot uses [Refit](https://github.com/reactiveui/refit) to generate strongly-typed HTTP clients from interface declarations. Every archive-related HTTP call is defined as an interface method, keeping transport concerns isolated from business logic.

### 1.1 `IBoardApi` — Board Archive Endpoints

```csharp
// src/UI/Interfaces/Api/IBoardApi.cs

/// <summary>Moves a board into the archived state.</summary>
[Post("/api/boards/{boardId}/archive")]
Task ArchiveBoardAsync(
	string boardId,
	CancellationToken cancellationToken = default);

/// <summary>Restores a board from the archived state back to active.</summary>
[Post("/api/boards/{boardId}/dearchive")]
Task DearchiveBoardAsync(
	string boardId,
	CancellationToken cancellationToken = default);

/// <summary>Returns all boards that the given owner has archived.</summary>
[Get("/api/users/{ownerId}/boards/archived")]
Task<IEnumerable<BoardDto>> GetArchivedBoardsByOwnerAsync(
	Guid ownerId,
	CancellationToken cancellationToken = default);
```

**Design notes:**
- Both mutation endpoints use `POST` (not `DELETE`/`PUT`) to signal a state-transition intent on the server.
- `GetArchivedBoardsByOwnerAsync` returns the full `BoardDto` collection. Because the backend does not expose server-side pagination for archived boards, the client applies filtering and pagination itself (see Section 3).
- `CancellationToken` is threaded through to support component-level cancellation on navigation away.

---

### 1.2 `IBoardTaskApi` — Task Archive Endpoints

```csharp
// src/UI/Interfaces/Api/IBoardTaskApi.cs

/// <summary>Moves a single task into the archived state.</summary>
[Post("/api/tasks/{taskId}/archive")]
Task ArchiveAsync(Guid taskId);

/// <summary>Restores a single task to its active state.</summary>
[Post("/api/tasks/{taskId}/restore")]
Task RestoreAsync(Guid taskId);

/// <summary>
/// Returns a paginated, filtered list of archived tasks for a specific board.
/// Pagination is performed server-side for tasks.
/// </summary>
[Get("/api/tasks/archived")]
Task<List<ArchivedTaskDto>> SearchArchivedRangeTaskItemsAsync(
	int page,
	int pageSize,
	string searchTerm,
	Guid boardId);
```

**Design notes:**
- Task archiving uses a lighter `ArchivedTaskDto` for the archived list, carrying only the fields needed to render an archive row (`Id`, `Title`, `Assignee`, `DueDate`), reducing payload size compared to `TaskItemDto`.
- The `SearchArchivedRangeTaskItemsAsync` endpoint is server-side paginated (page + pageSize), unlike boards which perform client-side pagination.

---

## 2. Service Layer Logic

Refit interfaces are never injected into Blazor components directly. Instead, they are wrapped by service classes that implement domain-focused interfaces. This layer owns error handling, cache invalidation, and any client-side data transformations.

### 2.1 `BoardService` — Archive/Restore/Fetch for Boards

```csharp
// src/UI/Services/BoardService.cs

public async Task ArchiveBoardAsync(
	string boardId,
	CancellationToken cancellationToken = default)
{
	try
	{
		await _boardApi.ArchiveBoardAsync(boardId, cancellationToken);
	}
	catch (Exception)
	{
		throw; // propagate to let the component display the error
	}
}

public async Task DearchiveBoardAsync(
	string boardId,
	CancellationToken cancellationToken = default)
{
	try
	{
		await _boardApi.DearchiveBoardAsync(boardId, cancellationToken);
	}
	catch (Exception)
	{
		throw;
	}
}

public async Task<IEnumerable<BoardDto>> GetArchivedBoardsByOwnerAsync(
	Guid ownerId,
	CancellationToken cancellationToken = default)
{
	try
	{
		return await _boardApi.GetArchivedBoardsByOwnerAsync(ownerId, cancellationToken);
	}
	catch (Exception)
	{
		return []; // graceful degradation — return empty on failure
	}
}
```

#### Client-Side Pagination of Archived Boards

Because the backend `GET /api/users/{ownerId}/boards/archived` returns the full collection, `BoardService` implements its own pagination/filtering shim through `GetArchivedBoardsRangeForUserAsync`:

```csharp
public async Task<IEnumerable<BoardSearchDto>> GetArchivedBoardsRangeForUserAsync(
	Guid userId,
	string searchTerm,
	int page,
	int pageSize,
	CancellationToken cancellationToken = default)
{
	var archivedBoards = await GetArchivedBoardsByOwnerAsync(userId, cancellationToken);

	// In-memory filtering
	var filteredBoards = string.IsNullOrWhiteSpace(searchTerm)
		? archivedBoards
		: archivedBoards.Where(b =>
			(!string.IsNullOrEmpty(b.Name) && b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
			(!string.IsNullOrEmpty(b.Description) && b.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));

	// In-memory pagination
	var pagedBoards = filteredBoards
		.Skip((page - 1) * pageSize)
		.Take(pageSize);

	return pagedBoards.Select(b => new BoardSearchDto
	{
		Id          = b.Id,
		Name        = b.Name,
		Description = b.Description,
		OwnerId     = b.OwnerId,
		CreatedAt   = b.CreatedAt,
		UpdatedAt   = b.UpdatedAt,
		NumberOfMembers = 0,
		NumberOfTasks   = 0
	});
}
```

**Rationale:** The backend does not currently expose a paginated archived-board search endpoint, so the entire archived collection is fetched once and sliced in memory. `NumberOfMembers` and `NumberOfTasks` are zeroed-out because retrieving stats for each archived board individually would be expensive and those values are not shown in the archived list view.

---

### 2.2 `TaskService` — Archive/Restore/Fetch for Tasks

```csharp
// src/UI/Services/TaskService.cs

public async Task ArchiveAsync(Guid taskId)
{
	try
	{
		await _boardTaskApi.ArchiveAsync(taskId);
	}
	catch (Exception ex)
	{
		throw new Exception($"Failed to archive task: {ex.Message}", ex);
	}
}

public async Task RestoreAsync(Guid taskId)
{
	try
	{
		await _boardTaskApi.RestoreAsync(taskId);
	}
	catch (Exception ex)
	{
		throw new Exception($"Failed to restore task: {ex.Message}", ex);
	}
}

public async Task<List<ArchivedTaskDto>> SearchArchivedRangeTaskItemsAsync(
	int page,
	int pageSize,
	string searchTerm,
	Guid boardId)
{
	try
	{
		return await _boardTaskApi.SearchArchivedRangeTaskItemsAsync(page, pageSize, searchTerm, boardId);
	}
	catch (Exception ex)
	{
		throw new Exception($"Failed to search archived tasks: {ex.Message}", ex);
	}
}
```

**Error handling contract:**
- `TaskService` re-throws enriched exceptions so that the calling component can surface a meaningful message to the user.
- In contrast, `BoardService`'s read methods (`GetArchivedBoardsByOwnerAsync`) return empty collections on failure, matching the boards list's offline/cache-first design philosophy.

---

## 3. State Separation and Caching

The frontend keeps active and archived entities in completely separate logical channels. There is no shared in-memory list that is filtered by an `IsArchived` flag; the two states are fetched, stored, and displayed independently.

### 3.1 Board State Separation in `Boards` Page

The `Boards` component holds a single reactive list `_boards` and a `_filterType` string field:

```csharp
private List<BoardSearchDto> _boards = new();
private string _filterType = "all"; // "all" | "owner" | "member" | "archived"
```

`SearchBoards()` is the single entry point for all board queries. The filter type determines which backend path is called:

```csharp
if (_filterType == "archived")
{
	// Archived boards path — completely separate API call returning archived-only data
	results = await BoardService.GetArchivedBoardsRangeForUserAsync(
		userId, _searchTerm, _currentPage, _pageSize);
}
else
{
	// Active boards paths
	results = await BoardService.SearchBoardsRangeForUserAsync(
		userId, _selectedOrganizationId.Value, _searchTerm, _currentPage, _pageSize);
}
```

When a filter switch occurs (`OnFilterChanged`), `SearchBoards(reset: true)` is called, which **clears `_boards` and resets the page counter before fetching**:

```csharp
private async Task OnFilterChanged(string value)
{
	_filterType = value;
	await SearchBoards(reset: true);
}

private async Task SearchBoards(bool reset = false)
{
	if (reset)
	{
		_currentPage = 1;
		_boards.Clear();  // ← previous-filter data is discarded immediately
		_hasMoreData = true;
	}
	// ...fetch and populate _boards with results from the selected filter path
}
```

This means the UI **never co-mingles active and archived boards** in `_boards`. Switching from `"archived"` to `"all"` unconditionally discards the archived collection and fetches a fresh active-only set.

#### LocalStorage Cache

`BoardService` maintains a LocalStorage cache (`cached_boards_{userId}`) for the **active** boards list only. The archive flow does not write to this cache, ensuring that a cache hit never surfaces archived entries in the active boards view:

```csharp
// Cache is populated only during active-board fetches
await _localStorage.SetItemAsync($"{BOARDS_CACHE_KEY}_{userId}", boards);

// On archive success, BoardDetail navigates away and the cache is NOT updated —
// the next active-board fetch will miss the archived board and refetch from the server.
```

---

### 3.2 Task State Separation in `BoardDetail`

Active tasks are stored in `_boardDetail.Tasks` (`List<TaskItemDto>`), populated once during board load. There is no secondary "archived tasks" list on the board detail component; archived tasks are shown in a dedicated sub-page or modal that calls `SearchArchivedRangeTaskItemsAsync` independently.

When a user archives a task via `TaskDetailsModal`, the archive flow is:

**Step 1 — API call** (inside `TaskDetailsModal.razor.cs`):

```csharp
private async Task ArchiveTask()
{
	if (CurrentTask == null) return;

	try
	{
		_internalLoading = true;
		StateHasChanged();

		await TaskService.ArchiveAsync(Guid.Parse(CurrentTask.Id));

		await NotificationService.Success(new NotificationConfig
		{
			Message     = UI.Resources.I18n.Dashboard,
			Description = UI.Resources.I18n.TaskArchivedSuccess
		});

		// Notify parent component that this task was updated/archived
		if (OnTaskUpdated.HasDelegate)
			await OnTaskUpdated.InvokeAsync(CurrentTask);

		await HandleCancel(); // close the modal
	}
	catch (Exception) { /* silent — notification not shown on failure path */ }
	finally
	{
		_internalLoading = false;
		StateHasChanged();
	}
}
```

**Step 2 — Immediate removal from active list** (inside `BoardDetail.razor.cs`):

```csharp
private void HandleTaskUpdated(TaskItemDto updatedTask)
{
	if (_boardDetail?.Tasks != null)
	{
		var taskIndex = _boardDetail.Tasks.FindIndex(t => t.Id == updatedTask.Id);
		if (taskIndex >= 0)
		{
			_boardDetail.Tasks[taskIndex] = updatedTask;
			StateHasChanged();
		}
	}
}
```

> **Note:** `HandleTaskUpdated` replaces the task in the active list with the returned DTO. Because the backend marks the task as archived, the next board reload will not include it in the active task set. The task is effectively invisible in the active view from the moment the API call succeeds — no full page reload is required.

**Why no explicit `RemoveAll` for tasks?**  
Unlike `HandleTaskDeleted` (which calls `_boardDetail.Tasks.RemoveAll(...)` immediately), the archive handler currently relies on the updated DTO being returned and the server-side filtering excluding archived tasks on the next full reload. This is an intentional trade-off: the task stays in the local collection until the next board refresh, but the archive state is persisted on the server immediately.

---

**Board archiving** follows a simpler flow because a board cannot remain visible once archived. `BoardDetail` navigates the user directly to `/boards` upon a successful archive call, which forces a complete re-fetch of the active boards list:

```csharp
private async Task ArchiveBoard()
{
	if (_boardDetail == null) return;

	try
	{
		_isArchivingBoard = true;
		await BoardService.ArchiveBoardAsync(_boardDetail.Id);
		Message.Success(UI.Resources.I18n.BoardArchivedSuccess);
		Navigation.NavigateTo("/boards"); // ← triggers full active-board reload
	}
	catch (Exception ex)
	{
		Message.Error(string.Format(UI.Resources.I18n.FailedToArchiveBoard, ex.Message));
	}
	finally
	{
		_isArchivingBoard = false;
		_showArchiveBoardModal = false;
		StateHasChanged();
	}
}
```

Because `Boards.razor.cs` calls `SearchBoards(reset: true)` on `OnInitializedAsync`, the newly archived board is never included in the re-fetched active list — the backend already excludes it.

---

## Summary

| Concern | Boards | Tasks |
|---|---|---|
| Archive API verb | `POST /api/boards/{id}/archive` | `POST /api/tasks/{id}/archive` |
| Restore API verb | `POST /api/boards/{id}/dearchive` | `POST /api/tasks/{id}/restore` |
| Archived list endpoint | `GET /api/users/{ownerId}/boards/archived` (full collection) | `GET /api/tasks/archived` (server-side paginated) |
| Pagination of archived list | Client-side (`Skip`/`Take` in `BoardService`) | Server-side (`page` + `pageSize` params) |
| Active-list separation | `_filterType` gate + `_boards.Clear()` on filter change | Separate `_boardDetail.Tasks` list; archived list fetched independently |
| Post-archive active-list update | Navigate to `/boards` → full re-fetch | `HandleTaskUpdated` callback replaces DTO in-place; excluded on next reload |
| LocalStorage cache | Active boards cached; archive operations do not update the cache | No local cache for task lists |
