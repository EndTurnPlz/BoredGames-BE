using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.Contracts;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Room;

namespace BoredGames.Services;

public sealed class RoomManager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, GameRoom> _rooms = new();
    private readonly FrozenDictionary<string, IGameConfig> _configs;
    private readonly CancellationTokenSource _tickerCts = new();
    
    private readonly TimeSpan _abandonedRoomTimeout = TimeSpan.FromMinutes(5);
    private readonly PlayerConnectionManager _playerConnectionManager;
    private readonly ILogger<RoomManager> _logger;
    
    public RoomManager(IEnumerable<IGameConfig> gameConfigs, PlayerConnectionManager playerConnectionManager,
        ILogger<RoomManager> logger) 
    {
        _configs = gameConfigs.ToFrozenDictionary(c => c.GetGameName(), c => c);
        _playerConnectionManager = playerConnectionManager;
        _logger = logger;
    }
    
    private async void OnRoomChanged(object? sender, EventArgs e)
    {
        try {
            if (sender is not GameRoom) throw new ArgumentNullException(nameof(sender));
            if (e is not RoomChangedEventArgs args) throw new InvalidDataException("Invalid event args type");
        
            await _playerConnectionManager.PushSnapshotToPlayersAsync(args.PlayerIds, args.Snapshot); 
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not push room snapshot to players"); 
        }
    }
    
    public void Dispose()
    {
        _tickerCts.Cancel();
        _tickerCts.Dispose();
    }

    public void CleanupStaleResources()
    {
        CleanupDeadRooms(_abandonedRoomTimeout);
        CleanupExpiredPlayers();
    }

    private void CleanupDeadRooms(TimeSpan abandonedRoomTimeout)
    {
        var deadRoomIds = _rooms.Where(pair => pair.Value.IsDead(abandonedRoomTimeout)).Select(pair => pair.Key);

        foreach (var id in deadRoomIds)
        {
            if (_rooms.TryRemove(id, out var room)) {
                room.RoomChanged -= OnRoomChanged;
            }
        }
    }
    
    private void CleanupExpiredPlayers()
    {
        foreach (var (_, room) in _rooms) 
        {
            if (room.MayHaveExpiredPlayers()) {
                room.RemoveExpiredPlayers();
            }
        }
    }

    public Guid CreateRoom(string gameType, Player host)
    {
        if (!_configs.TryGetValue(gameType, out var config)) throw new CreateRoomFailedException();
        
        var room = new GameRoom(host, config);
        if (!_rooms.TryAdd(room.Id, room)) throw new CreateRoomFailedException();
        
        room.RoomChanged += OnRoomChanged;
        return room.Id;
    }
    
    public void JoinRoom(Guid roomId, Player player)
    {
        GetRoom(roomId).AddPendingPlayer(player);
    }
    
    [Pure]
    public GameRoom GetRoom(Guid roomId)
    {
        return _rooms.GetValueOrDefault(roomId) ?? throw new RoomNotFoundException();
    }
}