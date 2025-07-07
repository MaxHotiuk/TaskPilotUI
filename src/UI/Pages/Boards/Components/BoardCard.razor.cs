using Microsoft.AspNetCore.Components;
using UI.Models.Board;

namespace UI.Pages.Boards.Components;

public partial class BoardCard
{
    [Parameter] public BoardWithStats BoardStats { get; set; } = new();
    [Parameter] public EventCallback<string> OnBoardClick { get; set; }
    [Parameter] public EventCallback<string> OnEditBoard { get; set; }
    [Parameter] public EventCallback<string> OnDeleteBoard { get; set; }

    private BoardDto Board => BoardStats.Board;

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
