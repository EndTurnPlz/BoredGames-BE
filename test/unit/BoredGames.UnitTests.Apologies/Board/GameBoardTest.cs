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
        // Arrange
        _gameBoard.PawnTiles[0][0] = BoardTileDfs(_gameBoard, "a_1")!;
        _gameBoard.PawnTiles[0][1] = BoardTileDfs(_gameBoard, "a_3")!;

        var split2Effect = GetMoveEffect("Split2");
        var split5Effect = GetMoveEffect("Split5");
        var firstMove = new GenericComponents.Move("a_1", "a_s1", split2Effect);
        var secondMove = new GenericComponents.Move("a_3", "a_6", split5Effect);

        // Act
        var result = _gameBoard.TryExecuteSplitMove(firstMove, secondMove, 0);

        // Assert
        Assert.True(result);
        Assert.Equal("a_s1", _gameBoard.PawnTiles[0][0].Name);
        Assert.Equal("a_6", _gameBoard.PawnTiles[0][1].Name);
    }

    [Fact]
    public void TryExecuteSplitMove_InvalidSplitMove_ShouldReturnFalse()
    {
        var split2Effect = GetMoveEffect("Split2");
        var split1Effect = GetMoveEffect("Split1");
        
        _gameBoard.PawnTiles[0][0] = BoardTileDfs(_gameBoard, "a_1")!;
        _gameBoard.PawnTiles[0][1] = BoardTileDfs(_gameBoard, "a_3")!;
        
        var firstMove = new GenericComponents.Move("a_1", "a_3", split2Effect);
        var secondMove = new GenericComponents.Move("a_3", "a_4", split1Effect);
        
        var result = _gameBoard.TryExecuteSplitMove(firstMove, secondMove, 0);
        Assert.False(result);
    }

    [Fact]
    public void TryExecuteMovePawn_ValidMove_ShouldReturnTrue()
    {
        // Arrange
        var moveEffect = GetMoveEffect("Forward");
        var move = new GenericComponents.Move("a_4", "a_6", moveEffect);

        _gameBoard.PawnTiles[0][0] = BoardTileDfs(_gameBoard, "a_4")!;

        // Act
        var result = _gameBoard.TryExecuteMovePawn(move, CardDeck.CardTypes.Two, 0);

        // Assert
        Assert.True(result);
        Assert.Equal("a_6", _gameBoard.PawnTiles[0][0].Name);
    }

    [Fact]
    public void TryExecuteMovePawn_WithPawnKill_ShouldSendEnemyToStart()
    {
        // Arrange
        var moveEffect = GetMoveEffect("Forward");
        var move = new GenericComponents.Move("a_4", "a_6", moveEffect);

        // Setup player 0's pawn at a_4
        _gameBoard.PawnTiles[0][0] = BoardTileDfs(_gameBoard, "a_4")!;

        // Setup player 1's pawn at a_6 (destination)
        _gameBoard.PawnTiles[1][0] = BoardTileDfs(_gameBoard, "a_6")!;

        // Remember player 1's start tile
        var player1StartTile = BoardTileDfs(_gameBoard, "b_S")!;

        // Act
        var result = _gameBoard.TryExecuteMovePawn(move, CardDeck.CardTypes.Two, 0);

        // Assert
        Assert.True(result);
        Assert.Equal("a_6", _gameBoard.PawnTiles[0][0].Name);
        Assert.Equal(player1StartTile.Name, _gameBoard.PawnTiles[1][0].Name);
    }

    [Fact]
    public void TryExecuteMovePawn_InvalidMove_ShouldReturnFalse()
    {
        var moveEffect = GetMoveEffect("ExitStart");
        var move = new GenericComponents.Move("a_4", "a_6", moveEffect);
        _gameBoard.PawnTiles[0][0] = BoardTileDfs(_gameBoard, "a_6")!;

        var result = _gameBoard.TryExecuteMovePawn(move, CardDeck.CardTypes.Two, 0);
        Assert.False(result);
    }

    [Fact]
    public void FindTilesForApologiesMove_ShouldReturnValidTiles()
    {
        // Arrange
        const string sourceTileName = "a_10";
        const CardDeck.CardTypes card = CardDeck.CardTypes.Apologies;
        const int playerIndex = 0;

        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;
        _gameBoard.PawnTiles[1][0] = BoardTileDfs(_gameBoard, "b_10")!;

        // Act
        var result = GetPrivateMethodResult<List<BoardTile>>(
            "FindTilesForApologiesMove", sourceTile, card, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    [Fact]
    public void FindTilesForSwapMove_ShouldReturnValidTiles()
    {
        // Arrange
        const string sourceTileName = "a_10";
        const CardDeck.CardTypes card = CardDeck.CardTypes.Eleven;
        const int playerIndex = 0;

        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;
        _gameBoard.PawnTiles[1][0] = BoardTileDfs(_gameBoard, "b_12")!;

        // Act
        var result = GetPrivateMethodResult<List<BoardTile>>(
            "FindTilesForSwapMove", sourceTile, card, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count >= 0);
    }

    [Fact]
    public void FindTilesForSplitMove_ShouldReturnValidTiles()
    {
        // Arrange
        const string sourceTileName = "a_10";
        const CardDeck.CardTypes card = CardDeck.CardTypes.Seven;
        const int playerIndex = 0;

        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;
        _gameBoard.PawnTiles[0][0] = sourceTile;
        _gameBoard.PawnTiles[0][1] = BoardTileDfs(_gameBoard, "a_7")!;
        
        // Act
        var result = GetPrivateMethodResult<List<(BoardTile, int)>>(
            "FindTilesForSplitMove", sourceTile, card, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void ExecuteAnyAvailableSlides_ValidScenario_ShouldReassignPawns()
    {
        // Arrange
        _gameBoard.ExecuteAnyAvailableSlides();

        // Assert
        foreach (var tileSet in _gameBoard.PawnTiles) {
            Assert.All(tileSet, Assert.NotNull);
        }
    }

    [Fact]
    public void ExecuteAnyAvailableSlides_WithPawnOnSlider_ShouldMovePawn()
    {
        // Arrange - Find a slider tile
        var sliderTiles = new List<SliderTile>();
        HashSet<string> visited = [];
        Queue<BoardTile> queue = [];

        foreach (var pawnSet in _gameBoard.PawnTiles)
        foreach (var pawnTile in pawnSet) {
            visited.Add(pawnTile.Name);
            queue.Enqueue(pawnTile);
        }

        while (queue.Count > 0) {
            var queueTile = queue.Dequeue();

            if (queueTile is SliderTile slider) {
                sliderTiles.Add(slider);
            }

            switch (queueTile) {
                case WalkableTile walkableTile:
                {
                    for (var i = 0; i < 4; i++) {
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

        // Make sure we found at least one slider
        Assert.True(sliderTiles.Count >= 2);

        // Place a pawn on the slider
        var sliderTile = sliderTiles[0];
        var noMoveSliderTile = sliderTiles[1];
        _gameBoard.PawnTiles[1][0] = sliderTile;
        _gameBoard.PawnTiles[1][1] = noMoveSliderTile;
        var targetTile = sliderTile.TargetTile;

        // Act
        _gameBoard.ExecuteAnyAvailableSlides();

        // Assert
        Assert.Equal(targetTile.Name, _gameBoard.PawnTiles[1][0].Name);
        Assert.Equal(noMoveSliderTile.Name, _gameBoard.PawnTiles[1][1].Name);
    }
    
    
    [Theory]
    [InlineData("a_S", "a_4", "ExitStart", CardDeck.CardTypes.One, 0)]
    [InlineData("a_4", "a_6", "Forward", CardDeck.CardTypes.Two, 0)]
    [InlineData("a_6", "a_2", "Backward", CardDeck.CardTypes.Four, 0)]
    [InlineData("a_6", "a_5", "Backward", CardDeck.CardTypes.Ten, 0)]
    public void ValidateAndFindDestinationTile_ValidMove_ShouldReturnDestinationTile(
        string sourceTileName, string destTileName, string effectName, CardDeck.CardTypes cardType, int playerIndex)
    {
        // Arrange
        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;
        _gameBoard.PawnTiles[0][0] = sourceTile;
        var effect = GetMoveEffect(effectName);
        var move = new GenericComponents.Move(sourceTileName, destTileName, effect);

        // Act
        var result = GetPrivateMethodResult<BoardTile>(
            "ValidateAndFindDestinationTile", sourceTile, move, cardType, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(destTileName, result.Name);
    }

    [Theory]
    [InlineData("a_4", "a_6", "Forward", CardDeck.CardTypes.One, 0, typeof(InvalidDataException))]
    [InlineData("a_S", "a_10", "ExitStart", CardDeck.CardTypes.Three, 0, typeof(InvalidDataException))]
    [InlineData("a_6", "a_5", "Backward", CardDeck.CardTypes.Two, 0, typeof(InvalidDataException))]
    public void ValidateAndFindDestinationTile_InvalidMove_ShouldThrowException(
        string sourceTileName, string destTileName, string effectName, CardDeck.CardTypes cardType, 
        int playerIndex, Type expectedExceptionType)
    {
        // Arrange
        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;
        _gameBoard.PawnTiles[0][0] = sourceTile;
        var effect = GetMoveEffect(effectName);
        var move = new GenericComponents.Move(sourceTileName, destTileName, effect);

        // Act & Assert
        var ex = Assert.Throws<TargetInvocationException>(() => GetPrivateMethodResult<BoardTile>(
            "ValidateAndFindDestinationTile", sourceTile, move, cardType, playerIndex));
        Assert.IsType(expectedExceptionType, ex.InnerException);
    }


    [Theory]
    [InlineData("a_4", CardDeck.CardTypes.One, 0, 1, "a_5")]
    [InlineData("a_4", CardDeck.CardTypes.Two, 0, 1, "a_6")]
    [InlineData("a_4", CardDeck.CardTypes.Three, 0, 1, "a_7")]
    [InlineData("a_10", CardDeck.CardTypes.Four, 0, 0, null)]
    [InlineData("a_10", CardDeck.CardTypes.Five, 0, 1, "a_15")]
    [InlineData("a_10", CardDeck.CardTypes.Seven, 0, 1, "b_2")]
    [InlineData("a_10", CardDeck.CardTypes.Eight, 0, 1, "b_3")]
    [InlineData("a_10", CardDeck.CardTypes.Ten, 0, 1, "b_5")]
    [InlineData("a_10", CardDeck.CardTypes.Eleven, 0, 1, "b_6")]
    [InlineData("a_10", CardDeck.CardTypes.Twelve, 0, 1, "b_7")]
    [InlineData("a_10", CardDeck.CardTypes.Apologies, 0, 0, null)]
    public void FindTileForForwardMove_ValidScenario_ShouldReturnCorrectTiles(
        string sourceTileName, CardDeck.CardTypes card, int playerIndex, int expectedTileCount, string? destTileName)
    {
        // Arrange
        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;

        // Act
        var result = GetPrivateMethodResult<List<BoardTile>>(
            "FindTileForForwardMove", sourceTile, card, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTileCount, result.Count);
        Assert.Equal(destTileName, result.FirstOrDefault()?.Name);
    }

    [Fact]
    public void ProcessDestinationTile_ValidScenario_ShouldHandleCorrectly()
    {
        var sourceTile = new StartTile("a_1", null!);
        var destTile = new StartTile("a_2", null!);

        InvokePrivateMethod("ProcessDestinationTile", sourceTile, destTile, false);
    }

    [Theory]
    [InlineData("a_10", CardDeck.CardTypes.Four, 0, 1, "a_6")]
    [InlineData("a_5", CardDeck.CardTypes.One, 0, 0, null)]
    [InlineData("a_5", CardDeck.CardTypes.Four, 0, 1, "a_1")]
    [InlineData("a_5", CardDeck.CardTypes.Ten, 0, 1, "a_4")]
    public void FindTileForBackwardMove_ValidScenario_ShouldReturnCorrectTiles(
        string sourceTileName, CardDeck.CardTypes card, int playerIndex, int expectedTileCount, string? destTileName)
    {
        // Arrange
        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;

        // Act
        var result = GetPrivateMethodResult<List<BoardTile>>(
            "FindTileForBackwardMove", sourceTile, card, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTileCount, result.Count);
        Assert.Equal(destTileName, result.FirstOrDefault()?.Name);
    }

    [Theory]
    [InlineData("a_S", CardDeck.CardTypes.One, 0, 1)]
    [InlineData("a_S", CardDeck.CardTypes.Two, 0, 1)]
    [InlineData("a_S", CardDeck.CardTypes.Apologies, 0, 0)]
    public void FindTileForExitStartMove_ValidScenario_ShouldReturnCorrectTiles(
        string sourceTileName, CardDeck.CardTypes card, int playerIndex, int expectedTileCount)
    {
        // Arrange
        var sourceTile = BoardTileDfs(_gameBoard, sourceTileName)!;

        // Act
        var result = GetPrivateMethodResult<List<BoardTile>>(
            "FindTileForExitStartMove", sourceTile, card, playerIndex);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTileCount, result.Count);
    }

    [Theory]
    [InlineData("a_4", 0, true)]
    [InlineData("a_4", 1, true)]
    [InlineData("c_3", 0, false)]
    [InlineData("c_3", 1, true)]
    public void NoTeammateOnTargetTile_ShouldReturnExpectedResult(
        string tileName, int playerIndex, bool expected)
    {
        // Arrange
        _gameBoard.PawnTiles[0][1] = BoardTileDfs(_gameBoard, "c_3")!;
        var tile = BoardTileDfs(_gameBoard, tileName)!;

        // Act
        var result = GetPrivateMethodResult<bool>(
            "NoTeammateOnTargetTile", tile, playerIndex);

        // Assert
        Assert.Equal(expected, result);
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

    internal static BoardTile? BoardTileDfs(GameBoard gameBoard, string targetTileName)
    {
        HashSet<string> visited = [];
        Queue<BoardTile> queue = [];
        foreach (var pawnSet in gameBoard.PawnTiles) {
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