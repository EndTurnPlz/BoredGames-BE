namespace BoredGames.Common.Room.Models;

public interface IGameSnapshot
{
    int ViewNum { get; }
}

public record RoomSnapshot(GameRoom.State State, IEnumerable<string> Players, IGameSnapshot? GameSnapshot);