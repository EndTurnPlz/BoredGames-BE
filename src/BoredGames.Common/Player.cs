namespace BoredGames.Common;

public class Player
{
    private static readonly Dictionary<Guid, Player> PlayerMap = new();
    private readonly Guid _id = Guid.NewGuid();
    public string Username { get; init; } = "";
    public AbstractGame Game { get; set; } = null!;
    
    public bool IsConnected { get; set; } = false;
    
    public Player(out Guid playerId)
    {
        playerId = _id;
        PlayerMap[_id] = this;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return ((Player)obj)._id == _id;
    }

    public static bool operator ==(Player? a, Player? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Player? a, Player? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return !a.Equals(b);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    ~Player()
    {
        PlayerMap.Remove(_id);
        Game.LeaveGame(this);
    }

    public static Player? GetPlayer(Guid playerId)
    {
        PlayerMap.TryGetValue(playerId, out var player);
        return player;
    }
}