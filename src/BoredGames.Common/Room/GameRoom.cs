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

    private readonly GameConfig _gameConfig;
    public GameBase? Game; // Make this private in a later refactor
    
    private static TimeSpan AbandonedTimeout => TimeSpan.FromMinutes(5);

    public GameRoom(Player host, GameConfig gameConfig)
    {
        _gameConfig = gameConfig;
        _host = host;
        AddPlayer(host);
    }
    
    public bool IsDead()
    {
        if (CurrentState is State.GameInProgress) return false;
        if (DateTime.Now - CreatedAt > AbandonedTimeout) return true;
        if (_players.Count == 0) return true;

        return false;
    }

    public void AddPlayer(Player player)
    {
        if (CurrentState is not State.WaitingForPlayers) throw new RoomNotFoundException();
        if (_players.Count >= _gameConfig.MaxPlayerCount) throw new RoomIsFullException();

        ViewNum++;
        _players.Add(player);
    }

    public void RegisterPlayerConnected(Guid playerId)
    {
        if (!_players.Any(p => p.ValidateId(playerId))) throw new PlayerNotFoundException();
        
        var player = _players.First(p => p.ValidateId(playerId));
        player.IsConnected = true;
    }

    public void RegisterPlayerDisconnected(Guid playerId)
    {
        if (!_players.Any(p => p.ValidateId(playerId))) throw new PlayerNotFoundException();
        
        var player = _players.First(p => p.ValidateId(playerId));
        player.IsConnected = false;

        if (CurrentState is not State.GameInProgress) {
            RemovePlayer(player);
        } 
    }

    private void RemovePlayer(Player player)
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
        Game = _gameConfig.CreateGameInstance(_players);
        CurrentState = State.GameInProgress;
        ViewNum++;
    }

    public void ExecuteGameAction(string action, Guid? playerId = null, IGameActionArgs? args = null)
    {
        if (CurrentState is State.GameInProgress) throw new RoomNotStartedException();
        
        var player = playerId is not null ? _players.FirstOrDefault(p => p.ValidateId(playerId)) : null;
        Game!.ExecuteAction(action, player, args);
        if (Game!.HasEnded()) CurrentState = State.GameEnded;
    }
    
    public IGameSnapshot? GetGameSnapshot() => Game?.GetSnapshot();
    
    public IEnumerable<string> GetPlayerNames() => _players.Select(p => p.Username);
    
    public enum State
    {
        WaitingForPlayers,
        GameInProgress,
        GameEnded
    }
}