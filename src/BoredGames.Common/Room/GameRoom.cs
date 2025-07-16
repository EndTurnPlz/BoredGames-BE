using BoredGames.Common.Game;

namespace BoredGames.Common.Room;

public class GameRoom
{
    public int ViewNum { get; private set; }
    public Guid Id { get; } = Guid.NewGuid();
    private DateTime CreatedAt { get; } = DateTime.Now;
    private readonly Player _host;
    private readonly List<Player> _players = [];
    public State CurrentState { get; private set; } = State.WaitingForPlayers;

    private readonly AbstractGameConfig _gameConfig;
    public AbstractGame? Game; // Make this private in a later refactor
    
    private static TimeSpan AbandonedTimeout => TimeSpan.FromMinutes(5);

    public GameRoom(Player host, AbstractGameConfig gameConfig)
    {
        _gameConfig = gameConfig;
        _host = host;
        Join(host);
    }
    
    public bool IsDead()
    {
        if (CurrentState is State.GameInProgress) return false;
        if (DateTime.Now - CreatedAt > AbandonedTimeout) return true;
        if (_players.Count == 0) return true;

        return false;
    }

    public void Join(Player player)
    {
        if (CurrentState is not State.WaitingForPlayers) throw new RoomNotFoundException();
        if (_players.Count >= _gameConfig.MaxPlayerCount) throw new RoomIsFullException();

        ViewNum++;
        _players.Add(player);
    }

    public void Leave(Player player)
    {
        if (!_players.Contains(player)) throw new PlayerNotFoundException();
        
        ViewNum++;
        
        if (player == _host) {
            _players.Clear();
            return;
        }
        
        _players.Remove(player);
    }

    public void StartGame(Guid playerId)
    {
        if (CurrentState is not State.WaitingForPlayers) throw new RoomCannotStartException();
        if (!_host.ValidateId(playerId)) throw new PlayerNotHostException();
        if (_players.Count < _gameConfig.MinPlayerCount) throw new RoomCannotStartException();
        
        Game = Activator.CreateInstance(_gameConfig.GameType) as AbstractGame;
        CurrentState = State.GameInProgress;
    }
    
    public IGameSnapshot? GetGameSnapshot() => Game?.GetSnapshot();
    
    public IEnumerable<string> GetPlayerNames() => _players.Select(p => p.Username);
    
    public enum State
    {
        WaitingForPlayers,
        GameInProgress,
        GameEnded, // Implement this when there's a single game action function
    }
}