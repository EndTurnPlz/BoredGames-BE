using BoredGames.Common.Game;
using BoredGames.Common.Room.Models;

namespace BoredGames.Common.Room;

public class GameRoom
{
    public int ViewNum { get; private set; }
    public Guid Id { get; } = Guid.NewGuid();
    private readonly Player _host;
    private readonly List<Player> _players = [];
    private State _state = State.WaitingForPlayers;
    
    private readonly AbstractGameConfig _gameConfig;
    public AbstractGame? Game;
    
    public GameRoom(Player host, AbstractGameConfig gameConfig)
    {
        _gameConfig = gameConfig;
        _host = host;
        Join(host);
    }

    public void Join(Player player)
    {
        if (_state is not State.WaitingForPlayers) throw new RoomNotFoundException();
        if (_players.Count >= _gameConfig.MaxPlayerCount) throw new RoomIsFullException();

        ViewNum++;
        _players.Add(player);
    }

    public void Leave(Player player)
    {
        if (!_players.Contains(player)) throw new PlayerNotFoundException();
        
        ViewNum++;
        
        if (player == _host) {
            _state = State.Dead;
            _players.Clear();
            return;
        }
        
        _players.Remove(player);
    }

    public void SetConnectionStatus(Player player, bool isConnected)
    {
        
    }

    public void StartGame(Guid playerId)
    {
        if (_state is not State.WaitingForPlayers) throw new RoomCannotStartException();
        if (_host.ValidateId(playerId)) throw new PlayerNotHostException();
        if (_players.Count < _gameConfig.MinPlayerCount) throw new RoomCannotStartException();
        
        _state = State.GameStarted;
        
        // Add Game start logic here
    }

    public RoomSnapshot GetSnapshot()
    {
        return new RoomSnapshot(_state, _players.Select(p => p.Username), Game?.GetSnapshot());
    }
    
    public enum State
    {
        WaitingForPlayers,
        GameStarted,
        GameEnded,
        Dead
    }
}