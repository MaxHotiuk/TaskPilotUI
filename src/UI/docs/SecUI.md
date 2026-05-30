# TaskPilot UI — Authentication & Security Documentation

## Overview

TaskPilot uses a custom OAuth 2.0 Authorization Code flow with PKCE against **Microsoft Entra ID (Azure AD)**, without the MSAL browser library. All auth logic lives in a bespoke `AuthService`, a custom `DelegatingHandler` attaches tokens to HTTP requests, and UI access control is enforced programmatically via role checks on the cached `UserDto`.

---

## 1. MSAL Configuration

TaskPilot does **not** use the `Microsoft.Authentication.WebAssembly.Msal` NuGet package. Instead, it implements a fully custom OAuth 2.0 Authorization Code + PKCE flow, calling the Entra ID token endpoint directly. The configuration is read from `wwwroot/appsettings.json` at startup and mapped to the `AuthConfiguration` model.

### `wwwroot/appsettings.json`

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7067"
  },
  "AzureAd": {
    "ClientId": "f5cff8d2-443a-4279-a0a3-fd3c69aa779b",
    "TenantId": "common",
    "RedirectUri": "https://localhost:5001/login",
    "Scope": "api://f5cff8d2-443a-4279-a0a3-fd3c69aa779b/TaskPilot.All"
  }
}
```

**Key points:**

- `Scope` is a custom application scope (`TaskPilot.All`) registered against the backend's App Registration in Entra ID. This is the scope requested when the user initiates login, ensuring the resulting access token is audience-bound to the TaskPilot API.
- `TenantId: "common"` allows multi-tenant sign-in (any Microsoft account / work or school account).
- The `AuthConfiguration` model (`Models/Auth/AuthConfiguration.cs`) is the strongly-typed representation of this section:

```csharp
public class AuthConfiguration
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
```

### `Program.cs` — Startup bootstrapping

```csharp
public static async Task Main(string[] args)
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");

    // Load appsettings.json from the server at startup
    var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    var stream = await http.GetStreamAsync("appsettings.json");
    builder.Configuration.AddJsonStream(stream);

    // Register all API clients, handlers, and services
    builder.Services.AddApiClientsAndServices(builder.Configuration);

    var host = builder.Build();

    // Restore any previously stored token and start the refresh timer before the UI renders
    var authService = host.Services.GetRequiredService<IAuthService>();
    await authService.InitializeAsync();

    await host.RunAsync();
}
```

`authService.InitializeAsync()` is called before `RunAsync()`. This ensures that if a valid token is already in `localStorage` from a previous session, the `AuthState` is hydrated and the token-refresh timer is armed before the first component renders — preventing a flash of unauthenticated state.

### Login URL Construction (`AuthService.GetLoginUrlAsync`)

```csharp
public async Task<string> GetLoginUrlAsync()
{
    var config = GetAuthConfig();       // reads "AzureAd" section from IConfiguration
    var state = Guid.NewGuid().ToString();

    var codeVerifier = GenerateCodeVerifier();
    var codeChallenge = GenerateCodeChallenge(codeVerifier);  // SHA-256, Base64URL

    // Persist PKCE material in localStorage for the callback
    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_state", state);
    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "code_verifier", codeVerifier);

    var authUrl =
        $"https://login.microsoftonline.com/{config.TenantId}/oauth2/v2.0/authorize" +
        $"?client_id={config.ClientId}" +
        $"&response_type=code" +
        $"&redirect_uri={Uri.EscapeDataString(config.RedirectUri)}" +
        $"&response_mode=query" +
        $"&scope={Uri.EscapeDataString(config.Scope)}" +  // <-- TaskPilot.All scope
        $"&state={state}" +
        $"&code_challenge={codeChallenge}" +
        $"&code_challenge_method=S256";

    return authUrl;
}
```

The `scope` parameter requests `api://f5cff8d2-.../TaskPilot.All` — this tells Entra ID that the resulting access token should be issued for the TaskPilot backend API (not Microsoft Graph). PKCE (`code_challenge_method=S256`) prevents authorization code interception attacks.

---

## 2. Token Management — `AuthenticationHandler`

### Custom `DelegatingHandler`

Every protected Refit API client is wrapped with a custom `AuthenticationHandler` (`Handlers/AuthenticationHandler.cs`) that intercepts outgoing requests and attaches the Bearer token from the in-memory `AuthService`.

```csharp
public class AuthenticationHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticationHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

`GetAccessTokenAsync()` reads the token from the in-memory `_authState.AccessToken`, which is backed by `localStorage` if the state was rehydrated on startup. The handler never touches credentials directly — it delegates to `IAuthService`.

### Registration in `ServiceCollectionExtensions`

Every Refit client that calls a **protected** backend endpoint has `AddHttpMessageHandler<AuthenticationHandler>()` in its pipeline:

```csharp
services.AddScoped<AuthenticationHandler>();

services.AddRefitClient<IUserApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthenticationHandler>();

services.AddRefitClient<IBoardApi>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthenticationHandler>();

// ... same pattern for IBoardMemberApi, IBoardTaskApi, IBoardStateApi,
//     ICommentApi, IAttachmentApi, IAvatarApi, IChatApi, IChatSystemApi,
//     INotificationApi, ITagApi, IMeetingApi, IMeetingMemberApi,
//     IOrganizationApi, IInvitationApi, IGoogleCalendarApi
```

The `ITaskPilotAuthApi` client (used only for the initial `/auth/current` user lookup during the callback) does **not** have the handler attached — it passes the token manually in that one call as a header argument. The `IMicrosoftGraphApi` client points to `https://graph.microsoft.com` and also omits the handler, as it uses a separate token flow.

### Token Lifecycle — Acquisition and Refresh

After the Entra ID redirect, `AuthService.HandleCallbackAsync` exchanges the authorization code for tokens:

```csharp
var tokenRequest = new Dictionary<string, string>
{
    {"grant_type", "authorization_code"},
    {"client_id", config.ClientId},
    {"code", code},
    {"redirect_uri", config.RedirectUri},
    {"code_verifier", codeVerifier}          // PKCE verifier
};

var tokenResponse = await tokenApi.GetTokenAsync(tokenRequest);

if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
{
    var accessToken = accessTokenElement.GetString();
    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "access_token", accessToken);
    SetTokenTimes(accessToken!);             // decodes JWT claims: iat, exp

    if (tokenResponse.TryGetProperty("refresh_token", out var newRefreshTokenElement))
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refresh_token", newRefreshToken);
        StartTokenRefreshTimer(accessToken!); // schedules proactive refresh at half-TTL
    }
}
```

**Proactive refresh** is scheduled at half the token's TTL (derived from the JWT `iat`/`exp` claims). When the timer fires, `RefreshTokenAsync()` performs a `grant_type=refresh_token` exchange silently in the background, updates `localStorage` and `_authState`, and reschedules itself:

```csharp
var tokenRequest = new Dictionary<string, string>
{
    {"grant_type", "refresh_token"},
    {"client_id", config.ClientId},
    {"refresh_token", refreshToken},
    {"redirect_uri", config.RedirectUri},
    {"scope", config.Scope}                  // re-request the TaskPilot.All scope
};
```

---

## 3. UI Protection and Conditional Rendering

TaskPilot does not use Blazor's built-in `AuthorizeView` / `[Authorize]` attribute infrastructure (which requires `AuthenticationStateProvider`). Instead, protection is enforced **programmatically** in each component's code-behind and in **markup-level role checks** against the `UserDto` retrieved from `IAuthService`.

### Route-Level Guard — `Login.razor.cs`

The `/login` page doubles as the OAuth callback handler. On initialization it checks whether the user is already authenticated and redirects away immediately:

```csharp
protected override async Task OnInitializedAsync()
{
    // Fast path: already authenticated in-memory
    if (AuthService.AuthState.IsAuthenticated && AuthService.GetCachedUser() != null)
    {
        Navigation.NavigateTo("/");
        return;
    }

    // Slow path: check localStorage for a valid token
    if (await AuthService.IsAuthenticatedAsync())
    {
        Navigation.NavigateTo("/");
        return;
    }

    // OAuth callback: code + state present in query string
    var uri = new Uri(Navigation.Uri);
    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
    var code = query["code"];
    var state = query["state"];

    if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
    {
        await HandleCallback(code, state);  // exchanges code for token
    }
}
```

### Role-Based Conditional Rendering — `OrganizationMembers.razor`

UI elements are shown or hidden by checking the `_currentUserRole` field (loaded from `UserDto.Organizations[].Role`) directly in Razor markup:

```razor
<PageHeaderExtra>
    @if (_currentUserRole == "Manager")
    {
        <Button Type="@ButtonType.Primary" OnClick="@ShowAddGuestModal">
            <Icon Type="user-add" />
            @UI.Resources.I18n.AddGuest
        </Button>
    }
    @if (_currentUserRole == "Member" && !_hasManagers)
    {
        <Button Type="@ButtonType.Primary" OnClick="@ShowManagerRequestModal">
            <Icon Type="crown" />
            @UI.Resources.I18n.RequestManagerRole
        </Button>
    }
</PageHeaderExtra>
```

Only `Manager`-role members see the **Add Guest** button. Only plain `Member`s in an organization that currently has no managers see the **Request Manager Role** button.

### Admin Route Guard — `ManagerRequests.razor.cs`

Admin-only pages perform an imperative role check in `OnInitializedAsync` and redirect unauthorized users:

```csharp
protected override async Task OnInitializedAsync()
{
    await base.OnInitializedAsync();

    var currentUser = AuthService.GetCachedUser();
    if (currentUser?.Role != "Admin")
    {
        NavigationManager.NavigateTo("/");  // hard redirect, no render
        return;
    }

    await LoadRequestsAsync();
}
```

The `Role` field on `UserDto` is the **application-level** role (e.g., `"Admin"`, `"User"`), distinct from the per-organization role stored in `OrganizationSummaryDto.Role`.

### Dynamic Menu — `BasicLayout.razor.cs`

The sidebar menu is built dynamically after authentication, showing organization-specific entries only when the user belongs to organizations:

```csharp
var currentUser = await AuthService.GetCurrentUserAsync();
if (currentUser != null && currentUser.Organizations?.Any() == true)
{
    var organizationMenuItems = new List<MenuDataItem>();
    foreach (var org in currentUser.Organizations)
    {
        organizationMenuItems.Add(new MenuDataItem
        {
            Path = $"/organization/{org.Id}",
            Name = org.Name,
            Key = $"org-{org.Id}",
        });
    }
    // ... add to _menuData
}
```

`AuthService.OnAuthStateChanged` is subscribed in `OnInitializedAsync`, so the menu rebuilds automatically if the auth state changes during the session.

---

## 4. Organization State

### Data Model

The current user's organization memberships are embedded directly in the `UserDto` returned from the backend after login:

```csharp
public class UserDto
{
    public Guid Id { get; set; }
    public string EntraId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;           // app-level: Admin / User
    public List<OrganizationSummaryDto> Organizations { get; set; } = new();
}

public class OrganizationSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;           // org-level: Guest / Member / Manager
}
```

### Retrieval Flow

1. After the OAuth callback, `AuthService.HandleCallbackAsync` exchanges the authorization code for an access token and then calls `GetCurrentUserAsync()`.
2. `GetCurrentUserAsync` calls `ITaskPilotAuthApi.GetCurrentAsync(bearerToken)` — a backend endpoint that returns the fully-populated `UserDto` including the `Organizations` list.
3. The `UserDto` is stored on `_authState.User` in memory and is accessible to any component via `IAuthService.GetCachedUser()` (synchronous) or `IAuthService.GetCurrentUserAsync()` (async, with fallback to `localStorage` token if cache is cold).

### `OrganizationSelector` Component

The `OrganizationSelector` component (`Components/Shared/OrganizationSelector.razor`) reads the organization list directly from the cached user and persists the user's **last selected organization** to `localStorage` for cross-session continuity:

```csharp
private async Task LoadOrganizations()
{
    var currentUser = await AuthService.GetCurrentUserAsync();

    if (currentUser?.Organizations != null)
    {
        _organizations = currentUser.Organizations.ToList();

        // Optionally filter out guest-only memberships
        _filteredOrganizations = ExcludeGuestOrganizations
            ? _organizations.Where(o => o.Role != "Guest").ToList()
            : _organizations;

        // Auto-select if only one eligible org
        if (_filteredOrganizations.Count == 1 && !SelectedOrganizationId.HasValue)
        {
            await HandleOrganizationChanged(_filteredOrganizations[0].Id);
        }
        // Restore last selection from localStorage
        else if (!SelectedOrganizationId.HasValue)
        {
            var savedOrgId = await LocalStorageService.GetItemAsync<Guid?>(SELECTED_ORG_KEY);
            // ... restore if still a valid member
        }
    }
}
```

**There is no dedicated organization context service or Cascading parameter.** Each page that needs the current organization reads it from the route parameter (e.g., `/organization/{OrganizationId}`) or from `OrganizationSelector`'s `SelectedOrganizationIdChanged` callback, which writes the selection to `localStorage` under the key `"selectedOrganizationId"` for persistence across page navigations.