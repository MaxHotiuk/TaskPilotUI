using Microsoft.AspNetCore.Components;
using UI.Models.State;
using UI.Interfaces.Services;

namespace UI.Pages.Board.Components;

public partial class ManageStatesModal : ComponentBase
{
    [Inject] private ITaskStateService TaskStateService { get; set; } = default!;
    [Inject] private IMessageService Message { get; set; } = default!;
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string BoardId { get; set; } = string.Empty;
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<List<StateDto>>? OnStatesChanged { get; set; }
    public List<StateDto> States { get; set; } = new();
    private bool _showAddStateModal;
    private bool _isAddingState;
    private CreateStateRequest _addStateForm = new();
    private int _nextOrder = 1;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        UpdateNextOrder();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (IsVisible && !string.IsNullOrEmpty(BoardId))
        {
            await LoadStatesAsync();
        }
    }

    private void UpdateNextOrder()
    {
        _nextOrder = States?.Count > 0 ? States.Max(s => s.Order) + 1 : 1;
    }

    private async Task LoadStatesAsync()
    {
        States = await TaskStateService.GetBoardStatesAsync(BoardId);
        States = States.OrderBy(s => s.Order).ToList();
        UpdateNextOrder();
        StateHasChanged();
    }

    private void ShowAddStateModal()
    {
        UpdateNextOrder();
        _showAddStateModal = true;
    }

    private void ResetAddStateForm()
    {
        _addStateForm = new CreateStateRequest();
    }

    private async Task AddState()
    {
        if (string.IsNullOrWhiteSpace(_addStateForm.Name))
        {
            Message.Error("Please enter a state name");
            return;
        }

        try
        {
            _isAddingState = true;
            StateHasChanged();
            _addStateForm.Order = _nextOrder;

            var stateId = await TaskStateService.CreateAsync(BoardId, _addStateForm);

            await LoadStatesAsync();

            _showAddStateModal = false;
            Message.Success($"State '{_addStateForm.Name}' added successfully");
        }
        catch (Exception ex)
        {
            Message.Error($"Failed to add state: {ex.Message}");
        }
        finally
        {
            _isAddingState = false;
            StateHasChanged();
            if (OnStatesChanged.HasValue)
                await OnStatesChanged.Value.InvokeAsync(States);
        }
    }

    private async Task OnNameChanged(StateDto state)
    {
        await UpdateState(state);
    }

    private async Task UpdateState(StateDto state)
    {
        await TaskStateService.UpdateAsync(state.Id, new UpdateStateRequest { Name = state.Name, Order = state.Order });
        await LoadStatesAsync();
        if (OnStatesChanged.HasValue)
            await OnStatesChanged.Value.InvokeAsync(States);
    }

    private async Task OnDeleteState(StateDto state)
    {
        await TaskStateService.DeleteAsync(state.Id);
        await LoadStatesAsync();
        if (OnStatesChanged.HasValue)
            await OnStatesChanged.Value.InvokeAsync(States);
    }

    private bool IsFirstState(StateDto state)
    {
        return States.OrderBy(s => s.Order).First().Id == state.Id;
    }

    private bool IsLastState(StateDto state)
    {
        return States.OrderBy(s => s.Order).Last().Id == state.Id;
    }

    private async Task MoveStateUp(StateDto state)
    {
        var orderedStates = States.OrderBy(s => s.Order).ToList();
        var currentIndex = orderedStates.FindIndex(s => s.Id == state.Id);

        if (currentIndex > 0)
        {
            var prevState = orderedStates[currentIndex - 1];

            var request = new SwapStateOrderRequest
            {
                FirstStateId = state.Id,
                SecondStateId = prevState.Id
            };

            await TaskStateService.SwapOrderAsync(BoardId, request);
            await LoadStatesAsync();
            if (OnStatesChanged.HasValue)
                await OnStatesChanged.Value.InvokeAsync(States);
        }
    }

    private async Task MoveStateDown(StateDto state)
    {
        var orderedStates = States.OrderBy(s => s.Order).ToList();
        var currentIndex = orderedStates.FindIndex(s => s.Id == state.Id);

        if (currentIndex < orderedStates.Count - 1)
        {
            var nextState = orderedStates[currentIndex + 1];

            var request = new SwapStateOrderRequest
            {
                FirstStateId = state.Id,
                SecondStateId = nextState.Id
            };

            await TaskStateService.SwapOrderAsync(BoardId, request);
            await LoadStatesAsync();
            if (OnStatesChanged.HasValue)
                await OnStatesChanged.Value.InvokeAsync(States);
        }
    }
}