using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UI.Interfaces.Services;
using UI.Extensions;

namespace UI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");


            builder.Services.AddApiClientsAndServices(builder.Configuration);

            var host = builder.Build();

            var authService = host.Services.GetRequiredService<IAuthService>();
            await authService.InitializeAsync();

            await host.RunAsync();
        }
    }
}