using System.Linq.Expressions;
using System.Reflection;

namespace BoredGames.Core.Game;

using GameActionDelegate = Action<GameBase, Player, IGameActionArgs?>;

public class GameAction
{
    private readonly GameActionDelegate _action;
    public readonly Type? ArgsType;
    
    // Private Constructor
    private GameAction(GameActionDelegate action, Type? argsType)
    {
        _action = action;
        ArgsType = argsType;
    }
    
    // Public factory method
    public static GameAction Create(MethodInfo method, GameBase gameInstance)
    {
        var gameType = gameInstance.GetType();
        var parameters = method.GetParameters();
        if (parameters.Length is > 2 or 0) throw new ArgumentException("Game actions must have 1-2 parameters.");

        // Discover signature
        if (parameters[0].ParameterType != typeof(Player)) {
            throw new ArgumentException("A game action requires a player object be passed as the first parameter. ");
        }
        var argsParamInfo = parameters.SingleOrDefault(p => typeof(IGameActionArgs).IsAssignableFrom(p.ParameterType));
        var argsType = argsParamInfo?.ParameterType;

        // Define expression parameters for the final delegate
        var gameInstanceParam = Expression.Parameter(typeof(GameBase), "game");
        var playerParam = Expression.Parameter(typeof(Player), "player");
        var argsParam = Expression.Parameter(typeof(IGameActionArgs), "args");

        
        // Build the list of arguments for the final method call
        var castGameInstance = Expression.Convert(gameInstanceParam, gameType);
        List<Expression> methodCallArgs = [playerParam];
        if (argsType != null) {
            var castArgs = Expression.Convert(argsParam, argsType);
            methodCallArgs.Add(castArgs);
        }

        var methodCall = Expression.Call(castGameInstance, method, methodCallArgs);

        // Compile the tree into a highly optimized delegate.
        var actionDelegate = Expression.Lambda<GameActionDelegate>(
            methodCall,
            gameInstanceParam,
            playerParam,
            argsParam
        ).Compile();
        
        return new GameAction(actionDelegate, argsType);
    }
        
    public void Execute(GameBase gameInstance, Player player, IGameActionArgs? args = null)
    {
        //Action expects no args, but args were given.
        if (ArgsType == null && args != null)
        {
            throw new ArgumentException("This action does not accept arguments.");
        }

        //Action expects args, but none were given.
        if (ArgsType != null && args == null)
        {
            throw new ArgumentException($"Action requires arguments of type '{ArgsType.Name}'.");
        }

        //Action expects args, but the wrong type was given.
        if (ArgsType != null && !ArgsType.IsInstanceOfType(args))
        {
            throw new ArgumentException($"Invalid argument type. Expected '{ArgsType.Name}' " +
                                        $"but received '{args!.GetType().Name}'.");
        }
        
        _action(gameInstance, player, args);
    }
}