using System;
using BoredGames.Games.UpsAndDowns.Board;
using Xunit;

namespace BoredGames.UnitTests.UpsAndDowns.Board;

/// <summary>
/// Contains tests for the <see cref="GameBoard"/> class.
/// </summary>
public class GameBoardTests
{
    [Fact]
    public void Constructor_WhenCalled_InitializesAllPlayersAtPositionZero()
    {
        // Arrange
        const int playerCount = 4;

        // Act
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(playerCount);

        // Assert
        Assert.Equal(playerCount, gameBoard.PlayerPositions.Count);
        Assert.All(gameBoard.PlayerPositions, position => Assert.Equal(0, position));
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void IsPlayerOnEnd_WhenNoPlayerIsOnTile100_ReturnsFalse()
    {
        // Arrange
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(2);
        gameBoard.MovePlayer(0, 50);
        gameBoard.MovePlayer(1, 99);

        // Act
        var result = gameBoard.IsPlayerOnEnd;

        // Assert
        Assert.False(result);
    }
    
    //---------------------------------------------------------------------------------
    
    [Fact]
    public void IsPlayerOnEnd_WhenAPlayerIsOnTile100_ReturnsTrue()
    {
        // Arrange
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(2);
        gameBoard.MovePlayer(0, 98); // Position is now 97
        gameBoard.MovePlayer(0, 2);  // Move 3 to land exactly on 100

        // Act
        var result = gameBoard.IsPlayerOnEnd;

        // Assert
        Assert.True(result);
        Assert.Equal(100, gameBoard.PlayerPositions[0]);
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void MovePlayer_WithNormalMove_UpdatesPositionCorrectly()
    {
        // Arrange
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(1);
        gameBoard.MovePlayer(0, 15); // Starting position is 15
        
        // Act
        gameBoard.MovePlayer(0, 5); // Move 5 spaces
        
        // Assert
        Assert.Equal(20, gameBoard.PlayerPositions[0]);
    }
    
    //---------------------------------------------------------------------------------

    [Fact]
    public void MovePlayer_WhenOvershootingEnd_PositionRemainsUnchanged()
    {
        // Arrange
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(1);
        gameBoard.MovePlayer(0, 98); // Set the start position to 98
        
        // Act
        gameBoard.MovePlayer(0, 5); // Attempt to move past 100
        
        // Assert
        Assert.Equal(98, gameBoard.PlayerPositions[0]);
    }
    
    //---------------------------------------------------------------------------------
    
    [Fact]
    public void MovePlayer_WhenLandingOnAnyWarpTile_UpdatesToWarpedPosition()
    {
        // Arrange
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(1);

        // Act & Assert
        // This test dynamically checks every warp tile defined in the GameBoard.
        foreach (var (startTile, endTile) in gameBoard.WarpTiles)
        {

            // Reset player position to 0 for a clean move
            var resetBoard = GameBoard.CreateWithDefaultWarpTiles(1);
            
            // Move the player to the start of the warp tile
            resetBoard.MovePlayer(0, startTile);
            
            // The position should now be the warped endTile
            Assert.Equal(endTile, resetBoard.PlayerPositions[0]);
        }
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void MovePlayer_WithInvalidPlayerIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var gameBoard = GameBoard.CreateWithDefaultWarpTiles(1); // Board has index 0
        const int invalidIndex = 1;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => gameBoard.MovePlayer(invalidIndex, 5));
    }
}