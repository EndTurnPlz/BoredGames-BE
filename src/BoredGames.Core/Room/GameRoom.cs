using System.Collections.Immutable;
using BoredGames.Core.Game;

namespace BoredGames.Core.Room;

public class GameRoom
{
    // Room info
    private int ViewNum { get; set; }
    public Guid Id { get; } = Guid.NewGuid();
    private State CurrentState { get; set; } = State.WaitingForPlayers;
    private DateTime LastIdleAt { get; set; } = DateTime.Now;
    
    // Players
    private readonly Player _host;
    private readonly List<Player> _players = [];
    public IReadOnlyList<Player> Players => _players;
    private readonly List<Player> _pendingPlayers = [];

    // Game
    private readonly IGameConfig _gameConfig;
    private GameBase? _game;
    
    // Concurrency
    private readonly Lock _lock = new();
    
    // Event Emitter
    public event EventHandler? RoomStateChanged;
    
    private void OnRoomStateChanged()
    {
        RoomStateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public GameRoom(Player host, IGameConfig gameConfig)
    {
        _gameConfig = gameConfig;
        _host = host;
        AddPendingPlayer(host);
    }
    
    public bool IsDead(TimeSpan abandonedTimeout)
    {
        lock (_lock) {
            if (_players.Count == 0) return true;
            return DateTime.Now - LastIdleAt > abandonedTimeout;
        }
    }

    public bool MayHaveExpiredPlayers()
    {
        lock (_lock) {
            return _pendingPlayers.Count > 0;
        }   
    }

    public void AddPendingPlayer(Player player)
    {
        lock (_lock) {
            if (CurrentState is not State.WaitingForPlayers) throw new RoomNotFoundException();
            if (_players.Count == 0 && player != _host) throw new RoomNotStartedException();
            if (_players.Count >= _gameConfig.MaxPlayerCount) throw new RoomIsFullException();
            if (_pendingPlayers.Count > 25) throw new RoomIsFullException();
            
            _pendingPlayers.Add(player);
        }
    }

    public void RegisterPlayerConnected(Guid playerId)
    {
        lock (_lock) {
            // If still waiting for players
            if (CurrentState is State.WaitingForPlayers) {
                var pendingPlayer = _pendingPlayers.FirstOrDefault(p => p.Id == playerId) 
                                    ?? throw new PlayerNotFoundException();
                _players.Add(pendingPlayer);
                _pendingPlayers.Remove(pendingPlayer);
            }
            
            var player = _players.FirstOrDefault(p => p.Id == playerId) 
                         ?? throw new PlayerNotFoundException();
            player.IsConnected = true;
            ViewNum++;
            OnRoomStateChanged();
        }
    }

    public void RegisterPlayerDisconnected(Guid playerId)
    {
        lock (_lock) {
            if (_players.All(p => p.Id != playerId)) throw new PlayerNotFoundException();
        
            var player = _players.First(p => p.Id == playerId);
            player.IsConnected = false;

            if (CurrentState is State.WaitingForPlayers) {
                _players.Remove(player);

                if (player == _host) {
                    _players.Clear();
                }
            }
        
            ViewNum++;
            OnRoomStateChanged();
        }
    }

    public void StartGame(Guid playerId)
    {
        lock (_lock) {
            if (CurrentState is not State.WaitingForPlayers) throw new RoomCannotStartException();
            if (_host.Id != playerId) throw new PlayerNotHostException();
            _game = _gameConfig.CreateGameInstance(_players.ToImmutableList());
            CurrentState = State.GameInProgress;
            LastIdleAt = DateTime.Now;
            ViewNum++;
            OnRoomStateChanged();
        }
    } 

    public IGameActionResponse? ExecuteGameAction(IGameActionArgs args, Guid? playerId = null)
    {
        lock (_lock) {
            if (CurrentState is State.WaitingForPlayers) throw new RoomNotStartedException();
        
            var player = playerId is not null ? _players.FirstOrDefault(p => p.Id == playerId) : null;
            var result = _game!.ExecuteAction(args, player);
            if (_game!.HasEnded()) CurrentState = State.GameEnded;
            LastIdleAt = DateTime.Now;
            ViewNum++;
            OnRoomStateChanged();
            return result;
        }
    }

    public RoomSnapshot GetSnapshot()
    {
        lock (_lock) {
            var playerInfo = _players.Select(p => (p.Username, p.IsConnected));
            return new RoomSnapshot(ViewNum, CurrentState, playerInfo, _game?.GetSnapshot());
        }
    }

    public void RemoveExpiredPlayers()
    {
        lock (_lock) {
            if (CurrentState is not State.WaitingForPlayers) {
                _pendingPlayers.Clear();
            }
            
            var expiredPlayers = _pendingPlayers.Where(p => DateTime.Now - p.CreatedAt > TimeSpan.FromSeconds(10));
            foreach (var expiredPlayer in expiredPlayers) {
                _pendingPlayers.Remove(expiredPlayer);
            }
        }   
    }
    
    public enum State
    {
        WaitingForPlayers,
        GameInProgress,
        GameEnded
    }
}