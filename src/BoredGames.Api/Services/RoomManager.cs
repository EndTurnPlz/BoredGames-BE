using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Room;

namespace BoredGames.Services;

public sealed class RoomManager(PlayerConnectionManager playerConnectionManager, 
    ILogger<RoomManager> logger) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, GameRoom> _rooms = new();
    private readonly CancellationTokenSource _tickerCts = new();
    
    private readonly TimeSpan _abandonedRoomTimeout = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _idleGameTimeout = TimeSpan.FromMinutes(15);

    private async void OnRoomChanged(object? sender, EventArgs e)
    {
        try {
            if (sender is not GameRoom) throw new ArgumentNullException(nameof(sender));
            if (e is not RoomChangedEventArgs args) throw new InvalidDataException("Invalid event args type");
        
            await playerConnectionManager.PushSnapshotsToPlayersAsync(args.PlayerIds.ToList(), args.Snapshot.ToList()); 
        }
        catch (Exception ex) {
            logger.LogError(ex, "Could not push room snapshot to players"); 
        }
    }
    
    public void Dispose()
    {
        _tickerCts.Cancel();
        _tickerCts.Dispose();
    }

    public void CleanupStaleResources()
    {
        CleanupDeadRooms();
        CleanupExpiredPlayers();
    }

    private void CleanupDeadRooms()
    {
        var deadRoomIds = _rooms
            .Where(pair => pair.Value.IsDead(_abandonedRoomTimeout, _idleGameTimeout))
            .Select(pair => pair.Key);

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

    public Guid CreateRoom(GameRegistry.GameInfoEntry gameInfo, Player host)
    {
        var config = (IGameConfig)Activator.CreateInstance(gameInfo.ConfigType)!;
        var room = new GameRoom(config, gameInfo.Constructor, gameInfo.MinPlayers, gameInfo.MaxPlayers, host);
        if (!_rooms.TryAdd(room.Id, room)) throw new CreateRoomFailedException();
        
        room.RoomChanged += OnRoomChanged;
        return room.Id;
    }
    
    [Pure]
    public GameRoom GetRoom(Guid roomId)
    {
        return _rooms.GetValueOrDefault(roomId) ?? throw new RoomNotFoundException();
    }
}