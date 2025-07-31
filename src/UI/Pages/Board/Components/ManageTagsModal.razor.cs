using Microsoft.AspNetCore.Components;
using UI.Models.Tag;
using UI.Interfaces.Services;

namespace UI.Pages.Board.Components;

public partial class ManageTagsModal : ComponentBase
{
    [Inject] private ITagService TagService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<List<TagDto>>? OnTagsChanged { get; set; }
    public List<TagDto> Tags { get; set; } = new();
    private bool _showAddTagModal;
    private bool _isAddingTag;
    private CreateTagRequestDto _addTagForm = new CreateTagRequestDto { Color = "#0091ff" };

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && !string.IsNullOrEmpty(BoardId))
        {
            await LoadTagsAsync();
        }
    }

    private async Task LoadTagsAsync()
    {
        Tags = (await TagService.GetByBoardIdAsync(Guid.Parse(BoardId))).ToList();
        StateHasChanged();
    }

    private void ShowAddTagModal()
    {
        _showAddTagModal = true;
    }

    private void ResetAddTagForm()
    {
        _addTagForm = new CreateTagRequestDto { Color = "#039fff" };
    }

    private async Task AddTag()
    {
        if (string.IsNullOrWhiteSpace(_addTagForm.Name))
        {
            Message.Error("Please enter a tag name");
            return;
        }
        try
        {
            _isAddingTag = true;
            await TagService.CreateAsync(Guid.Parse(BoardId), _addTagForm);
            await LoadTagsAsync();
            _showAddTagModal = false;
            Message.Success($"Tag '{_addTagForm.Name}' added successfully");
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to add tag: {ex.Message}");
        }
        finally
        {
            _isAddingTag = false;
            StateHasChanged();
            if (OnTagsChanged.HasValue)
                await OnTagsChanged.Value.InvokeAsync(Tags);
        }
    }

    private async Task OnNameChanged(TagDto tag)
    {
        await UpdateTag(tag);
    }

    private async Task OnColorChanged(TagDto tag)
    {
        await UpdateTag(tag);
    }

    private async Task UpdateTag(TagDto tag)
    {
        await TagService.UpdateAsync(Guid.Parse(BoardId), tag.Id, new UpdateTagRequestDto { Name = tag.Name, Color = tag.Color });
        await LoadTagsAsync();
        if (OnTagsChanged.HasValue)
            await OnTagsChanged.Value.InvokeAsync(Tags);
    }

    private async Task OnDeleteTag(TagDto tag)
    {
        await TagService.DeleteAsync(Guid.Parse(BoardId), tag.Id);
        await LoadTagsAsync();
        if (OnTagsChanged.HasValue)
            await OnTagsChanged.Value.InvokeAsync(Tags);
    }
}
