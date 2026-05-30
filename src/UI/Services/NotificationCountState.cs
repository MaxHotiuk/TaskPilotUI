namespace UI.Services;

public class NotificationCountState
{
    private int _unreadCount;

    public int UnreadCount => _unreadCount;

    public event Action? OnChange;

    public void SetCount(int count)
    {
        _unreadCount = count;
        OnChange?.Invoke();
    }

    public void Decrement()
    {
        if (_unreadCount > 0)
        {
            _unreadCount--;
            OnChange?.Invoke();
        }
    }

    public void Reset() => SetCount(0);
}
