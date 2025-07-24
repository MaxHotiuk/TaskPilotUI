using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UI.Interfaces.Services;
using UI.Models.Backlog;

namespace UI.Pages.Board;

public partial class Backlog : ComponentBase
{
    [Parameter] public string BoardId { get; set; } = string.Empty;

    private Guid boardIdGuid;

    [Inject] public ITaskService TaskService { get; set; } = default!;
    [Inject] public IBoardService BoardService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    public List<BacklogDto> BacklogItems { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public bool InitLoading { get; set; } = true;
    public bool Loading { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool HasMore { get; set; } = true;
    public string BoardName { get; set; } = string.Empty;

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!Guid.TryParse(BoardId, out boardIdGuid))
        {
            boardIdGuid = Guid.Empty;
        }
        await LoadBackLog(reset: true);
        if (boardIdGuid != Guid.Empty)
        {
            var board = await BoardService.GetByIdAsync(boardIdGuid);
            BoardName = board?.Name ?? "Unknown Board";
        }
        else
        {
            NavigationManager.NavigateTo("/boards");
        }
        InitLoading = false;
    }

    public void OnBack()
    {
        NavigationManager.NavigateTo($"/board/{BoardId}");
    }


    public async Task OnSearch(ChangeEventArgs args)
    {
        SearchTerm = args.Value?.ToString() ?? string.Empty;
        await LoadBackLog(reset: true);
    }

    public async Task OnDateChanged()
    {
        await LoadBackLog(reset: true);
    }

    public async Task LoadMore()
    {
        Page++;
        await LoadBackLog(reset: false);
    }

    private async Task LoadBackLog(bool reset)
    {
        Loading = true;
        var result = await BoardService.SearchBacklogRangeForBoardAsync(
            boardIdGuid,
            SearchTerm,
            Page,
            PageSize,
            StartDate ?? DateOnly.MinValue,
            EndDate ?? DateOnly.MaxValue
        );
        var resultList = result.ToList();
        Console.WriteLine($"Loaded {resultList.Count} backlog items for page {Page} with search term '{SearchTerm}', startDate '{StartDate}', endDate '{EndDate}'");
        if (reset)
            BacklogItems = resultList;
        else
            BacklogItems.AddRange(resultList);
        HasMore = resultList.Count == PageSize;
        Loading = false;
    }
}
