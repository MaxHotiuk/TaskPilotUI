# 3.4 AI Integration — Frontend Logic Layer

This document describes the client-side logic layer that powers the AI Assistant feature in TaskPilot. It covers the Refit API interface, the service wrapper, request/response data models, DI registration, and error-handling strategy. UI markup and layout concerns are addressed separately in Section 3.6.

---

## 3.4.1 AI API Client (`IChatApi`)

Communication with the backend AI endpoint is handled through a [Refit](https://github.com/reactiveui/refit) interface. Refit generates a type-safe `HttpClient` implementation at compile time from the interface definition.

**File:** `src/UI/Interfaces/Api/IChatApi.cs`

```csharp
using Refit;
using UI.Models.Chat;
using UI.Models.Task;

namespace UI.Interfaces.Api;

public interface IChatApi
{
	[Post("/api/chat/ask")]
	Task<ChatResponse> AskAsync([Body] ChatRequest request);
}
```

### Key Details

| Aspect | Detail |
|---|---|
| HTTP verb | `POST` |
| Endpoint | `/api/chat/ask` |
| Request serialization | JSON body (`[Body]` attribute) |
| Response type | `Task<ChatResponse>` — awaitable, non-streaming |
| Authentication | Attached automatically via `AuthenticationHandler` (see §3.4.4) |

The single method `AskAsync` maps directly to the backend's AI question-answering endpoint. The `[Body]` attribute instructs Refit to serialize the `ChatRequest` object as the JSON request body.

---

## 3.4.2 Data Transfer Models

### `ChatRequest`

**File:** `src/UI/Models/Chat/ChatRequest.cs`

```csharp
namespace UI.Models.Chat;

public class ChatRequest
{
	public string? Message { get; set; }
	public string? SessionId { get; set; }
	public Guid? OrganizationId { get; set; }
}
```

| Property | Purpose |
|---|---|
| `Message` | The natural-language question entered by the user. |
| `SessionId` | A per-session GUID (generated fresh per request with `Guid.NewGuid().ToString()`) that allows the backend to maintain conversational context if needed. |
| `OrganizationId` | Scopes the AI query to a specific organization, allowing the backend to restrict knowledge-base context to that organization's data. |

### `ChatResponse`

**File:** `src/UI/Models/Chat/ChatResponse.cs`

```csharp
namespace UI.Models.Chat;

public class ChatResponse
{
	public string? Response { get; set; }
	public List<string>? Sources { get; set; }
	public string? SessionId { get; set; }
}
```

| Property | Purpose |
|---|---|
| `Response` | The AI-generated answer text, rendered verbatim in the UI. |
| `Sources` | Optional list of source references cited by the AI backend (e.g., document names, URLs). |
| `SessionId` | The session identifier echoed back from the backend; can be reused in subsequent requests to maintain conversational continuity. |

---

## 3.4.3 AI Service Logic (`ChatService`)

The `ChatService` is a thin scoped service that wraps `IChatApi` and is consumed by Blazor components via the `IChatService` abstraction. This indirection decouples components from Refit directly, simplifying testing and future implementation swaps.

**Interface — `src/UI/Interfaces/Services/IChatService.cs`**

```csharp
using UI.Models.Chat;

namespace UI.Interfaces.Services;

public interface IChatService
{
	Task<ChatResponse> AskAsync(ChatRequest request);
}
```

**Implementation — `src/UI/Services/ChatService.cs`**

```csharp
using UI.Models.Chat;
using UI.Interfaces.Services;
using UI.Interfaces.Api;

namespace UI.Services;

public class ChatService : IChatService
{
	private readonly IChatApi _chatApi;

	public ChatService(IChatApi chatApi)
	{
		_chatApi = chatApi;
	}

	public async Task<ChatResponse> AskAsync(ChatRequest request)
	{
		return await _chatApi.AskAsync(request);
	}
}
```

### Request Formatting (Component → Service)

The request object is constructed in `AiAssistant.razor.cs` immediately before calling the service:

```csharp
var request = new ChatRequest
{
	Message = _question,                        // Raw user input from TextArea
	SessionId = Guid.NewGuid().ToString(),      // Fresh session GUID per query
	OrganizationId = _selectedOrganizationId    // Organization scope from selector
};
_response = await ChatService!.AskAsync(request);
```

- `Message` is taken directly from the bound `_question` field.
- `SessionId` is regenerated on every call, making each request stateless from the frontend's perspective. If session continuity is required in the future, the `SessionId` from the previous `ChatResponse` should be forwarded here instead.
- `OrganizationId` is required; the submit button is disabled while it is `null`, preventing requests without an organization context.

### Response Handling

`ChatService.AskAsync` returns the deserialized `ChatResponse` object. The component stores it in `_response` and Blazor's reactive rendering pipeline propagates it to the UI automatically on the next render cycle:

```csharp
_response = await ChatService!.AskAsync(request);
// Blazor re-renders: _response.Response text and _response.Sources become visible
```

---

## 3.4.4 Streaming Data

The current implementation **does not use streaming**. The `IChatApi.AskAsync` method returns `Task<ChatResponse>`, which means Refit awaits the complete HTTP response body before deserializing and returning the result. The backend must finalize the full AI-generated answer before the client receives any data.

If streaming support (Server-Sent Events or chunked transfer) is added in the future, the recommended approach for Blazor WebAssembly would be:

```csharp
// Hypothetical streaming implementation via HttpClient directly
using var response = await _httpClient.PostAsync("/api/chat/ask/stream", content);
response.EnsureSuccessStatusCode();

await using var stream = await response.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream);

while (!reader.EndOfStream)
{
	var chunk = await reader.ReadLineAsync();
	if (!string.IsNullOrEmpty(chunk))
	{
		_partialResponse += chunk;
		await InvokeAsync(StateHasChanged); // Push each chunk to UI
	}
}
```

This pattern reads the response body incrementally and calls `StateHasChanged` after each chunk to produce a typewriter-style progressive rendering effect.

---

## 3.4.5 Error Handling & Timeouts

Error handling is performed entirely in the `AiAssistant` page's code-behind with a `try/catch/finally` block. The service layer itself does not swallow exceptions — it propagates them upward so that each call site can apply context-appropriate handling.

**Full flow from `AiAssistant.razor.cs`:**

```csharp
private async Task AskAiAsync()
{
	_isLoading = true;
	_error = null;
	_response = null;
	try
	{
		var request = new ChatRequest
		{
			Message = _question,
			SessionId = Guid.NewGuid().ToString(),
			OrganizationId = _selectedOrganizationId
		};
		_response = await ChatService!.AskAsync(request);
	}
	catch (Exception ex)
	{
		_error = ex.Message;
	}
	finally
	{
		_isLoading = false;
	}
}
```

### Behaviour Table

| Scenario | Outcome |
|---|---|
| Successful response | `_response` is populated; `_error` remains `null`. |
| Network failure / timeout | `HttpRequestException` is caught; `ex.Message` is assigned to `_error` and displayed in an Ant Design `<Alert>` component. |
| HTTP 429 (Rate Limit) | Refit throws `ApiException` (subtype of `Exception`); caught by the same handler; the raw error message surfaced to the user. |
| HTTP 500 (Server Error) | Same as above — `ApiException` caught, `_error` set. |
| Any other exception | Caught by the broad `Exception` handler; message displayed. |
| Request in progress | `_isLoading = true` disables the submit button and shows a loading spinner, preventing duplicate concurrent requests. |
| After any outcome | `_isLoading` is reset to `false` in `finally`, ensuring the UI never stays locked in a loading state. |

### Timeout Configuration

No explicit `HttpClient` timeout is configured specifically for the AI endpoint beyond the Refit default (which inherits the underlying `HttpClient` timeout, defaulting to **100 seconds** in .NET). If the AI backend is expected to have higher latency, a dedicated named `HttpClient` with a custom `Timeout` can be configured in `ServiceCollectionExtensions.cs`:

```csharp
// Example: extend timeout for the AI client only
services.AddRefitClient<IChatApi>(refitSettings)
	.ConfigureHttpClient(c =>
	{
		c.BaseAddress = new Uri(apiBaseUrl);
		c.Timeout = TimeSpan.FromSeconds(180); // Override for slow AI responses
	})
	.AddHttpMessageHandler<AuthenticationHandler>();
```

---

## 3.4.6 Dependency Injection Registration

Both the Refit client and the service wrapper are registered in `ServiceCollectionExtensions.cs`:

```csharp
// Refit HTTP client for AI endpoint
services.AddRefitClient<IChatApi>(refitSettings)
	.ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
	.AddHttpMessageHandler<AuthenticationHandler>();

// Scoped service wrapper
services.AddScoped<IChatService, ChatService>();
```

- `IChatApi` is registered as a Refit client tied to `apiBaseUrl` with the shared `AuthenticationHandler`, which injects the current user's JWT Bearer token into every outgoing request.
- `IChatService` / `ChatService` are registered as `Scoped`, so each Blazor circuit (user session) receives its own instance, with no shared mutable state between users.

---

## Summary of Component Interactions

```
AiAssistant.razor.cs
  └─ IChatService.AskAsync(ChatRequest)
	   └─ ChatService.AskAsync(ChatRequest)
			└─ IChatApi.AskAsync(ChatRequest)   [Refit → POST /api/chat/ask]
				 └─ AuthenticationHandler        [Attaches Bearer token]
					  └─ Backend AI Service      [Returns ChatResponse JSON]
```
