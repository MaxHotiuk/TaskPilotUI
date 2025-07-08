using AntDesign.ProLayout;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using UI.Services;
using UI.Interfaces.Services;
using UI.Interfaces.Api;
using UI.Handlers;
using Refit;
using System.Text.Json;

namespace UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");
            
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                })
            };

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
            
            builder.Services.AddScoped<AuthenticationHandler>();

            builder.Services.AddRefitClient<IUserApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
                
            builder.Services.AddRefitClient<IBoardApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
                
            builder.Services.AddRefitClient<IBoardMemberApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
                
            builder.Services.AddRefitClient<IBoardTaskApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
                
            builder.Services.AddRefitClient<IBoardStateApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
                
            builder.Services.AddRefitClient<ICommentApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
                
            builder.Services.AddRefitClient<ITaskPilotAuthApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));
                
            builder.Services.AddRefitClient<IMicrosoftGraphApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://graph.microsoft.com"));
                
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<IAzureAdTokenApi>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                return RestService.For<IAzureAdTokenApi>(httpClient, refitSettings);
            });

            builder.Services.AddAntDesign();
            builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));
            
            builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IBoardService, BoardService>();
            builder.Services.AddScoped<IBoardMemberService, BoardMemberService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<ITaskStateService, TaskStateService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddSingleton<IGlobalLoadingService, GlobalLoadingService>();

            var host = builder.Build();
            
            var authService = host.Services.GetRequiredService<IAuthService>();
            await authService.InitializeAsync();

            await host.RunAsync();
        }
    }
}