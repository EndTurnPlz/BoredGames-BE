namespace BoredGames.Common.Game;

public abstract class GameException(string message) : Exception(message);

public class JoinGameException(string message) : GameException(message);
