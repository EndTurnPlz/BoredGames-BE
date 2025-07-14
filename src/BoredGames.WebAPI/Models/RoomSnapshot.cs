using BoredGames.Common.Game;
using BoredGames.Common.Room;
using JetBrains.Annotations;

namespace BoredGames.Models;

[UsedImplicitly]
public record RoomSnapshot(GameRoom.State State, IEnumerable<string> Players, IGameSnapshot? GameSnapshot);