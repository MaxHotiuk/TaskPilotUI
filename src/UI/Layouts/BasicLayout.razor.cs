using AntDesign.Extensions.Localization;
using AntDesign.ProLayout;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Net.Http.Json;

namespace UI.Layouts
{
    public partial class BasicLayout : LayoutComponentBase, IDisposable
    {
        private MenuDataItem[] _menuData = Array.Empty<MenuDataItem>();
        private bool collapsed;

        [Inject] private ReuseTabsService TabService { get; set; } = default!;

        public LinkItem[] Links => new[]
        {
            new LinkItem
            {
                Key = "Ant Design Blazor",
                Title = "Ant Design Blazor",
                Href = "https://antblazor.com",
                BlankTarget = true,
            },
            new LinkItem
            {
                Key = "github",
                Title = "GitHub",
                Href = "https://github.com/ant-design-blazor/ant-design-pro-blazor",
                BlankTarget = true,
            },
            new LinkItem
            {
                Key = "Blazor",
                Title = "Blazor",
                Href = "https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor?WT.mc_id=DT-MVP-5003987",
                BlankTarget = true,
            }
        };

        protected override Task OnInitializedAsync()
        {
            _menuData = new[] {
                new MenuDataItem
                {
                    Path = "/",
                    Name = "welcome",
                    Key = "welcome",
                    Icon = "smile",
                }
            };
            return Task.CompletedTask;
        }

        void Toggle()
        {
            collapsed = !collapsed;
        }

        void Reload()
        {
            TabService.ReloadPage();
        }

        public void Dispose()
        {
            
        }
    }
}
