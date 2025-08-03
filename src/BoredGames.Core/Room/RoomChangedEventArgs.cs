namespace BoredGames.Core.Room;

public class RoomChangedEventArgs(IEnumerable<Guid> playerIds, IEnumerable<RoomSnapshot> snapshot) : EventArgs
{
    public IEnumerable<Guid> PlayerIds { get; } = playerIds;
    public  IEnumerable<RoomSnapshot> Snapshot { get; } = snapshot;
}