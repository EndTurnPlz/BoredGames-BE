using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using BoredGames.Apologies;
using BoredGames.Common;
using BoredGames.Common.Game;
using BoredGames.Common.Room;

namespace BoredGames;

public static class RoomManager
{
    private static readonly ConcurrentDictionary<Guid, GameRoom> Rooms = new();
    private static readonly CancellationTokenSource TickerCts = new();
    
    static RoomManager()
    {
        Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                while (await timer.WaitForNextTickAsync(TickerCts.Token))
                {
                    try {
                        CleanupDeadRooms();
                    } catch (Exception ex) {
                        // Add some actual logging here at some point
                        Console.WriteLine($"An error occurred during the background tick: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { } // This is expected on a graceful shutdown.
        });
    }
    
    private static void CleanupDeadRooms()
    {
        var deadRoomIds = Rooms.Where(pair => pair.Value.IsDead()).Select(pair => pair.Key);

        foreach (var id in deadRoomIds)
        {
            Rooms.TryRemove(id, out _);
        }
    }
    
    public static void StopService()
    {
        TickerCts.Cancel();
        TickerCts.Dispose();
    }

    public static Guid CreateRoom(GameTypes gameType, Player host)
    {
        var lobby = gameType switch
        {
            GameTypes.Apologies => new GameRoom(host, new ApologiesGameConfig()),
            _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
        };
        
        var ok = Rooms.TryAdd(lobby.Id, lobby);
        if (!ok) throw new CreateRoomFailedException();

        return lobby.Id;
    }
    
    public static void JoinRoom(Guid roomId, Player player)
    {
        GetRoom(roomId).Join(player);
    }
    
    [Pure]
    public static GameRoom GetRoom(Guid lobbyId)
    {
        return Rooms.GetValueOrDefault(lobbyId) ?? throw new RoomNotFoundException();
    }

    // Temporary function for now ... will delete this when the full snapshot is passed via SSE.
    public static int GetRoomViewNum(Guid lobbyId)
    {
        return GetRoom(lobbyId).ViewNum + (GetRoom(lobbyId).Game?.ViewNum ?? 0);
    }
}