using System.Diagnostics.Contracts;
using BoredGames.Apologies;
using BoredGames.Common;
using BoredGames.Common.Game;
using BoredGames.Common.Room;

namespace BoredGames;

public static class RoomManager
{
    private static readonly Dictionary<Guid, GameRoom> Rooms = new();

    public static Guid CreateRoom(GameTypes gameType, Player host)
    {
        var lobby = gameType switch
        {
            GameTypes.Apologies => new GameRoom(host, new ApologiesGameConfig()),
            _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
        };
        
        Rooms.Add(lobby.Id, lobby);
        return lobby.Id;
    }
    
    public static void JoinRoom(Guid roomId, Player player)
    {
        GetRoom(roomId).Join(player);
    }

    public static int GetRoomViewNum(Guid lobbyId)
    {
        return GetRoom(lobbyId).ViewNum;
    }

    public static void StartGame(Guid lobbyId, Guid playerId)
    {
        GetRoom(lobbyId).StartGame(playerId);
    }

    [Pure]
    public static GameRoom GetRoom(Guid lobbyId)
    {
        return Rooms.GetValueOrDefault(lobbyId) ?? throw new RoomNotFoundException();
    }
}