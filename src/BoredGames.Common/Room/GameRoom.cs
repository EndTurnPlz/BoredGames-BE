using BoredGames.Common.Game;

namespace BoredGames.Common.Room;

public class GameRoom
{
    public int ViewNum { get; private set; }
    public Guid Id { get; } = Guid.NewGuid();
    private readonly Player _host;
    private readonly List<Player> _players = [];
    public State CurrentState = State.WaitingForPlayers;
    
    private readonly AbstractGameConfig _gameConfig;
    private AbstractGame? _game;
    
    public GameRoom(Player host, AbstractGameConfig gameConfig)
    {
        _gameConfig = gameConfig;
        _host = host;
        Join(host);
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
            CurrentState = State.Dead;
            _players.Clear();
            return;
        }
        
        _players.Remove(player);
    }

    public void StartGame(Guid playerId)
    {
        if (CurrentState is not State.WaitingForPlayers) throw new RoomCannotStartException();
        if (_host.ValidateId(playerId)) throw new PlayerNotHostException();
        if (_players.Count < _gameConfig.MinPlayerCount) throw new RoomCannotStartException();
        
        _game = Activator.CreateInstance(_gameConfig.GameType) as AbstractGame;
        CurrentState = State.GameStarted;
    }
    
    public IGameSnapshot? GetGameSnapshot() => _game?.GetSnapshot();
    
    public IEnumerable<string> GetPlayerNames() => _players.Select(p => p.Username);
    
    public enum State
    {
        WaitingForPlayers,
        GameStarted,
        GameEnded,
        Dead
    }
}