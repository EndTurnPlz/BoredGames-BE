using BoredGames.Common.Room.Models;

namespace BoredGames.Common.Game;

public abstract class AbstractGame
{
    public int ViewNum { get; protected set; }
    public bool HasStarted => ViewNum > 0;

    public abstract IGameSnapshot GetSnapshot();
}