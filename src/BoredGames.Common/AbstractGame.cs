namespace BoredGames.Common;

public abstract class AbstractGame(Player host)
{
    public int CurrentView { get; protected set; }
    public bool HasStarted => CurrentView > 0;
    protected Player Host { get; set; } = host;

    public abstract bool StartGame(Player player);

    public abstract void JoinGame(Player player);
    public abstract void LeaveGame(Player player);
}

public enum GameTypes
{
    Apologies,
    NGameTypes
}