using System.Linq.Expressions;
using System.Reflection;

namespace BoredGames.Core.Game;

public class GameAction
{
    private readonly Func<GameBase, Player, IGameActionArgs?, IGameActionResponse?> _action;
    public readonly Type? ArgsType;
    
    // --- Private Constructor ---
    private GameAction(Func<GameBase, Player, IGameActionArgs?, IGameActionResponse?> action, Type? argsType)
    {
        _action = action;
        ArgsType = argsType;
    }
    
    // --- Public factory method ---
    public static GameAction Create(MethodInfo method, GameBase gameInstance)
    {
        var gameType = gameInstance.GetType();
        var parameters = method.GetParameters();
        if (parameters.Length > 2) throw new ArgumentException("Game actions must have at most two parameters.");

        // Discover signature
        var argsParamInfo = parameters.SingleOrDefault(p => typeof(IGameActionArgs).IsAssignableFrom(p.ParameterType));
        var argsType = argsParamInfo?.ParameterType;
        if (parameters[0] is null)
            throw new ArgumentException("A game action requires that a player object be passed as the first parameter. ");

        // Define expression parameters for the final delegate
        var gameInstanceParam = Expression.Parameter(typeof(GameBase), "game");
        var playerParam = Expression.Parameter(typeof(Player), "player");
        var argsParam = Expression.Parameter(typeof(IGameActionArgs), "args");

        // Create expressions to cast the generic parameters to the specific types needed by the method
        var castGameInstance = Expression.Convert(gameInstanceParam, gameType);
        
        // Build the list of arguments for the final method call
        List<Expression> methodCallArgs = [playerParam];
        if (argsType != null) {
            var castArgs = Expression.Convert(argsParam, argsType);
            methodCallArgs.Add(castArgs);
        }

        var methodCall = Expression.Call(castGameInstance, method, methodCallArgs);

        // Ensure the return type is correctly handled (void vs. returning a response)
        Expression finalBody = method.ReturnType == typeof(void)
            ? Expression.Block(methodCall, Expression.Constant(null, typeof(IGameActionResponse)))
            : Expression.Convert(methodCall, typeof(IGameActionResponse));

        var lambda = Expression.Lambda<Func<GameBase, Player, IGameActionArgs?, IGameActionResponse?>>(
            finalBody,
            gameInstanceParam,
            playerParam,
            argsParam
        );

        // Compile the tree into a highly optimized delegate.
        var compiledDelegate = lambda.Compile();

        return new GameAction(compiledDelegate, argsType);
    }
        
    public IGameActionResponse? Execute(GameBase gameInstance, Player player, IGameActionArgs? args = null)
    {
        if (!ArgsType?.IsInstanceOfType(args) ?? args is not null)
            throw new ArgumentException($"Invalid argument type provided. " + 
                                        $"Expected: {ArgsType?.Name ?? "null"}, " +
                                        $"Actual: {args?.GetType().Name ?? "null"}.");
        
        return _action(gameInstance, player, args);
    }
}