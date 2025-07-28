namespace BoredGames.Core.Room;

public abstract class RoomException(string message): Exception(message);
public sealed class RoomIsFullException() : RoomException("Room is already full");
public sealed class RoomNotFoundException() : RoomException("Room was not found");
public sealed class RoomCannotStartException() : RoomException("Invalid start conditions");
public sealed class RoomAlreadyStartedException() : RoomException("Room already started");
public sealed class RoomNotStartedException() : RoomException("Room not started");
public sealed class PlayerNotHostException() : RoomException("Player is not host");
public sealed class PlayerNotFoundException() : RoomException("Player Not Found");
public sealed class CreateRoomFailedException() : RoomException("Room failed to create");
public sealed class PlayerAlreadyConnectedException() : RoomException("Player already connected");
