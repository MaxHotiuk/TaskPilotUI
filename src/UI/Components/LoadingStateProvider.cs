using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;

namespace UI.Components;

public class LoadingStateProvider : ComponentBase, IDisposable
{
    [CascadingParameter] public IGlobalLoadingService? LoadingService { get; set; }
    
    protected bool IsLoading => LoadingService?.IsLoading ?? false;
    
    protected override void OnInitialized()
    {
        if (LoadingService != null)
        {
            LoadingService.OnLoadingChanged += StateHasChanged;
        }
    }

    public void Dispose()
    {
        if (LoadingService != null)
        {
            LoadingService.OnLoadingChanged -= StateHasChanged;
        }
    }
}
