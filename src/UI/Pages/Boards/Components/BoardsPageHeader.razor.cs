using Microsoft.AspNetCore.Components;

namespace UI.Pages.Boards.Components;

public partial class BoardsPageHeader
{
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public bool ShowHeader { get; set; }
    [Parameter] public string SearchTerm { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SearchTermChanged { get; set; }
    [Parameter] public string FilterType { get; set; } = "all";
    [Parameter] public EventCallback OnRefreshClicked { get; set; }
    [Parameter] public EventCallback OnCreateClicked { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnSearchChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterChanged { get; set; }
}
