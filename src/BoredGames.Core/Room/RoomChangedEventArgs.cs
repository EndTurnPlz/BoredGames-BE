namespace BoredGames.Core.Room;

public class RoomChangedEventArgs(IEnumerable<Guid> playerIds, RoomSnapshot snapshot) : EventArgs
{
    public IEnumerable<Guid> PlayerIds { get; } = playerIds;
    public RoomSnapshot Snapshot { get; } = snapshot;
}