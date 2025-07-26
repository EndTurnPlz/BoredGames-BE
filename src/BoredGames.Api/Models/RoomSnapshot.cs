using BoredGames.Core.Game;
using BoredGames.Core.Room;
using JetBrains.Annotations;

namespace BoredGames.Models;

[UsedImplicitly]
public record RoomSnapshot(
    int ViewNum, 
    GameRoom.State State, 
    IEnumerable<(string, bool)> Players, 
    IGameSnapshot? GameSnapshot);