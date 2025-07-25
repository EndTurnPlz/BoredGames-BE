using System.Reflection;
using BoredGames.Games.Apologies.Board;
using BoredGames.Games.Apologies.Deck;
using BoredGames.Games.Apologies.Models;
using JetBrains.Annotations;

namespace BoredGames.UnitTests.Apologies.Board;

[TestSubject(typeof(GameBoard))]
public class GameBoardTest
{
    private readonly GameBoard _gameBoard = new();

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        Assert.NotNull(_gameBoard.PawnTiles);
        Assert.Equal(4, _gameBoard.PawnTiles.Length);
        foreach (var tileSet in _gameBoard.PawnTiles) {
            Assert.Equal(4, tileSet.Length);
            Assert.All(tileSet, Assert.NotNull);
        }
    }

    [Theory]
    [InlineData(0, CardDeck.CardTypes.One)]
    [InlineData(1, CardDeck.CardTypes.Two)]
    [InlineData(2, CardDeck.CardTypes.Seven)]
    [InlineData(3, CardDeck.CardTypes.Eleven)]
    public void GetValidMovesForPlayer_ValidScenario_ShouldReturnMoves(int playerIndex, CardDeck.CardTypes card)
    {
        var moves = _gameBoard.GetValidMovesForPlayer(playerIndex, card);
        Assert.NotNull(moves);
        Assert.IsType<List<GenericComponents.Moveset>>(moves);
    }

    [Theory]
    [InlineData(-1, CardDeck.CardTypes.One)]
    [InlineData(4, CardDeck.CardTypes.Seven)]
    public void GetValidMovesForPlayer_InvalidPlayerIndex_ShouldThrowIndexOutOfRangeException(
        int playerIndex, CardDeck.CardTypes card)
    {
        Assert.Throws<IndexOutOfRangeException>(() => _gameBoard.GetValidMovesForPlayer(playerIndex, card));
    }

    [Fact]
    public void TryExecuteSplitMove_ValidSplitMove_ShouldReturnTrue()
    {
        
        _gameBoard.PawnTiles[0][0] = BoardTileDfs("a_1")!;
        _gameBoard.PawnTiles[0][1] = BoardTileDfs("a_3")!;
        
        var split2Effect = GetMoveEffect("Split2");
        var split5Effect = GetMoveEffect("Split5");
        var firstMove = new GenericComponents.Move("a_1", "a_s1", split2Effect);
        var secondMove = new GenericComponents.Move("a_3", "a_6", split5Effect);
        
        
        var result = _gameBoard.TryExecuteSplitMove(firstMove, secondMove, 0);
        Assert.True(result);
    }

    [Fact]
    public void TryExecuteSplitMove_InvalidSplitMove_ShouldReturnFalse()
    {
        var split2Effect = GetMoveEffect("Split2");
        var split1Effect = GetMoveEffect("Split1");
        
        _gameBoard.PawnTiles[0][0] = BoardTileDfs("a_1")!;
        _gameBoard.PawnTiles[0][1] = BoardTileDfs("a_3")!;
        
        var firstMove = new GenericComponents.Move("a_1", "a_3", split2Effect);
        var secondMove = new GenericComponents.Move("a_3", "a_4", split1Effect);
        
        var result = _gameBoard.TryExecuteSplitMove(firstMove, secondMove, 0);
        Assert.False(result);
    }

    [Fact]
    public void TryExecuteMovePawn_ValidMove_ShouldReturnTrue()
    {
        var moveEffect = GetMoveEffect("Forward");
        var move = new GenericComponents.Move("a_4", "a_6", moveEffect);
        
        _gameBoard.PawnTiles[0][0] = BoardTileDfs("a_4")!;

        var result = _gameBoard.TryExecuteMovePawn(move, CardDeck.CardTypes.Two, 0);
        Assert.True(result);
    }

    [Fact]
    public void TryExecuteMovePawn_InvalidMove_ShouldReturnFalse()
    {
        var moveEffect = GetMoveEffect("ExitStart");
        var move = new GenericComponents.Move("a_4", "a_6", moveEffect);
        _gameBoard.PawnTiles[0][0] = BoardTileDfs("a_6")!;

        var result = _gameBoard.TryExecuteMovePawn(move, CardDeck.CardTypes.Two, 0);
        Assert.False(result);
    }

    [Fact]
    public void ExecuteAnyAvailableSlides_ValidScenario_ShouldReassignPawns()
    {
        _gameBoard.ExecuteAnyAvailableSlides();

        foreach (var tileSet in _gameBoard.PawnTiles) {
            Assert.All(tileSet, Assert.NotNull);
        }
    }
    
    
    [Fact]
    public void ValidateAndFindDestinationTile_ValidMove_ShouldReturnDestinationTile()
    {
        // Arrange
        const string sourceTileName = "a_S";
        const string destTileName = "a_4";
        const CardDeck.CardTypes cardType = CardDeck.CardTypes.One;
        const int playerIndex = 0;
        
        var sourceTile = BoardTileDfs(sourceTileName)!;

        var effect = GetMoveEffect("ExitStart");
        var move = new GenericComponents.Move(sourceTileName, destTileName, effect);

        // Act
        var result = GetPrivateMethodResult<BoardTile>(
            "ValidateAndFindDestinationTile", sourceTile, move, cardType, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(destTileName, result.Name);
    }


    [Fact]
    public void ProcessDestinationTile_ValidScenario_ShouldHandleCorrectly()
    {
        var sourceTile = new StartTile("a_1", null!);
        var destTile = new StartTile("a_2", null!);

        InvokePrivateMethod("ProcessDestinationTile", sourceTile, destTile, false);
    }

    [Fact]
    public void BuildGameBoard_ShouldBuildCorrectly()
    {
        InvokePrivateMethod("BuildGameBoard");

        Assert.NotNull(_gameBoard.PawnTiles);
        Assert.Equal(4, _gameBoard.PawnTiles.Length);
    }

    private T GetPrivateMethodResult<T>(string methodName, params object[] parameters)
    {
        var method = typeof(GameBoard).GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) throw new InvalidOperationException($"Method {methodName} not found");
        return (T)method.Invoke(_gameBoard, parameters)!;
    }

    private void InvokePrivateMethod(string methodName, params object[] parameters)
    {
        var method = typeof(GameBoard).GetMethod(methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null) throw new InvalidOperationException($"Method {methodName} not found");
        method.Invoke(_gameBoard, parameters);
    }

    private static int GetMoveEffect(string value)
    {
        var moveEffect = typeof(GameBoard).GetNestedType("MoveEffect", BindingFlags.NonPublic);
        if (moveEffect is null) Assert.Fail();
        return (int)Enum.Parse(moveEffect, value);
    }

    private BoardTile? BoardTileDfs(string targetTileName)
    {
        HashSet<string> visited = [];
        Queue<BoardTile> queue = [];
        foreach (var pawnSet in _gameBoard.PawnTiles) {
            foreach (var pawnTile in pawnSet) {
                visited.Add(pawnTile.Name);
                queue.Enqueue(pawnTile);
            }
        }

        while (queue.Count > 0) {
            var queueTile = queue.Dequeue();
            
            if (queueTile.Name == targetTileName) return queueTile;

            switch (queueTile) {
                case WalkableTile walkableTile:
                {
                    for ( var i = 0; i < 4; i++ ) {
                        if (visited.Contains(walkableTile.EvaluateNextTile(i).Name)) continue;
                        visited.Add(walkableTile.EvaluateNextTile(i).Name);
                        queue.Enqueue(walkableTile.EvaluateNextTile(i));
                    }
                    break;
                }
                case StartTile startTile:
                {
                    visited.Add(startTile.NextTile.Name);
                    queue.Enqueue(startTile.NextTile);
                    break;
                }
            }
        }

        return null;
    }
}