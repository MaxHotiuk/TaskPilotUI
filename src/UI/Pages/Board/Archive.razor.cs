using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UI.Interfaces.Services;
using UI.Models.Task;

namespace UI.Pages.Board;

public partial class Archive : ComponentBase
{
    [Parameter] public string BoardId { get; set; } = string.Empty;

    private Guid boardIdGuid;

    [Inject] public ITaskService TaskService { get; set; } = default!;
    [Inject] public IBoardService BoardService { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    public List<ArchivedTaskDto> ArchivedTasks { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public bool InitLoading { get; set; } = true;
    public bool Loading { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool HasMore { get; set; } = true;
    public string BoardName { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        if (!Guid.TryParse(BoardId, out boardIdGuid))
        {
            boardIdGuid = Guid.Empty;
        }
        await LoadTasks(reset: true);
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
        await LoadTasks(reset: true);
    }

    public async Task LoadMore()
    {
        Page++;
        await LoadTasks(reset: false);
    }

    private async Task LoadTasks(bool reset)
    {
        Loading = true;
        var result = await TaskService.SearchArchivedRangeTaskItemsAsync(Page, PageSize, SearchTerm, boardIdGuid);
        Console.WriteLine($"Loaded {result.Count} tasks for page {Page} with search term '{SearchTerm}'");
        if (reset)
            ArchivedTasks = result;
        else
            ArchivedTasks.AddRange(result);
        HasMore = result.Count == PageSize;
        Loading = false;
    }

    public RenderFragment[] GetActions(ArchivedTaskDto item)
    {
        return
        [
            builder =>
            {
                builder.OpenComponent<Button>(0);
                builder.AddAttribute(1, "Type", ButtonType.Link);
                builder.AddAttribute(2, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => Restore(item.Id)));
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(4, "Restore")));
                builder.CloseComponent();
            }
        ];
    }

    public async Task Restore(Guid taskId)
    {
        await TaskService.RestoreAsync(taskId);
        Page = 1;
        await LoadTasks(reset: true);
    }
}
