namespace BoredGames.Common.Game;

public class GameAction
{
    // --- Private Constructor ---
    private GameAction(Func<object, Player?, IGameActionResponse?> action, Type argType, bool requiresPlayer)
    {
        _action = action;
        _argType = argType;
        _requiresPlayer = requiresPlayer;
    }

    // --- Public Static Factory Methods ---
    
    // Factory for: No player, return value
    public static GameAction Create<TArgs>(Func<TArgs, IGameActionResponse?> action) where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(void), false);
        IGameActionResponse? WrappedAction(object args, Player? _) { action((TArgs)args); return null; }
    }
    
    // Factory for: Player and typed arguments
    public static GameAction Create<TArgs>(Action<TArgs, Player> action) where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), true);
        IGameActionResponse? WrappedAction(object args, Player? p) { action((TArgs)args, p!); return null; }
    }

    // Factory for: Player and typed arguments, return value
    public static GameAction Create<TArgs>(Func<TArgs, Player, IGameActionResponse?> action) where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), true);
        IGameActionResponse? WrappedAction(object args, Player? p) => action((TArgs)args, p!);
    }

    private readonly Func<object, Player?, IGameActionResponse?> _action;
    private readonly Type _argType;
    private readonly bool _requiresPlayer;
        
    public IGameActionResponse? Execute(IGameActionArgs args, Player? player)
    {
        // Corrected and improved validation logic
        if (_requiresPlayer && player is null) 
            throw new ArgumentException("A Player is required for this action");
        if (!_argType.IsInstanceOfType(args))
            throw new ArgumentException("Invalid argument type or null args provided when args were expected.");
        
        return _action(args, player);
    }
}