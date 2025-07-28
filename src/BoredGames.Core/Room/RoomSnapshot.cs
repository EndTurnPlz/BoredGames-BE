using BoredGames.Core.Game;
using JetBrains.Annotations;

namespace BoredGames.Core.Room;

[UsedImplicitly]
public record RoomSnapshot(
    int ViewNum, 
    GameRoom.State State, 
    IEnumerable<string> PlayerNames,
    IEnumerable<bool> PlayerConnStatus,
    IGameSnapshot? GameSnapshot);