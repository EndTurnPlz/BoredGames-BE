namespace BoredGames.Core;

public class Player
{
    private readonly Guid _id = Guid.NewGuid();
    public string Username { get; init; } = "";
    public bool IsConnected { get; set; }

    public Player(out Guid playerId)
    {
        playerId = _id;
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

    internal bool ValidateId(Guid? id)
    {
        return _id == id;
    }
}