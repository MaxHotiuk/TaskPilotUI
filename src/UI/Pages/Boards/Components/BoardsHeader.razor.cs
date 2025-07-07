using Microsoft.AspNetCore.Components;

namespace UI.Pages.Boards.Components;

public partial class BoardsHeader
{
    [Parameter] public string SearchTerm { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> SearchTermChanged { get; set; }
    [Parameter] public string FilterType { get; set; } = "all";
    [Parameter] public EventCallback<ChangeEventArgs> OnSearchChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterChanged { get; set; }
}
