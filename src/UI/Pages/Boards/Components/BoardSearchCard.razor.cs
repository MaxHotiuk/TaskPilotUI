using Microsoft.AspNetCore.Components;
using UI.Models.Board;
using AntDesign;
using UI.Interfaces.Services;

namespace UI.Pages.Boards.Components;

public partial class BoardSearchCard
{
    [Inject] private IBoardService BoardService { get; set; } = default!;
    [Inject] private MessageService MessageService { get; set; } = default!;
    [Parameter] public BoardSearchDto Board { get; set; } = new();
    [Parameter] public bool IsOwner { get; set; }
    [Parameter] public bool IsArchived { get; set; }
    [Parameter] public EventCallback<string> OnBoardClick { get; set; }
    [Parameter] public EventCallback<string> OnEditBoard { get; set; }
    [Parameter] public EventCallback<string> OnDeleteBoard { get; set; }
    
    [Parameter]
    public bool AlwaysShowActions { get; set; }

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

    private async Task DearchiveBoardAsync()
    {
        await BoardService.DearchiveBoardAsync(Board.Id);
        StateHasChanged();
        MessageService.Info("Refresh to see changes");
    }

    private string GetRelativeTime(string dateTimeString)
    {
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

            if (timeSpan.Days > 365)
            {
                var years = timeSpan.Days / 365;
                return $"{years} year{(years == 1 ? "" : "s")} ago";
            }
            if (timeSpan.Days > 30)
            {
                var months = timeSpan.Days / 30;
                return $"{months} month{(months == 1 ? "" : "s")} ago";
            }
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
    
    public RenderFragment[] GetCardActions()
    {
        var actions = new List<RenderFragment>();

        actions.Add(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "board-action-btn board-action-view");
            builder.AddAttribute(2, "title", "View Board");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, HandleViewClick));
            builder.OpenComponent(4, typeof(Icon));
            builder.AddAttribute(5, "Type", "eye");
            builder.CloseComponent();
            builder.OpenElement(6, "span");
            builder.AddContent(7, "View");
            builder.CloseElement();
            builder.CloseElement();
        });

        if (IsOwner)
        {
            actions.Add(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "board-action-btn board-action-delete");
                builder.AddAttribute(2, "title", "Delete Board");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, HandleDeleteClick));
                builder.OpenComponent(4, typeof(Icon));
                builder.AddAttribute(5, "Type", "delete");
                builder.CloseComponent();
                builder.OpenElement(6, "span");
                builder.AddContent(7, "Delete");
                builder.CloseElement();
                builder.CloseElement();
            });
        }

        return [.. actions];
    }
}