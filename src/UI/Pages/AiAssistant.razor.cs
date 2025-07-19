using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;
using UI.Models.Chat;

namespace UI.Pages;

public partial class AiAssistant : ComponentBase
{
    private string? _question;
    private ChatResponse? _response;
    private bool _isLoading = false;
    private string? _error;

    [Inject]
    private IChatService? ChatService { get; set; }

    private async Task AskAiAsync()
    {
        _isLoading = true;
        _error = null;
        _response = null;
        try
        {
            var request = new ChatRequest { Message = _question, SessionId = Guid.NewGuid().ToString() };
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
}