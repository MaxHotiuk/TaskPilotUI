using AntDesign.ProLayout;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using UI.Services;

namespace UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            // Configure HttpClient with the API base URL
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
            builder.Services.AddAntDesign();
            builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));
            
            // Register services
            builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IBoardService, BoardService>();

            var host = builder.Build();
            
            // Initialize AuthService to set up token state without making API calls
            var authService = host.Services.GetRequiredService<IAuthService>();
            await authService.InitializeAsync();

            await host.RunAsync();
        }
    }
}