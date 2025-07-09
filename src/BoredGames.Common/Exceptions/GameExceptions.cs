namespace BoredGames.Common.Exceptions;

public class CreateGameException(string message) : Exception(message);
public class JoinGameException(string message) : Exception(message);
public class StartGameException(string message) : Exception(message);

public class GameNotOverException(string message) : Exception(message);