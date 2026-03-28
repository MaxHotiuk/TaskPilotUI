using Microsoft.AspNetCore.Components;
using Refit;
using UI.Interfaces.SignalR;
using System.Text.Json;
using UI.Handlers;
using UI.Interfaces.Api;
using UI.Interfaces.Services;
using UI.Services;
using AntDesign.ProLayout;

namespace UI.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiClientsAndServices(this IServiceCollection services, IConfiguration configuration)
        {
            var apiBaseUrl = configuration["Api:BaseUrl"] ?? throw new InvalidOperationException("API Base URL is not configured.");

            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                })
            };

            services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
            services.AddScoped<AuthenticationHandler>();

            services.AddRefitClient<IUserApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IBoardApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IBoardMemberApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IBoardTaskApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IBoardStateApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<ICommentApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<ITaskPilotAuthApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));

            services.AddRefitClient<IAttachmentApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IAvatarApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IChatApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IChatSystemApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<INotificationApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
            
            services.AddRefitClient<ITagApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IMeetingApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();
            
            services.AddRefitClient<IMeetingMemberApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IOrganizationApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IInvitationApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
                .AddHttpMessageHandler<AuthenticationHandler>();

            services.AddRefitClient<IMicrosoftGraphApi>(refitSettings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://graph.microsoft.com"));

            services.AddHttpClient();
            services.AddTransient<IAzureAdTokenApi>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                return RestService.For<IAzureAdTokenApi>(httpClient, refitSettings);
            });

            services.AddAntDesign();
            services.Configure<ProSettings>(configuration.GetSection("ProSettings"));

            services.AddScoped<ILocalStorageService, LocalStorageService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IBoardService, BoardService>();
            services.AddScoped<IBoardMemberService, BoardMemberService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<ITaskStateService, TaskStateService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddSingleton<IGlobalLoadingService, GlobalLoadingService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddScoped<IAvatarService, AvatarService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IChatSystemService, ChatSystemService>();
            services.AddScoped<IColorService, ColorService>();
            services.AddScoped<Interfaces.Services.INotificationService, Services.NotificationService>();
            services.AddScoped<IMeetingService, MeetingService>();
            services.AddScoped<IMeetingMemberService, MeetingMemberService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<IInvitationService, InvitationService>();
            services.AddSingleton<IPublicDomainService, PublicDomainService>();
            services.AddScoped<INotificationSignalRService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NotificationSignalRService>>();
                var messageService = sp.GetRequiredService<IMessageService>();
                return new NotificationSignalRService(logger, config, messageService);
            });

            services.AddScoped<ISignalRService, SignalRService>(sp =>
            {
                var navigationManager = sp.GetRequiredService<NavigationManager>();
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SignalRService>>();
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new SignalRService(navigationManager, logger, configuration);
            });

            services.AddScoped<IChatSignalRService>(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ChatSignalRService>>();
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new ChatSignalRService(logger, configuration);
            });

            return services;
        }
    }
}
