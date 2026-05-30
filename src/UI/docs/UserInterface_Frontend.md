# 3.6 User Interface – Blazor WebAssembly Frontend

This document provides a technical description of the Blazor WebAssembly user interface implemented in the `UI` project. It covers the component architecture, routing strategy, UI library integration, state management patterns, authentication-aware rendering, and internationalization.

---

## 3.6.1 Component Architecture & Routing

### Application Bootstrap

The application entry point is `Program.cs`. It creates the Blazor WebAssembly host, loads runtime configuration from `appsettings.json`, registers all services, and initialises the authentication state before starting the runtime loop.

```csharp
// src/UI/Program.cs
public static async Task Main(string[] args)
{
	var builder = WebAssemblyHostBuilder.CreateDefault(args);
	builder.RootComponents.Add<App>("#app");

	var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
	var stream = await http.GetStreamAsync("appsettings.json");
	builder.Configuration.AddJsonStream(stream);

	builder.Services.AddApiClientsAndServices(builder.Configuration);

	var host = builder.Build();

	// Eagerly restore the stored JWT and user profile before the first render
	var authService = host.Services.GetRequiredService<IAuthService>();
	await authService.InitializeAsync();

	await host.RunAsync();
}
```

### `App.razor` – Root Component

`App.razor` is the root component mounted at the `#app` DOM element. It provides the `IGlobalLoadingService` as a cascading value so that any descendant component can subscribe to the global loading state. It also hosts the Ant Design `<Message />` container (for toast notifications) and the `<GlobalLoadingComponent />` overlay.

```razor
<!-- src/UI/App.razor -->
@namespace UI
@inject IGlobalLoadingService LoadingService

<CascadingValue Value="LoadingService">
	<Routes />
	<Message />
	<GlobalLoadingComponent />
</CascadingValue>
```

### `Routes.razor` – Router Configuration

The router is configured in `Routes.razor`. All pages that declare a `@page` directive are discovered automatically from the entry assembly. The default layout is `BasicLayout`. The `<AntContainer />` component is required by Ant Design Blazor to render portal-based overlays (modals, drawers, tooltips).

```razor
<!-- src/UI/Routes.razor -->
<Router AppAssembly="typeof(Program).Assembly">
	<Found Context="routeData">
		<CascadingValue Value="routeData">
			<RouteView RouteData="routeData" DefaultLayout="typeof(UI.Layouts.BasicLayout)" />
		</CascadingValue>
		<FocusOnNavigate RouteData="routeData" Selector="h1" />
	</Found>
</Router>

<AntContainer />
```

### `BasicLayout.razor` – Shell Layout

`BasicLayout` wraps all authenticated pages in the Ant Design Pro `<BasicLayout>` shell, which provides the collapsible side-navigation, header, and footer regions. Notification toast components are embedded here so they are available on every page. The layout inherits `LayoutComponentBase` and renders the current page via `@Body`.

```razor
<!-- src/UI/Layouts/BasicLayout.razor (excerpt) -->
<AntDesign.ProLayout.BasicLayout Logo="@("pro_icon.svg")"
	Title="@("TaskPilot")"
	MenuData="_menuData"
	MenuAccordion
	@bind-Collapsed="collapsed">
	<ChildContent>
		<NotificationToast IsVisible=@showNotification
						   Message=@notificationMessage
						   NotificationType=@notificationType
						   OnClick=@(() => Navigation.NavigateTo("/notifications"))
						   OnClose=@HideNotification />
		@Body
	</ChildContent>
	<FooterRender>
		<FooterView Copyright="2025 bezshumu" Links="Links" />
	</FooterRender>
</AntDesign.ProLayout.BasicLayout>
```

The navigation menu is built dynamically in the code-behind using localised strings from the `I18n` resource class. Unread counts for notifications, chats, and pending invitations are appended to the relevant menu item labels:

```csharp
// src/UI/Layouts/BasicLayout.razor.cs (excerpt)
new MenuDataItem
{
	Path = "/notifications",
	Name = _unreadNotificationsCount > 0
		? $"{UI.Resources.I18n.NotificationsMenu} ({_unreadNotificationsCount})"
		: UI.Resources.I18n.NotificationsMenu,
	Key = "notifications",
	Icon = "bell"
},
new MenuDataItem
{
	Path = "/invitations",
	Name = _invitationsCount > 0
		? string.Format(UI.Resources.I18n.InvitationsMenuWithCount, _invitationsCount)
		: UI.Resources.I18n.InvitationsMenu,
	Key = "invitations",
	Icon = "mail"
},
```

### Page Routing

Every page component declares its route with the `@page` directive. A page may declare multiple routes:

```razor
@page "/"
@page "/boards"
```

Route parameters are bound directly to `[Parameter]` properties in the code-behind:

```csharp
// src/UI/Pages/Board/BoardDetail.razor.cs
[Parameter] public string BoardId { get; set; } = string.Empty;
[SupplyParameterFromQuery(Name = "taskId")] public string? TaskId { get; set; }
```

### Code-Behind Pattern (`.razor` / `.razor.cs` Separation)

All non-trivial components follow the partial-class code-behind pattern. The `.razor` file contains only the template markup; all C# logic lives in the corresponding `.razor.cs` partial class. This separation keeps templates readable and logic unit-testable.

```
Pages/Board/
├── BoardDetail.razor          ← template (markup only)
└── BoardDetail.razor.cs       ← partial class (logic, lifecycle, DI)
```

The partial class inherits `ComponentBase` (or `BaseComponentWithLoading` where global loading is needed) and uses `[Inject]` attributes for dependency injection:

```csharp
// src/UI/Pages/Board/BoardDetail.razor.cs (excerpt)
public partial class BoardDetail : ComponentBase, IDisposable
{
	[Parameter] public string BoardId { get; set; } = string.Empty;

	[Inject] private IBoardService BoardService { get; set; } = default!;
	[Inject] private IAuthService AuthService { get; set; } = default!;
	[Inject] private NavigationManager Navigation { get; set; } = default!;
	[Inject] private IMessageService Message { get; set; } = default!;
	...
}
```

### `BaseComponentWithLoading` – Shared Base Class

A reusable abstract base class subscribes to the global loading event and triggers `StateHasChanged()` automatically, so pages automatically reflect the loading spinner without boilerplate:

```csharp
// src/UI/Components/Base/BaseComponentWithLoading.cs
public abstract class BaseComponentWithLoading : ComponentBase, IDisposable
{
	[CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;

	protected bool IsLoading => LoadingService?.IsLoading ?? false;

	protected override void OnInitialized()
	{
		if (LoadingService != null)
			LoadingService.OnLoadingChanged += StateHasChanged;

		base.OnInitialized();
	}

	public virtual void Dispose()
	{
		if (LoadingService != null)
			LoadingService.OnLoadingChanged -= StateHasChanged;
	}
}
```

---

## 3.6.2 UI Library Integration – Ant Design Blazor

The project uses [Ant Design Blazor](https://antblazor.com/) (`AntDesign` NuGet package) along with the `AntDesign.ProLayout` package for the Pro-style application shell. All interactive UI primitives (modals, forms, tables, selects, tags, buttons, icons, spinners) come from this library.

### Service Registration

```csharp
// src/UI/Extensions/ServiceCollectionExtensions.cs (excerpt)
services.AddAntDesign();
services.Configure<ProSettings>(options => { ... });
```

### Modals – `TaskDetailsModal`

The `TaskDetailsModal` is the most complex UI element in the application. It renders a two-column modal: the left column shows task details or an edit form, and the right column shows the comment thread.

```razor
<!-- src/UI/Pages/Board/Components/TaskDetailsModal.razor -->
<Modal Title=@(CurrentTask != null
			   ? string.Format(UI.Resources.I18n.TaskDetailsTitle, CurrentTask.Title)
			   : UI.Resources.I18n.TaskDetailsTitle)
	   Visible=@IsVisible
	   OnOk=@HandleOk
	   OnCancel=@HandleCancel
	   ConfirmLoading=@IsLoading
	   Width="1200"
	   Footer=@GetModalFooter()>

	@if (CurrentTask != null)
	{
		<AntDesign.Row Gutter="16" style="min-height:400px; max-height:60vh; overflow:hidden;">
			<AntDesign.Col xs="24" sm="24" md="8" lg="8" xl="8" style="height:100%; overflow:auto;">
				@if (IsEditing)
				{
					<TaskEditMode FormModel=@FormModel
								  States=@States
								  BoardMembers=@BoardMembers
								  AllUsers=@AllUsers
								  CanManageTask=@CanManageTask
								  OnFormSubmit=@HandleSubmit
								  OnFormSubmitFailed=@HandleSubmitFailed
								  Tags=@Tags />
				}
				else
				{
					<TaskViewMode TaskId=@CurrentTask.Id
								  States=@States
								  AllUsers=@AllUsers
								  Tags=@Tags />
					<TaskQuickActions States=@States
									  CurrentStateId=@CurrentTask.StateId
									  IsLoading=@(IsLoading || _internalLoading)
									  OnStateChange=@MoveTaskToState />
				}
			</AntDesign.Col>
			<AntDesign.Col xs="24" sm="24" md="16" lg="16" xl="16"
						   style="height:100%; border-left:1px solid #f0f0f0; background:#fafafa; overflow:auto;">
				@if (!IsEditing)
				{
					<TaskCommentsComponent TaskId=@CurrentTask.Id
										  AllUsers=@AllUsers
										  CanAddComment=@CanManageTask
										  CurrentUserId=@CurrentUserId />
				}
			</AntDesign.Col>
		</AntDesign.Row>
	}
	else
	{
		<Spin Size=@SpinSize.Large Tip=@UI.Resources.I18n.LoadingTaskDetails>
			<div style="height: 200px;" />
		</Spin>
	}
</Modal>
```

The component parameters controlling visibility, loading state, and data are declared in the code-behind as `[Parameter]` properties:

```csharp
// src/UI/Pages/Board/Components/TaskDetailsModal.razor.cs (excerpt)
public partial class TaskDetailsModal : ComponentBase
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public bool IsLoading { get; set; }
	[Parameter] public TaskItemDto? CurrentTask { get; set; }
	[Parameter] public List<StateDto> States { get; set; } = new();
	[Parameter] public List<BoardMemberDto> BoardMembers { get; set; } = new();
	[Parameter] public bool CanManageTask { get; set; }
	[Parameter] public List<TagDto> Tags { get; set; } = new();
	[Parameter] public EventCallback<TaskItemDto> OnTaskUpdated { get; set; }
	[Parameter] public EventCallback<string> OnTaskDeleted { get; set; }
	[Parameter] public EventCallback OnCancel { get; set; }

	[Inject] private ITaskService TaskService { get; set; } = default!;
	[Inject] private NotificationService NotificationService { get; set; } = default!;
	...
}
```

### Forms with Validation – `TaskEditMode`

The `<Form>` component from Ant Design Blazor binds to a model object and supports declarative layout and per-field validation:

```razor
<!-- src/UI/Pages/Board/Components/TaskEditMode.razor (excerpt) -->
<Form Model=@FormModel
	  LabelCol="new ColLayoutParam { Span = 6 }"
	  WrapperCol="new ColLayoutParam { Span = 18 }"
	  OnFinish=@OnFormSubmit
	  OnFinishFailed=@OnFormSubmitFailed>

	<FormItem Label=@UI.Resources.I18n.TaskTitleLabel Required>
		<Input @bind-Value=@FormModel.Title
			   Placeholder=@UI.Resources.I18n.EnterTaskTitle
			   MaxLength="200" />
	</FormItem>

	<FormItem Label=@UI.Resources.I18n.DescriptionLabel>
		<TextArea @bind-Value=@FormModel.Description
				  Placeholder=@UI.Resources.I18n.EnterTaskDescription
				  Rows="4"
				  MaxLength="1000" />
	</FormItem>

	<FormItem Label=@UI.Resources.I18n.States Required>
		<Select @bind-Value=@FormModel.StateId
				TItemValue="int" TItem="int"
				Style="width: 100%;"
				Placeholder=@UI.Resources.I18n.SelectStatePlaceholder>
			<SelectOptions>
				@foreach (var state in States)
				{
					<SelectOption TItemValue="int" TItem="int"
								  Value=@state.Id Label=@state.Name />
				}
			</SelectOptions>
		</Select>
	</FormItem>

	<FormItem Label="Priority" Required>
		<Select @bind-Value=@FormModel.Priority TItemValue="int" TItem="int" Style="width: 100%;">
			<SelectOptions>
				<SelectOption TItemValue="int" TItem="int" Value="1" Label=@UI.Resources.I18n.PriorityLow />
				<SelectOption TItemValue="int" TItem="int" Value="2" Label=@UI.Resources.I18n.PriorityNormal />
				<SelectOption TItemValue="int" TItem="int" Value="3" Label=@UI.Resources.I18n.PriorityHigh />
				<SelectOption TItemValue="int" TItem="int" Value="4" Label=@UI.Resources.I18n.PriorityImmediate />
			</SelectOptions>
		</Select>
	</FormItem>
</Form>
```

### Kanban Board – Data Grid / Column View

The Kanban board is implemented in `BoardColumns.razor`. Each workflow state is rendered as a column; tasks within each column are displayed as hoverable `<Card>` components. A `<Badge>` shows the task count per state.

```razor
<!-- src/UI/Pages/Board/Components/BoardColumns.razor (excerpt) -->
@foreach (var state in BoardDetail.States.OrderBy(s => s.Order))
{
	<div class="state-column">
		<div class="state-header">
			<Text Strong>@state.Name</Text>
			<Badge Count=@GetTaskCountForState(state.Id)
				   Style="background-color: rgb(58, 143, 234);" />
		</div>
		<div class="task-container" style="max-height: 600px; overflow-y: auto;">
			@foreach (var task in GetTasksForState(state.Id))
			{
				<div class="task-item-wrapper" @onclick=@(() => OnTaskClick.InvokeAsync(task))>
					<Card Size=@CardSize.Small Class="task-item" Hoverable>
						<Body>
							<Text Strong>@task.Title</Text>
							<Tag Color=@GetPriorityColor(task.Priority) Icon="exclamation-circle">
								@GetPriorityLabel(task.Priority)
							</Tag>
						</Body>
					</Card>
				</div>
			}
		</div>
	</div>
}
```

### Boards Grid – Responsive Card List

The boards list page uses an Ant Design responsive `<Row>` / `<Col>` grid to display board cards:

```razor
<!-- src/UI/Pages/Boards/Boards.razor (excerpt) -->
<Row Gutter="(16, 16)" Style="margin-top: 24px;">
	@foreach (var board in _boards)
	{
		<AntDesign.Col Xxl="6" Xl="8" Lg="8" Md="12" Sm="12" Xs="24">
			<BoardSearchCard Board=@board
							 IsOwner=@IsCurrentUserOwner(board.OwnerId)
							 IsArchived=@(_filterType == "archived")
							 OnBoardClick=@HandleBoardClick
							 OnEditBoard=@HandleEditBoard
							 OnDeleteBoard=@HandleDeleteBoard />
		</AntDesign.Col>
	}
</Row>
```

### Notification / Message Service

The `IMessageService` (injected from Ant Design Blazor) is used throughout the codebase to show transient success, warning, and error toasts:

```csharp
// Example usage in BoardDetail.razor.cs
[Inject] private IMessageService Message { get; set; } = default!;

await Message.Success(UI.Resources.I18n.BoardArchivedSuccessfully);
await Message.Error(UI.Resources.I18n.FailedToArchiveBoard);
```

A custom `NotificationToast` component is embedded in `BasicLayout` to surface real-time push notifications (received via SignalR) as overlay banners:

```razor
<!-- src/UI/Components/Shared/NotificationToast.razor -->
<div class="notification-toast @(IsVisible ? "show" : "hide")" @onclick=@OnClick>
	<div class="notification-content">
		<div class="notification-icon">
			@switch (NotificationType)
			{
				case NotificationType.AddedToBoard:    <Icon Type="team" />    break;
				case NotificationType.AssignedToTask:  <Icon Type="user" />    break;
				case NotificationType.CommentedOnTask: <Icon Type="message" /> break;
				default:                               <Icon Type="notification" /> break;
			}
		</div>
		<div class="notification-text">
			<div class="notification-title">@UI.Resources.I18n.NewNotification</div>
			<div class="notification-message">@Message</div>
		</div>
		<div class="notification-close" @onclick:stopPropagation="true" @onclick=@OnClose>
			<Icon Type="close" />
		</div>
	</div>
</div>
```

---

## 3.6.3 State Management & Data Binding

### Internal Component State

Components maintain their own local state through private C# fields. Rendering is automatically triggered after event handlers complete. When state is changed outside the normal Blazor event loop (e.g., in a SignalR callback or timer tick), `StateHasChanged()` must be called explicitly:

```csharp
// src/UI/Layouts/BasicLayout.razor.cs (excerpt)
private void HandleNotificationCountChanged()
{
	_unreadNotificationsCount = NotificationCountState.UnreadCount;
	_ = BuildMenuDataAsync();
	InvokeAsync(StateHasChanged);  // called from a non-UI thread
}
```

### Global Loading State

The `GlobalLoadingService` implements a reference-counted loading flag. Components that inherit `BaseComponentWithLoading` subscribe to its `OnLoadingChanged` event, which triggers `StateHasChanged()` and re-renders the spinner:

```csharp
// src/UI/Services/GlobalLoadingService.cs (excerpt)
public void ShowLoading()
{
	_loadingCounter++;
	if (!_isLoading)
	{
		_isLoading = true;
		OnLoadingChanged?.Invoke();
	}
}

public void HideLoading()
{
	if (_loadingCounter > 0) _loadingCounter--;

	if (_loadingCounter == 0 && _isLoading)
	{
		_isLoading = false;
		OnLoadingChanged?.Invoke();
	}
}
```

Pages consume this pattern via the cascading parameter:

```csharp
// src/UI/Pages/Board/BoardDetail.razor.cs (excerpt)
[CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;

protected bool IsLoading => LoadingService?.IsLoading ?? false;

protected override void OnInitialized()
{
	LoadingService.OnLoadingChanged += StateHasChanged;
	base.OnInitialized();
}
```

### `[Parameter]` and `EventCallback` – Parent-Child Communication

Data flows **down** to child components via `[Parameter]` properties. User-triggered changes flow **up** to the parent via `EventCallback` delegates. The following example shows the `OrganizationSelector` component:

```csharp
// src/UI/Components/Shared/OrganizationSelector.razor.cs (excerpt)
[Parameter] public Guid? SelectedOrganizationId { get; set; }
[Parameter] public EventCallback<Guid> SelectedOrganizationIdChanged { get; set; }
[Parameter] public bool ShowLabel { get; set; } = true;
[Parameter] public string? Placeholder { get; set; }
[Parameter] public bool ExcludeGuestOrganizations { get; set; } = false;
```

The parent binds to the component using two-way binding syntax, which wires up both the value parameter and its `Changed` callback:

```razor
<!-- src/UI/Pages/Boards/Boards.razor (excerpt) -->
<OrganizationSelector SelectedOrganizationId="_selectedOrganizationId"
					  SelectedOrganizationIdChanged="@OnOrganizationChanged"
					  ShowLabel="false"
					  Placeholder="Select organization to view boards" />
```

The `NotificationToast` component demonstrates the use of parameterised `EventCallback` for close and click actions:

```csharp
// src/UI/Components/Shared/NotificationToast.razor.cs
public partial class NotificationToast : ComponentBase
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public string Message { get; set; } = string.Empty;
	[Parameter] public NotificationType NotificationType { get; set; }
	[Parameter] public EventCallback OnClick { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }
}
```

The parent invokes these callbacks to update its own state and re-render:

```razor
<NotificationToast IsVisible=@showNotification
				   Message=@notificationMessage
				   NotificationType=@notificationType
				   OnClick=@(() => Navigation.NavigateTo("/notifications"))
				   OnClose=@HideNotification />
```

---

## 3.6.4 Authentication & Authorization UI

### Authentication Architecture

The application uses Microsoft Identity Platform (Azure AD) with the OAuth 2.0 Authorization Code + PKCE flow. Authentication state is managed by a custom `AuthService` that stores the JWT in browser local storage and refreshes it on a timer. All API calls are intercepted by `AuthenticationHandler`, a `DelegatingHandler` that attaches the `Bearer` token to every outgoing HTTP request:

```csharp
// src/UI/Handlers/AuthenticationHandler.cs
public class AuthenticationHandler : DelegatingHandler
{
	private readonly IAuthService _authService;

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var token = await _authService.GetAccessTokenAsync();

		if (!string.IsNullOrEmpty(token))
			request.Headers.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

		return await base.SendAsync(request, cancellationToken);
	}
}
```

The handler is registered in the DI container and attached to every Refit API client:

```csharp
// src/UI/Extensions/ServiceCollectionExtensions.cs (excerpt)
services.AddScoped<AuthenticationHandler>();

services.AddRefitClient<IBoardApi>(refitSettings)
	.ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
	.AddHttpMessageHandler<AuthenticationHandler>();
```

### Login Page

The login page (`/login`) is intentionally kept outside the authenticated layout. It presents a Microsoft sign-in button and delegates to `AuthService.InitiateLoginAsync()`:

```razor
<!-- src/UI/Pages/Login.razor (excerpt) -->
@page "/login"

<Card class="login-card" Title="@UI.Resources.I18n.SignInToTaskPilot">
	@if (_isLoading)
	{
		<Spin Size=@SpinSize.Large Tip="@UI.Resources.I18n.SigningYouIn">
			<div style="height: 100px;" />
		</Spin>
	}
	else if (_error != null)
	{
		<Alert Type=@AlertType.Error
			   Message="@UI.Resources.I18n.AuthenticationError"
			   Description=@_error
			   ShowIcon=@true />
	}

	<Button Type=@ButtonType.Primary
			Size=@ButtonSize.Large
			Icon="microsoft"
			Loading=@_isLoading
			OnClick=@HandleLogin
			Block=@true>
		@UI.Resources.I18n.SignInWithMicrosoft
	</Button>
</Card>
```

### Guard-Based Navigation

Because the project does not use Blazor's built-in `AuthenticationStateProvider` (the JWT is managed directly by `AuthService`), authentication guards are implemented as imperative redirects in each page's `OnInitializedAsync` lifecycle method:

```csharp
// src/UI/Pages/Boards/Boards.razor.cs (excerpt)
protected override async Task OnInitializedAsync()
{
	await LoadInitialBoards();
}

private async Task LoadInitialBoards()
{
	await AuthService.ExecuteWithGlobalLoadingAsync(LoadingService, async service =>
	{
		var isAuthenticated = await service.IsAuthenticatedAsync();
		if (!isAuthenticated)
		{
			Navigation.NavigateTo("/login");
			return;
		}
		// ... load boards
	});
}
```

### Role-Based UI Rendering

Certain UI controls are conditionally rendered based on the current user's role or board membership permissions. Permissions are evaluated by helper methods in the component's code-behind and passed down as boolean `[Parameter]` values to child components:

```csharp
// src/UI/Pages/Board/BoardDetail.razor.cs (excerpt)
private bool CanManageMembers()
{
	if (_currentUser == null || _boardDetail == null) return false;
	var member = _boardDetail.Members
		.FirstOrDefault(m => m.UserId == _currentUser.Id.ToString());
	return member?.Role is "Owner" or "Manager";
}
```

The boolean is then forwarded as a parameter:

```razor
<MembersModal CanManageMembers=@(CanManageMembers())
			  ... />
```

Inside child components, conditional rendering hides management controls from read-only members:

```razor
@if (CanManageTask)
{
	<FormItem Label=@UI.Resources.I18n.AssigneeLabel>
		<Select @bind-Value=@FormModel.AssigneeId ... />
	</FormItem>
}
```

The user's system role (e.g. `Admin`) is surfaced visually with a coloured `<Tag>`:

```razor
<!-- src/UI/Pages/Welcome.razor (excerpt) -->
<Tag Color=@(_currentUser.Role == "Admin" ? "red" : "blue")>
	@_currentUser.Role
</Tag>
```

---

## 3.6.5 Localization (I18n)

### Approach

All user-facing strings are defined as `public const string` fields in the static class `UI.Resources.I18n`. This class acts as a strongly-typed string table that is compiled into the WebAssembly bundle, providing compile-time safety and IDE refactoring support. The strings are authored in Ukrainian (the primary locale of the thesis project).

```csharp
// src/UI/Resources/I18n.cs (excerpt)
namespace UI.Resources;

public static class I18n
{
	// Authentication
	public const string SignInToTaskPilot      = "Увійти в TaskPilot";
	public const string WelcomeToTaskPilot     = "Ласкаво просимо в TaskPilot";
	public const string SignInWithMicrosoft    = "Увійти через Microsoft";
	public const string AuthenticationError    = "Помилка автентифікації";

	// Boards
	public const string LoadingYourBoards      = "Завантаження ваших дошок...";
	public const string CreateYourFirstBoard   = "Створіть вашу першу дошку";
	public const string LoadMoreBoards         = "Завантажити ще дошок";

	// Tasks
	public const string TaskTitleLabel         = "Назва";
	public const string DescriptionLabel       = "Опис";
	public const string AssigneeLabel          = "Виконавець";
	public const string PriorityLow            = "Низький";
	public const string PriorityNormal         = "Нормальний";
	public const string PriorityHigh           = "Високий";
	public const string PriorityImmediate      = "Негайний";
	...
}
```

A companion `.resx` file (`I18n.resx`) is also provided for tooling compatibility, and a `I18n.zh-CN.resx` file exists for a Simplified Chinese locale, demonstrating that the architecture supports multi-language extension.

### Usage in Components

Strings are referenced inline in templates and in code-behind without any injection or lookup ceremony:

```razor
<!-- Label referencing I18n in a form -->
<FormItem Label=@UI.Resources.I18n.TaskTitleLabel Required>
	<Input @bind-Value=@FormModel.Title
		   Placeholder=@UI.Resources.I18n.EnterTaskTitle />
</FormItem>
```

Format-string substitution uses `string.Format` with the constant as the format template:

```razor
<!-- src/UI/Pages/Welcome.razor (excerpt) -->
<Alert Description=@(string.Format(UI.Resources.I18n.WelcomeLoggedInAs, _currentUser.Username))
	   Type=@AlertType.Success />
```

Menu items with dynamic counts use the same interpolation pattern:

```csharp
// src/UI/Layouts/BasicLayout.razor.cs (excerpt)
Name = _invitationsCount > 0
	? string.Format(UI.Resources.I18n.InvitationsMenuWithCount, _invitationsCount)
	: UI.Resources.I18n.InvitationsMenu,
```

The Ant Design Blazor localization extension is registered during service configuration via `AntDesign.Extensions.Localization`, which allows the Ant Design component library itself (date-pickers, pagination, etc.) to render in the correct locale at runtime.

---

## Summary

| Concern | Implementation |
|---|---|
| Application shell | `AntDesign.ProLayout.BasicLayout` with collapsible side-nav |
| Routing | Blazor `<Router>` with `@page` directives; `<RouteView>` with default layout |
| Code separation | `.razor` template + `.razor.cs` partial class pattern throughout |
| UI component library | Ant Design Blazor (`AntDesign`, `AntDesign.ProLayout`) |
| Forms & validation | `<Form>`, `<FormItem>`, `<Input>`, `<Select>`, `<TextArea>` from AntDesign |
| Modals | `<Modal>` with custom `Footer` render fragment |
| Notifications | `IMessageService` (transient toasts) + `NotificationToast` (real-time push) |
| State management | Local fields, `StateHasChanged()`, `OnLoadingChanged` event, `CascadingValue` |
| Parent-child communication | `[Parameter]` + `EventCallback<T>` |
| Authentication | Custom `AuthService` with Azure AD OAuth2/PKCE; JWT stored in localStorage |
| Auth HTTP handler | `AuthenticationHandler : DelegatingHandler` attached to all Refit clients |
| Authorization | Imperative navigation guards + boolean `[Parameter]` permission flags |
| Localization | Compile-time `static class I18n` string constants; `.resx` for tooling support |
