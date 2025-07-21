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

        public LinkItem[] Links => Array.Empty<LinkItem>();
        protected override Task OnInitializedAsync()
        {
            _menuData = new[] {
                new MenuDataItem
                {
                    Path = "/",
                    Name = "boards",
                    Key = "boards",
                    Icon = "appstore",
                },
                new MenuDataItem
                {
                    Path = "/profile",
                    Name = "profile",
                    Key = "profile",
                    Icon = "user",
                },
                new MenuDataItem
                {
                    Path = "/ai-assistant",
                    Name = "Ask AI",
                    Key = "aiAssistant",
                    Icon = "robot"
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
