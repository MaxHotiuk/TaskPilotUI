using Microsoft.AspNetCore.Components;
using UI.Interfaces.Services;

namespace UI.Components.Base;

public abstract class BaseComponentWithLoading : ComponentBase, IDisposable
{
    [CascadingParameter] public IGlobalLoadingService LoadingService { get; set; } = default!;
    
    protected bool IsLoading => LoadingService?.IsLoading ?? false;
    
    protected override void OnInitialized()
    {
        if (LoadingService != null)
        {
            LoadingService.OnLoadingChanged += StateHasChanged;
        }
        base.OnInitialized();
    }

    public virtual void Dispose()
    {
        if (LoadingService != null)
        {
            LoadingService.OnLoadingChanged -= StateHasChanged;
        }
    }
}
