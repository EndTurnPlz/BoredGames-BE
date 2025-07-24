using JetBrains.Annotations;

namespace BoredGames.Core.Game;

public class GameAction
{
    private readonly Func<object, Player?, IGameActionResponse?> _action;
    public readonly Type ArgType;
    private readonly bool _requiresPlayer;
    
    // --- Private Constructor ---
    private GameAction(Func<object, Player?, IGameActionResponse?> action, Type argType, bool requiresPlayer)
    {
        _action = action;
        ArgType = argType;
        _requiresPlayer = requiresPlayer;
    }

    // --- Public Static Factory Methods ---
    
    // Factory for: No player, return value
    [UsedImplicitly]
    public static GameAction Create<TArgs>(Func<TArgs, IGameActionResponse?> action) 
        where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), false);

        IGameActionResponse? WrappedAction(object args, Player? _)
        {
            return action((TArgs)args);
        }
    }
    
    // Factory for: Player, no return value
    [UsedImplicitly]
    public static GameAction Create<TArgs>(Action<TArgs, Player> action) 
        where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), true);

        IGameActionResponse? WrappedAction(object args, Player? p)
        {
            action((TArgs)args, p!); return null;
        }
    }

    // Factory for: Player and typed arguments, return value
    [UsedImplicitly]
    public static GameAction Create<TArgs>(Func<TArgs, Player, IGameActionResponse?> action) 
        where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), true);

        IGameActionResponse? WrappedAction(object args, Player? p)
        {
            return action((TArgs)args, p!);
        }
    }
        
    public IGameActionResponse? Execute(IGameActionArgs args, Player? player)
    {
        // Corrected and improved validation logic
        if (_requiresPlayer && player is null) 
            throw new ArgumentException("A Player is required for this action");
        if (!ArgType.IsInstanceOfType(args))
            throw new ArgumentException("Invalid argument type or null args provided when args were expected.");
        
        return _action.Invoke(args, player);
    }
}