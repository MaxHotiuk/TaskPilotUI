using Microsoft.AspNetCore.Components;
using UI.Models.Board;

namespace UI.Pages.Boards.Components;

public partial class BoardCard : ComponentBase
{
    [Parameter] public BoardWithStats BoardStats { get; set; } = new();
    [Parameter] public EventCallback<string> OnBoardClick { get; set; }
    [Parameter] public EventCallback<string> OnEditBoard { get; set; }
    [Parameter] public EventCallback<string> OnDeleteBoard { get; set; }

    private BoardDto Board => BoardStats.Board;

    private RenderFragment[] _actions => new RenderFragment[]
    {
        builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "board-action-btn board-action-edit");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, HandleEditClick));
            builder.AddAttribute(3, "title", "Edit Board");
            builder.OpenComponent<AntDesign.Icon>(4);
            builder.AddAttribute(5, "Type", "edit");
            builder.CloseComponent();
            builder.CloseElement();
        },
        builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "board-action-btn board-action-view");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, HandleViewClick));
            builder.AddAttribute(3, "title", "View Board");
            builder.OpenComponent<AntDesign.Icon>(4);
            builder.AddAttribute(5, "Type", "eye");
            builder.CloseComponent();
            builder.CloseElement();
        },
        builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "board-action-btn board-action-delete");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, HandleDeleteClick));
            builder.AddAttribute(3, "title", "Delete Board");
            builder.OpenComponent<AntDesign.Icon>(4);
            builder.AddAttribute(5, "Type", "delete");
            builder.CloseComponent();
            builder.CloseElement();
        }
    };

    private async Task HandleViewClick()
    {
        await OnBoardClick.InvokeAsync(Board.Id);
    }

    private async Task HandleEditClick()
    {
        await OnEditBoard.InvokeAsync(Board.Id);
    }

    private async Task HandleDeleteClick()
    {
        await OnDeleteBoard.InvokeAsync(Board.Id);
    }

    private string GetRelativeTime(string dateTimeString)
    {
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();
            
            if (timeSpan.Days > 30)
                return $"{timeSpan.Days / 30} month{(timeSpan.Days / 30 == 1 ? "" : "s")} ago";
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days} day{(timeSpan.Days == 1 ? "" : "s")} ago";
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours} hour{(timeSpan.Hours == 1 ? "" : "s")} ago";
            if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes} minute{(timeSpan.Minutes == 1 ? "" : "s")} ago";
            
            return "Just now";
        }
        
        return "Unknown";
    }
}
