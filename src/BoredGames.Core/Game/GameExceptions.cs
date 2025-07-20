namespace BoredGames.Core.Game;

public abstract class GameException(string message) : Exception(message);

public sealed class InvalidPlayerException() : GameException("Invalid player");
public sealed class InvalidMoveException() : GameException("Invalid move");
public sealed class InvalidActionException() : GameException("Invalid action");