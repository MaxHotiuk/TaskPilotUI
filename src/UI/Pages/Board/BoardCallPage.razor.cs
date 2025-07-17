using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskPilotUI.UI.Pages.Board
{
    public partial class BoardCallPage : ComponentBase, IDisposable
{
    [Parameter] public string? BoardId { get; set; }

    protected bool _inCall = false;
    protected bool _cameraOn = true;
    protected bool _micOn = true;
    protected bool _connectionReady = false;
    protected DotNetObjectReference<BoardCallPage>? _objRef;
    protected List<RemoteUser> _remoteUsers = new();

    [Inject] protected IJSRuntime? JS { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(BoardId) && JS != null)
        {
            try
            {
                _objRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("BoardCallInterop.init", BoardId, "localVideo", _objRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing BoardCallInterop: {ex.Message}");
            }
        }
    }

    [JSInvokable]
    public Task OnWebRtcConnected()
    {
        _connectionReady = true;
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task AddRemoteUser(string userId, string displayName)
    {
        if (_remoteUsers.All(u => u.UserId != userId))
        {
            _remoteUsers.Add(new RemoteUser
            {
                UserId = userId,
                DisplayName = displayName,
                VideoId = $"remoteVideo_{userId}"
            });
            InvokeAsync(StateHasChanged);
        }
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task RemoveRemoteUser(string userId)
    {
        var user = _remoteUsers.FirstOrDefault(u => u.UserId == userId);
        if (user != null)
        {
            _remoteUsers.Remove(user);
            InvokeAsync(StateHasChanged);
        }
        return Task.CompletedTask;
    }

    protected async Task StartCall()
    {
        try
        {
            if (JS != null)
            {
                await JS.InvokeVoidAsync("BoardCallInterop.startCall");
            }
            _inCall = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting call: {ex.Message}");
        }
    }

    protected async Task HangUp()
    {
        try
        {
            if (JS != null)
            {
                await JS.InvokeVoidAsync("BoardCallInterop.hangUp");
            }
            _inCall = false;
            _remoteUsers.Clear();
            // Note: Don't reset camera/mic state here as the hardware should stay on
            // The JavaScript hangUp function preserves the local stream
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error hanging up: {ex.Message}");
        }
    }

    protected async Task ToggleCamera()
    {
        try
        {
            _cameraOn = !_cameraOn;
            if (JS != null)
            {
                await JS.InvokeVoidAsync("BoardCallInterop.toggleCamera", _cameraOn);
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling camera: {ex.Message}");
        }
    }

    protected async Task ToggleMic()
    {
        try
        {
            _micOn = !_micOn;
            if (JS != null)
            {
                await JS.InvokeVoidAsync("BoardCallInterop.toggleMic", _micOn);
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling mic: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

        public class RemoteUser
        {
            public string? UserId { get; set; }
            public string? DisplayName { get; set; }
            public string? VideoId { get; set; }
        }
    }
}
