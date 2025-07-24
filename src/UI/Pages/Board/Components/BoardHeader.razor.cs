using Microsoft.AspNetCore.Components;
using UI.Models.Board;

namespace UI.Pages.Board.Components;

public partial class BoardHeader : ComponentBase
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public BoardDetailDto? BoardDetail { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }
    [Parameter] public EventCallback OnShowMembers { get; set; }
    [Parameter] public EventCallback OnRefresh { get; set; }
    [Parameter] public EventCallback OnCreateTask { get; set; }
    [Parameter] public EventCallback OnManageStates { get; set; }
    [Parameter] public EventCallback OnManageTags { get; set; }
    [Parameter] public string? CurrentUserId { get; set; }
    [Parameter] public EventCallback OnArchive { get; set; }
    [Parameter] public EventCallback OnOnlyMine { get; set; }
    public bool IsOnlyMine { get; set; } = false;

    public void OnOnlyMineToggle()
    {
        IsOnlyMine = !IsOnlyMine;
        OnOnlyMine.InvokeAsync(IsOnlyMine);
    }

    public void OnCreateCall()
    {
        NavigationManager.NavigateTo($"/board/{BoardDetail?.Id}/call");
    }

    public void OnBacklog()
    {
        if (BoardDetail != null)
        {
            NavigationManager.NavigateTo($"/board/{BoardDetail.Id}/backlog");
        }
    }
}
