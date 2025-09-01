namespace HallApp.Web.Managers;

public static class NotificationConnectionManager
{
    private static readonly Dictionary<int, string> _connections = new Dictionary<int, string>();

    public static void AddConnection(int userId, string connectionId)
    {
        _connections[userId] = connectionId;
    }

    public static string GetConnectionId(int userId)
    {
        _connections.TryGetValue(userId, out var connectionId);
        return connectionId;
    }

    public static void RemoveConnection(string connectionId)
    {
        var userId = _connections.FirstOrDefault(x => x.Value == connectionId).Key;
        if (userId != 0)
        {
            _connections.Remove(userId);
        }
    }
}
