using System.Collections.Frozen;

namespace BoredGames.Games.UpsAndDowns.Board;

public class GameBoard
{
    private readonly List<int> _playerPositions;
    public IReadOnlyList<int> PlayerPositions => _playerPositions;

    public readonly FrozenDictionary<int, int> WarpTiles;
    public bool IsPlayerOnEnd => _playerPositions.Contains(100);

    private GameBoard(int playerCount, Dictionary<int, int> warpTiles)
    {
        WarpTiles = warpTiles.ToFrozenDictionary();
        _playerPositions = Enumerable.Repeat(0, playerCount).ToList();
    }

    public void MovePlayer(int playerIndex, int moveDistance)
    {
        var currentPosition = _playerPositions[playerIndex];
        var newPosition = currentPosition + moveDistance;

        if (newPosition > 100) {
            newPosition = currentPosition;
        }

        newPosition = WarpTiles.GetValueOrDefault(newPosition, newPosition);
        _playerPositions[playerIndex] = newPosition;
    }
    
    private static readonly Dictionary<int, int> DefaultWarpTiles = new()
    {
        {1, 38},
        {4, 14},
        {8, 30},
        {21, 42},
        {28, 76},
        {32, 10},
        {36, 6},
        {48, 26},
        {50, 67},
        {62, 18},
        {71, 92},
        {80, 98},
        {88, 24},
        {95, 56},
        {97, 78}
    };

    public static GameBoard Create(int playerCount, Dictionary<int, int> warpTiles)
    {
        return new GameBoard(playerCount, warpTiles);
    }

    public static GameBoard CreateWithDefaultWarpTiles(int playerCount)
    {
        return Create(playerCount, DefaultWarpTiles);
    }

    // public static GameBoard CreateAndGenerateWarpTiles(int playerCount)
    // {
    //     
    // }
}