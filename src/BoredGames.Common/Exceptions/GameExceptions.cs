namespace BoredGames.Common.Exceptions;


public abstract class GameException(string message) : Exception(message);
public class CreateGameException(string message) : GameException(message);
public class JoinGameException(string message) : GameException(message);
public class StartGameException(string message) : GameException(message);
