namespace BoredGames.Core;

public class Player(string name)
{
    public readonly Guid Id = Guid.NewGuid();
    public string Username { get; } = name;
    public bool IsConnected { get; set; }
    public DateTime CreatedAt { get; } = DateTime.Now;
    
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return ((Player)obj).Id == Id;
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
        return Id.GetHashCode();
    }
}