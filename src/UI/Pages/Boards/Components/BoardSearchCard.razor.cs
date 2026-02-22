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
        MessageService.Info(UI.Resources.I18n.RefreshToSeeChanges);
    }

    private string GetRelativeTime(string dateTimeString)
    {
        if (DateTime.TryParse(dateTimeString, out var dateTime))
        {
            var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();

            if (timeSpan.Days > 365)
            {
                var years = timeSpan.Days / 365;
                return string.Format(UI.Resources.I18n.YearsAgo, years);
            }
            if (timeSpan.Days > 30)
            {
                var months = timeSpan.Days / 30;
                return string.Format(UI.Resources.I18n.MonthsAgo, months);
            }
            if (timeSpan.Days > 0)
                return $"{timeSpan.Days} дн тому";
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours} год тому";
            if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes} хв тому";

            return UI.Resources.I18n.JustNow;
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
            builder.AddAttribute(2, "title", UI.Resources.I18n.View);
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, HandleViewClick));
            builder.OpenComponent(4, typeof(Icon));
            builder.AddAttribute(5, "Type", "eye");
            builder.CloseComponent();
            builder.OpenElement(6, "span");
            builder.AddContent(7, UI.Resources.I18n.View);
            builder.CloseElement();
            builder.CloseElement();
        });

        if (IsOwner)
        {
            actions.Add(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "board-action-btn board-action-delete");
                builder.AddAttribute(2, "title", UI.Resources.I18n.DeleteBoardTitle);
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, HandleDeleteClick));
                builder.OpenComponent(4, typeof(Icon));
                builder.AddAttribute(5, "Type", "delete");
                builder.CloseComponent();
                builder.OpenElement(6, "span");
                builder.AddContent(7, UI.Resources.I18n.DeleteLabel);
                builder.CloseElement();
                builder.CloseElement();
            });
        }

        return [.. actions];
    }
}