using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Games.UpsAndDowns;
using BoredGames.Games.UpsAndDowns.Board;
using BoredGames.Games.UpsAndDowns.Models;
using Xunit;

namespace BoredGames.UnitTests.UpsAndDowns;

/// <summary>
/// Contains tests for the <see cref="UpsAndDownsGame"/> class.
/// </summary>
public class UpsAndDownsGameTests
{
    private readonly Player _player1 = new("Player 1");
    private readonly Player _player2 = new("Player 2");
    private readonly Player _player3 = new("Player 3");

    /// <summary>
    /// Creates a new game instance with the specified number of players.
    /// </summary>
    private (UpsAndDownsGame game, ImmutableList<Player> players) CreateGame(int playerCount)
    {
        if (playerCount is < 2 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(playerCount), "Player count must be between 2 and 5 for these tests.");
        }
        
        var allPlayers = new[] { _player1, _player2, _player3 };
        var players = allPlayers.Take(playerCount).ToImmutableList();
        var game = new UpsAndDownsGame(players);
        
        return (game, players);
    }

    //---------------------------------------------------------------------------------
    
    [Fact]
    public void Constructor_WhenGameStarts_InitializesCorrectly()
    {
        // Arrange
        var (game, _) = CreateGame(2);

        // Act
        var snapshot = (UpsAndDownsSnapshot)game.GetSnapshot();

        // Assert
        Assert.False(game.HasEnded());
        Assert.Equal(UpsAndDownsGame.State.P1Turn, snapshot.GameState);
        Assert.Equal(-1, snapshot.LastDieRoll); // Die has not been rolled yet
        Assert.Equal(2, snapshot.PlayerLocations.Count());
        Assert.All(snapshot.PlayerLocations, loc => Assert.Equal(0, loc)); // Players start at position 0
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void PlayerMoveAction_WithInvalidPlayer_ThrowsInvalidPlayerException()
    {
        // Arrange
        var (game, _) = CreateGame(2);
        var invalidPlayer = new Player("Invalid Player");

        // Act & Assert
        Assert.Throws<InvalidPlayerException>(() => game.ExecuteAction("move", invalidPlayer));
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void PlayerMoveAction_WithValidPlayer_AdvancesPlayerAndGameState()
    {
        // Arrange
        var (game, players) = CreateGame(2);
        var gameBoardField = typeof(UpsAndDownsGame).GetField("_gameBoard", BindingFlags.NonPublic | BindingFlags.Instance);
        var emptyWarpTilesBoard = GameBoard.Create(players!.Count, new Dictionary<int, int>());
        gameBoardField?.SetValue(game, emptyWarpTilesBoard);
        
        var initialSnapshot = (UpsAndDownsSnapshot)game.GetSnapshot();
        var p1InitialPosition = initialSnapshot.PlayerLocations.ElementAt(0);

        // Act
        game.ExecuteAction("move", players[0]);
        var newSnapshot = (UpsAndDownsSnapshot)game.GetSnapshot();

        // Assert
        Assert.Equal(UpsAndDownsGame.State.P2Turn, newSnapshot.GameState);
        Assert.InRange(newSnapshot.LastDieRoll, 1, 6);
        
        // Assuming no warp tile at the destination for this simple test
        Assert.Equal(p1InitialPosition + newSnapshot.LastDieRoll, newSnapshot.PlayerLocations.ElementAt(0)); 
        Assert.Equal(initialSnapshot.PlayerLocations.ElementAt(1), newSnapshot.PlayerLocations.ElementAt(1)); // Player 2 has not moved
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void AdvanceGameState_WhenCalledSequentially_CyclesThroughPlayerTurns()
    {
        // Arrange
        var (game, players) = CreateGame(3);

        // Act & Assert
        // P1's Turn -> P2's Turn
        game.ExecuteAction("move", players[0]);
        var snapshot1 = (UpsAndDownsSnapshot)game.GetSnapshot();
        Assert.Equal(UpsAndDownsGame.State.P2Turn, snapshot1.GameState);
        
        // P2's Turn -> P3's Turn
        game.ExecuteAction("move", players[1]);
        var snapshot2 = (UpsAndDownsSnapshot)game.GetSnapshot();
        Assert.Equal(UpsAndDownsGame.State.P3Turn, snapshot2.GameState);
        
        // P3's Turn -> P1's Turn
        game.ExecuteAction("move", players[2]);
        var snapshot3 = (UpsAndDownsSnapshot)game.GetSnapshot();
        Assert.Equal(UpsAndDownsGame.State.P1Turn, snapshot3.GameState);
    }

    //---------------------------------------------------------------------------------
    
    [Fact]
    public void AdvanceGameState_WhenPlayerReachesEnd_SetsStateToEnd()
    {
        // Arrange
        var (game, _) = CreateGame(2);
        
        // Use reflection to force a player to be on the winning tile (100).
        // This is necessary because we cannot control the die roll to guarantee a win.
        var gameBoardField = typeof(UpsAndDownsGame).GetField("_gameBoard", BindingFlags.NonPublic | BindingFlags.Instance);
        var gameBoard = gameBoardField?.GetValue(game);

        var playerPositionsField = gameBoard?.GetType().GetField("_playerPositions", BindingFlags.NonPublic | BindingFlags.Instance);
        var playerPositions = playerPositionsField?.GetValue(gameBoard) as List<int>;
        
        Assert.NotNull(playerPositions);
        playerPositions[0] = 100; // Place Player 1 on the winning tile.
        
        // Advance the game state
        game.GetType().GetMethod("AdvanceGameState", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(game, null);
        
        // Assert
        var snapshot = (UpsAndDownsSnapshot)game.GetSnapshot();
        Assert.Equal(UpsAndDownsGame.State.End, snapshot.GameState);
        Assert.True(game.HasEnded());
    }

    //---------------------------------------------------------------------------------

    [Fact]
    public void GameActions_WhenGameHasEnded_DoNotChangeState()
    {
        // Arrange
        var (game, players) = CreateGame(2);

        // Force the game into an 'End' state using reflection
        var gameStateProperty = typeof(UpsAndDownsGame).GetProperty("GameState", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(gameStateProperty);
        gameStateProperty.SetValue(game, UpsAndDownsGame.State.End);
        
        var initialSnapshot = (UpsAndDownsSnapshot)game.GetSnapshot();

        // Act: Try to move a player after the game has ended
        Assert.Throws<InvalidOperationException>(() => game.ExecuteAction("move", players[0]));
        var newSnapshot = (UpsAndDownsSnapshot)game.GetSnapshot();

        // Assert
        Assert.True(game.HasEnded());
        Assert.Equal(UpsAndDownsGame.State.End, newSnapshot.GameState);
        // Assert that no game state values have changed
        Assert.Equal(initialSnapshot.PlayerLocations, newSnapshot.PlayerLocations);
        Assert.Equal(initialSnapshot.LastDieRoll, newSnapshot.LastDieRoll);
    }
    
    //---------------------------------------------------------------------------------
    
    [Fact]
    public void PlayerMoveAction_WhenNotPlayersTurn_ThrowsInvalidOperationException()
    {
        // Arrange
        var (game, players) = CreateGame(2);
    
        // The game starts in P1's turn by default.
        var initialSnapshot = game.GetSnapshot() as UpsAndDownsSnapshot;

        // Act & Assert
        // Attempting to move Player 2 during Player 1's turn should fail.
        Assert.Throws<InvalidPlayerException>(() => game.ExecuteAction("move", players[1]));
    
        // Verify that the game state has not changed.
        var finalSnapshot = game.GetSnapshot() as UpsAndDownsSnapshot;
        Assert.Equal(initialSnapshot!.GameState, finalSnapshot!.GameState);
        Assert.Equal(initialSnapshot!.PlayerLocations, finalSnapshot!.PlayerLocations);
    }
}