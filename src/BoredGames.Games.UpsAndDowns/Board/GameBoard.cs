namespace BoredGames.Games.UpsAndDowns.Board;

public class GameBoard(int playerCount)
{
    private readonly List<int> _playerPosition = [
        ..Enumerable.Range(0, playerCount)
    ];
    
    public IReadOnlyList<int> PlayerPositions => _playerPosition;
    
    public readonly Dictionary<int, int> WarpTiles = new()
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

    public bool IsPlayerOnEnd => _playerPosition.Contains(100);
    
    public void MovePlayer(int playerIndex, int moveDistance)
    {
        var currentPosition = _playerPosition[playerIndex];
        var newPosition = currentPosition + moveDistance;

        if (newPosition > 100) {
            newPosition = currentPosition;
        }

        newPosition = WarpTiles.GetValueOrDefault(newPosition, newPosition);
        _playerPosition[playerIndex] = newPosition;
    }
}