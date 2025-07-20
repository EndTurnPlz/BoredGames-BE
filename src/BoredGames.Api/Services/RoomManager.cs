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
    private readonly CancellationTokenSource _tickerCts = new();
    private readonly FrozenDictionary<GameTypes, IGameConfig> _configs;
    
    public RoomManager(IEnumerable<IGameConfig> gameConfigs)
    {
        _configs = gameConfigs.ToFrozenDictionary(c => c.GameType, c => c);
        Task.Run(StartRoomCleanupTask);
    }

    private async Task? StartRoomCleanupTask()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try {
            while (await timer.WaitForNextTickAsync(_tickerCts.Token)) {
                try {
                    CleanupDeadRooms();
                }
                catch (Exception ex) {
                    // Add some actual logging here at some point
                    Console.WriteLine($"An error occurred during the background tick: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException) { } // This is expected on a graceful shutdown.
    }

    private void CleanupDeadRooms()
    {
        var deadRoomIds = _rooms.Where(pair => pair.Value.IsDead()).Select(pair => pair.Key);

        foreach (var id in deadRoomIds)
        {
            _rooms.TryRemove(id, out _);
        }
    }

    public void Dispose()
    {
        _tickerCts.Cancel();
        _tickerCts.Dispose();
    }

    public Guid CreateRoom(GameTypes gameType, Player host)
    {
        var lobby = new GameRoom(host, _configs[gameType]);
        var ok = _rooms.TryAdd(lobby.Id, lobby);
        if (!ok) throw new CreateRoomFailedException();
        return lobby.Id;
    }
    
    public void JoinRoom(Guid roomId, Player player)
    {
        GetRoom(roomId).AddPlayer(player);
    }
    
    [Pure]
    public GameRoom GetRoom(Guid lobbyId)
    {
        return _rooms.GetValueOrDefault(lobbyId) ?? throw new RoomNotFoundException();
    }

    // Temporary function for now ... will delete this when the full snapshot is passed via SSE.
    public int GetRoomViewNum(Guid lobbyId)
    {
        return GetRoom(lobbyId).ViewNum;
    }
}