using UI.Interfaces.Services;

namespace UI.Extensions;

public static class ServiceExtensions
{
    public static async Task ExecuteWithGlobalLoadingAsync<T>(
        this T service, 
        IGlobalLoadingService loadingService, 
        Func<T, Task> operation)
    {
        await loadingService.ExecuteWithLoadingAsync(() => operation(service));
    }

    public static async Task<TResult> ExecuteWithGlobalLoadingAsync<T, TResult>(
        this T service, 
        IGlobalLoadingService loadingService, 
        Func<T, Task<TResult>> operation)
    {
        return await loadingService.ExecuteWithLoadingAsync(() => operation(service));
    }

    public static async Task ExecuteMultipleWithGlobalLoadingAsync<T>(
        this T service,
        IGlobalLoadingService loadingService,
        params Func<T, Task>[] operations)
    {
        await loadingService.ExecuteWithLoadingAsync(async () =>
        {
            var tasks = operations.Select(op => op(service));
            await Task.WhenAll(tasks);
        });
    }

    public static async Task ExecuteWithGlobalLoadingAndErrorHandlingAsync<T>(
        this T service,
        IGlobalLoadingService loadingService,
        Func<T, Task> operation,
        Func<Exception, Task>? onError = null,
        Func<Task>? onFinally = null)
    {
        await loadingService.ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                await operation(service);
            }
            catch (Exception ex)
            {
                if (onError != null)
                    await onError(ex);
                else
                    throw;
            }
            finally
            {
                if (onFinally != null)
                    await onFinally();
            }
        });
    }

    public static async Task<TResult> ExecuteWithGlobalLoadingAndErrorHandlingAsync<T, TResult>(
        this T service,
        IGlobalLoadingService loadingService,
        Func<T, Task<TResult>> operation,
        Func<Exception, Task<TResult>>? onError = null,
        Func<Task>? onFinally = null)
    {
        return await loadingService.ExecuteWithLoadingAsync(async () =>
        {
            try
            {
                return await operation(service);
            }
            catch (Exception ex)
            {
                if (onError != null)
                    return await onError(ex);
                else
                    throw;
            }
            finally
            {
                if (onFinally != null)
                    await onFinally();
            }
        });
    }
}
