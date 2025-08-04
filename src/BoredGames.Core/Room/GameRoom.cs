using System.Collections.Immutable;
using System.Text.Json;
using BoredGames.Core.Game;
using GameConstructor = System.Func<BoredGames.Core.Game.IGameConfig, 
                                    System.Collections.Immutable.ImmutableList<BoredGames.Core.Player>, 
                                    BoredGames.Core.Game.GameBase>;

namespace BoredGames.Core.Room;

public class GameRoom
{
    // Room info
    private int ViewNum { get; set; }
    public Guid Id { get; } = Guid.NewGuid();
    private State RoomState { get; set; } = State.WaitingForPlayers;
    private DateTime LastIdleAt { get; set; } = DateTime.Now;
    private readonly int _minPlayerCount;
    private readonly int _maxPlayerCount;
    
    // Players
    private readonly Player _host;
    private readonly List<Player> _players = [];
    private readonly List<Player> _pendingPlayers = [];

    // Game
    private readonly IGameConfig _gameConfig;
    private GameBase? _game;
    private readonly GameConstructor _gameConstructor;
    
    // Concurrency
    private readonly Lock _lock = new();
    
    // Event
    public event EventHandler? RoomChanged;
    
    public enum State
    {
        WaitingForPlayers,
        GameInProgress,
        GameEnded
    }
    
    private void EmitRoomChangedEvent()
    {
        if (!_lock.IsHeldByCurrentThread) {
            throw new SynchronizationLockException("Current thread does not hold the room lock");
        }
        ViewNum++;
        var playerIds = _players.Select(p => p.Id);
        var snapshots = _players.Select(GetSnapshot);
        RoomChanged?.Invoke(this, new RoomChangedEventArgs(playerIds, snapshots));
    }
    
    public GameRoom(IGameConfig gameConfig, GameConstructor gameConstructor, int minPlayers, int maxPlayers, Player host)
    {
        _gameConfig = gameConfig;
        _gameConstructor = gameConstructor;
        _minPlayerCount = minPlayers;
        _maxPlayerCount = maxPlayers;
        _host = host;
        AddPendingPlayer(host);
    }
    
    public bool IsDead(TimeSpan abandonedRoomTimeout, TimeSpan idleGameTimeout)
    {
        lock (_lock) {
            if (_players.Count == 0 && !_pendingPlayers.Contains(_host)) return true;
            
            var timeout = RoomState is State.WaitingForPlayers ? abandonedRoomTimeout : idleGameTimeout;
            return DateTime.Now - LastIdleAt > timeout;
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
            if (RoomState is not State.WaitingForPlayers) throw new RoomNotFoundException();
            if (_players.Count == 0 && player != _host) throw new RoomNotStartedException();
            if (_players.Count >= _maxPlayerCount || _pendingPlayers.Count > 25) 
            {
                throw new RoomIsFullException();
            }
            
            if (_pendingPlayers.Exists(p => p.Username == player.Username) ||
                _players.Exists(p => p.Username == player.Username)) 
            {
                throw new NameAlreadyTakenException();
            }
            
            _pendingPlayers.Add(player);
        }
    }

    public void RegisterPlayerConnected(Guid playerId)
    {
        lock (_lock) {
            // If still waiting for players
            if (RoomState is State.WaitingForPlayers) {
                if (_players.Count >= _maxPlayerCount) throw new RoomIsFullException();
                var pendingPlayer = _pendingPlayers.SingleOrDefault(p => p.Id == playerId) 
                                    ?? throw new PlayerNotFoundException();
                _players.Add(pendingPlayer);
                _pendingPlayers.Remove(pendingPlayer);
            }
            
            var player = _players.SingleOrDefault(p => p.Id == playerId) 
                         ?? throw new PlayerNotFoundException();
            player.IsConnected = true;
            ViewNum++;
            EmitRoomChangedEvent();
        }
    }

    public void RegisterPlayerDisconnected(Guid playerId)
    {
        lock (_lock) {
            if (_players.All(p => p.Id != playerId)) throw new PlayerNotFoundException();
        
            var player = _players.Single(p => p.Id == playerId);
            player.IsConnected = false;

            if (RoomState is State.WaitingForPlayers) {
                _players.Remove(player);

                if (player == _host) {
                    _players.Clear();
                }
            }
        
            EmitRoomChangedEvent();
        }
    }

    public void StartGame(Guid playerId)
    {
        lock (_lock) {
            if (RoomState is not State.WaitingForPlayers) throw new RoomCannotStartException();
            if (_host.Id != playerId) throw new PlayerNotHostException();
            if (_players.Count < _minPlayerCount || _players.Count > _maxPlayerCount) throw new RoomCannotStartException();
            
            _game = _gameConstructor(_gameConfig, _players.ToImmutableList());
            RoomState = State.GameInProgress;
            LastIdleAt = DateTime.Now;
            EmitRoomChangedEvent();
        }
    } 

    public void ExecuteGameAction(string actionName, Guid playerId, JsonElement? args)
    {
        lock (_lock) {
            if (RoomState is State.WaitingForPlayers) throw new RoomNotStartedException();
            
            var player = _players.SingleOrDefault(p => p.Id == playerId) ?? throw new PlayerNotFoundException();
            if (!player.IsConnected) throw new PlayerNotConnectedException();
            _game!.ExecuteAction(actionName, player, args);
            if (_game!.HasEnded()) RoomState = State.GameEnded;
            LastIdleAt = DateTime.Now;
            EmitRoomChangedEvent();
        }
    }

    private RoomSnapshot GetSnapshot(Player player)
    {
        lock (_lock) {
            var playerNames = _players.Select(p => p.Username);
            var playerConnStatus = _players.Select(p => p.IsConnected);
            return new RoomSnapshot(ViewNum, RoomState, playerNames, playerConnStatus, _game?.GetSnapshot(player));
        }
    }

    public void RemoveExpiredPlayers()
    {
        lock (_lock) {
            if (RoomState is not State.WaitingForPlayers) {
                _pendingPlayers.Clear();
            }
            
            var expiredPlayers = _pendingPlayers.Where(p => DateTime.Now - p.CreatedAt > TimeSpan.FromSeconds(10));
            foreach (var expiredPlayer in expiredPlayers) {
                _pendingPlayers.Remove(expiredPlayer);
            }
        }   
    }
}