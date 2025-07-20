using System.Collections.Immutable;
using BoredGames.Core.Game;

namespace BoredGames.Core.Room;

public class GameRoom
{
    public int ViewNum { get; private set; }
    public Guid Id { get; } = Guid.NewGuid();
    private DateTime LastIdleAt { get; set; } = DateTime.Now;
    private readonly Player _host;
    private readonly List<Player> _players = [];
    public State CurrentState { get; private set; } = State.WaitingForPlayers;

    private readonly IGameConfig _gameConfig;
    private GameBase? _game; // Make this private in a later refactor
    
    private static TimeSpan AbandonedTimeout => TimeSpan.FromMinutes(5);

    public GameRoom(Player host, IGameConfig gameConfig)
    {
        _gameConfig = gameConfig;
        _host = host;
        AddPlayer(host);
    }
    
    public bool IsDead()
    {
        if (CurrentState is State.GameInProgress) return false;
        if (_players.Count == 0) return true;
        if (DateTime.Now - LastIdleAt > AbandonedTimeout) return true;

        return false;
    }

    public void AddPlayer(Player player)
    {
        if (CurrentState is not State.WaitingForPlayers) throw new RoomNotFoundException();
        if (_players.Count >= _gameConfig.MaxPlayerCount) throw new RoomIsFullException();

        _players.Add(player);
        ViewNum++;
    }

    public void RegisterPlayerConnected(Guid playerId)
    {
        if (!_players.Any(p => p.ValidateId(playerId))) throw new PlayerNotFoundException();
        var player = _players.First(p => p.ValidateId(playerId));
        player.IsConnected = true;
        ViewNum++;
    }

    public void RegisterPlayerDisconnected(Guid playerId)
    {
        if (!_players.Any(p => p.ValidateId(playerId))) throw new PlayerNotFoundException();
        
        var player = _players.First(p => p.ValidateId(playerId));
        player.IsConnected = false;

        if (CurrentState is State.WaitingForPlayers) {
            RemovePlayer(player);
        } 
        
        ViewNum++;
    }

    private void RemovePlayer(Player player)
    {
        if (!_players.Contains(player)) throw new PlayerNotFoundException();
        
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
        _game = _gameConfig.CreateGameInstance(_players.ToImmutableList());
        CurrentState = State.GameInProgress;
        ViewNum++;
    } 

    public IGameActionResponse? ExecuteGameAction(IGameActionArgs args, Guid? playerId = null)
    {
        if (CurrentState is not State.GameInProgress) throw new RoomNotStartedException();
        
        var player = playerId is not null ? _players.FirstOrDefault(p => p.ValidateId(playerId)) : null;
        var result = _game!.ExecuteAction(args, player);
        if (_game!.HasEnded()) 
        {
            LastIdleAt = DateTime.Now;
            CurrentState = State.GameEnded;
        }
        
        ViewNum++;
        return result;
    }
    
    public IGameSnapshot? GetGameSnapshot() => _game?.GetSnapshot();
    
    public IEnumerable<string> GetPlayerNames() => _players.Select(p => p.Username);
    
    public enum State
    {
        WaitingForPlayers,
        GameInProgress,
        GameEnded
    }
}