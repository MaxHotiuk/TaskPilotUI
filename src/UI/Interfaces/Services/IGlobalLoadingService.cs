using System;
using System.Threading.Tasks;

namespace UI.Interfaces.Services;

public interface IGlobalLoadingService
{
    bool IsLoading { get; }
    event Action? OnLoadingChanged;
    
    void ShowLoading();
    void HideLoading();
    Task ExecuteWithLoadingAsync(Func<Task> operation);
    Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation);
    void ForceHideLoading();
}
