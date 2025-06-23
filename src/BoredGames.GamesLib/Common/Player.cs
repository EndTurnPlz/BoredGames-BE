namespace BoredGames.GamesLib.Common;

public class Player
{
    public Player(ref AbstractGame game)
    {
        Game = game;
        PlayerMap[_id] = this;
    }

    ~Player()
    {
        PlayerMap.Remove(_id);
    }

    public string Username { get; set; } = "";
    public AbstractGame Game { get; }
    private int HighestKnownView { get; set; }
    private readonly Guid _id = Guid.NewGuid();

    public void Heartbeat(int view)
    {
        if (view > Game.CurrentView) return;
        HighestKnownView = Math.Max(HighestKnownView, view);
    }

    public bool HasCurrentView()
    {
        return HighestKnownView == Game.CurrentView;
    }

    public static Player? GetPlayer(Guid playerId)
    {
        PlayerMap.TryGetValue(playerId, out var player);
        return player;
    }

    private static readonly Dictionary<Guid, Player> PlayerMap = new();
}