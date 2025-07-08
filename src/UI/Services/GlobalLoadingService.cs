using System;
using System.Threading.Tasks;
using UI.Interfaces.Services;

namespace UI.Services;

public class GlobalLoadingService : IGlobalLoadingService
{
    private bool _isLoading = false;
    private int _loadingCounter = 0;
    
    public event Action? OnLoadingChanged;

    public bool IsLoading => _isLoading;

    /// <summary>
    /// Show global loading indicator
    /// </summary>
    public void ShowLoading()
    {
        _loadingCounter++;
        if (!_isLoading)
        {
            _isLoading = true;
            OnLoadingChanged?.Invoke();
        }
    }

    /// <summary>
    /// Hide global loading indicator
    /// </summary>
    public void HideLoading()
    {
        if (_loadingCounter > 0)
        {
            _loadingCounter--;
        }

        if (_loadingCounter == 0 && _isLoading)
        {
            _isLoading = false;
            OnLoadingChanged?.Invoke();
        }
    }

    /// <summary>
    /// Execute an async operation with global loading indicator
    /// </summary>
    public async Task ExecuteWithLoadingAsync(Func<Task> operation)
    {
        try
        {
            ShowLoading();
            await operation();
        }
        finally
        {
            HideLoading();
        }
    }

    /// <summary>
    /// Execute an async operation with global loading indicator and return result
    /// </summary>
    public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            ShowLoading();
            return await operation();
        }
        finally
        {
            HideLoading();
        }
    }

    /// <summary>
    /// Force hide all loading indicators
    /// </summary>
    public void ForceHideLoading()
    {
        _loadingCounter = 0;
        if (_isLoading)
        {
            _isLoading = false;
            OnLoadingChanged?.Invoke();
        }
    }
}
