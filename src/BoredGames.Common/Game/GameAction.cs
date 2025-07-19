namespace BoredGames.Common.Game;

public class GameAction
{
    // --- Private Constructor ---
    private GameAction(Func<Player?, IGameActionArgs?, IGameActionResponse?> action, Type argType, bool requiresPlayer)
    {
        _action = action;
        _argType = argType;
        _requiresPlayer = requiresPlayer;
    }

    // --- Public Static Factory Methods ---
    
    // Factory for: Player-only, no return
    public static GameAction Create(Action<Player> action)
    {
        return new GameAction(WrappedAction, typeof(void), true);
        IGameActionResponse? WrappedAction(Player? p, IGameActionArgs? _) { action(p!); return null; }
    }
    
    // Factory for: No player, return value
    public static GameAction Create(Func<IGameActionResponse?> action)
    {
        return new GameAction(WrappedAction, typeof(void), false);
        IGameActionResponse? WrappedAction(Player? player, IGameActionArgs? gameActionArgs) => action();
    }
    
    // Factory for: Player-only, return value
    public static GameAction Create(Func<Player, IGameActionResponse?> action)
    {
        return new GameAction(WrappedAction, typeof(void), true);
        IGameActionResponse? WrappedAction(Player? p, IGameActionArgs? _) => action(p!);
    }
    
    // Factory for: Player and typed arguments
    public static GameAction Create<TArgs>(Action<Player, TArgs> action) where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), true);
        IGameActionResponse? WrappedAction(Player? p, IGameActionArgs? args) { action(p!, (TArgs)args!); return null; }
    }

    // Factory for: Player and typed arguments, return value
    public static GameAction Create<TArgs>(Func<Player, TArgs, IGameActionResponse?> action) where TArgs : class, IGameActionArgs
    {
        return new GameAction(WrappedAction, typeof(TArgs), true);
        IGameActionResponse? WrappedAction(Player? p, IGameActionArgs? args) => action(p!, (TArgs)args!);
    }

    private readonly Func<Player?, IGameActionArgs?, IGameActionResponse?> _action;
    private readonly Type _argType;
    private readonly bool _requiresPlayer;
        
    public IGameActionResponse? Execute(Player? player, IGameActionArgs? args)
    {
        // Corrected and improved validation logic
        if (_requiresPlayer && player is null) 
            throw new ArgumentException("Player is required for this action.");
        if (_argType == typeof(void) && args is not null) 
            throw new ArgumentException("Args provided when no args were expected.");
        if (_argType != typeof(void) && (args is null || !_argType.IsInstanceOfType(args)))
            throw new ArgumentException("Invalid argument type or null args provided when args were expected.");
        
        return _action(player, args);
    }
}