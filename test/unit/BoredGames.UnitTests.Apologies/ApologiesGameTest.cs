using BoredGames.Games.Apologies;
using BoredGames.Core.Game;
using BoredGames.Games.Apologies.Models;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using BoredGames.Core;
using BoredGames.Games.Apologies.Board;
using BoredGames.Games.Apologies.Deck;
using BoredGames.UnitTests.Apologies.Board;

namespace BoredGames.UnitTests.Apologies;

public class ApologiesGameTest
{
    private readonly ImmutableList<Player> _players = ImmutableList.Create(
        new Player("Player1")
        {
            IsConnected = true
        },
        new Player("Player2")
        {
            IsConnected = true
        },
        new Player("Player3")
        {
            IsConnected = true
        },
        new Player("Player4")
        {
            IsConnected = true
        });

    [Fact]
    public void Constructor_ShouldInitializeGameCorrectly()
    {
        // Arrange & Act
        var game = new ApologiesGame(_players);
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.False(game.HasEnded());
        Assert.Equal(ApologiesGame.State.P1Draw, snapshot.GameState);
        Assert.Equal(4, snapshot.TurnOrder.Count());
    }

    [Fact]
    public void DrawCard_ShouldThrowInvalidPlayerException_WhenWrongPlayerDraws()
    {
        // Arrange
        var game = new ApologiesGame(_players);

        // Act & Assert
        Assert.Throws<InvalidPlayerException>(() => 
            game.ExecuteAction("draw", _players[1]));
    }

    [Fact]
    public void MovePawn_ShouldThrowInvalidPlayerException_WhenWrongPlayerMoves()
    {
        // Arrange
        var game = new ApologiesGame(_players);
        var move = new GenericModels.Move("a_1", "a_3", 0);
        
        // Act & Assert
        Assert.Throws<InvalidPlayerException>(() =>
            game.ExecuteAction("move", _players[1],ToJsonElement(new ActionArgs.MovePawnArgs(move))));
    }

    [Fact]
    public void HasEnded_ShouldReturnTrue_WhenGameStateIsEnd()
    {
        // Arrange
        var game = new ApologiesGame(_players);
        typeof(ApologiesGame)
            .GetProperty("GameState", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(game, ApologiesGame.State.End);

        // Act & Assert
        Assert.True(game.HasEnded());
    }

    [Fact]
    public void HasEnded_ShouldReturnFalse_WhenGameStateIsNotEnd()
    {
        // Arrange
        var game = new ApologiesGame(_players);
        
        // Act & Assert
        foreach (var state in Enum.GetValues<ApologiesGame.State>()) {
            if (state == ApologiesGame.State.End) continue;
            typeof(ApologiesGame)
                .GetProperty("GameState", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(game, state);
            Assert.False(game.HasEnded());
        }
    }

    [Fact]
    public void GetSnapshot_ShouldReturnValidSnapshot()
    {
        // Arrange
        var game = new ApologiesGame(_players);

        // Act
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(_players[0].Username, snapshot.TurnOrder.ElementAt(0));
        Assert.Equal(_players.Count, snapshot.TurnOrder.Count());
    }
    
    [Fact]
    public void DrawCard_ShouldTransitionToMoveState()
    {
        // Arrange
        var game = CreateGameWithCards([CardDeck.CardTypes.One]);

        // Act
        game.ExecuteAction("draw", _players[0]);
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.Equal(ApologiesGame.State.P1Move, snapshot.GameState);
    }

    [Fact]
    public void MovePawn_ShouldTransitionToNextPlayerDrawState()
    {
        // Arrange
        var game = CreateGameWithCards([CardDeck.CardTypes.One]);
        var res = (game.ExecuteAction("draw", _players[0]) as ActionResponses.DrawCardResponse)!;
        var moveOpt = res.Movesets.ElementAt(0).Opts.ElementAt(0);
        var move = new GenericModels.Move(moveOpt.From, moveOpt.To, moveOpt.Effects.First());

        // Act
        game.ExecuteAction("move", _players[0], ToJsonElement(new ActionArgs.MovePawnArgs(move)));
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.Equal(ApologiesGame.State.P2Draw, snapshot.GameState);
    }

    [Fact]
    public void GameCompletion_ShouldTransitionToEndState()
    {
        // Arrange
        var game = CreateGameWithCards([CardDeck.CardTypes.One]);
        var gameBoard = (GameBoard)typeof(ApologiesGame)
            .GetField("_gameBoard", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(game)!;

        var before = GameBoardTest.BoardTileDfs(gameBoard, "a_s5")!;
        var home = GameBoardTest.BoardTileDfs(gameBoard, "a_H")!;


        gameBoard.PawnTiles[0] = Enumerable.Repeat(home, 4).ToArray();
        gameBoard.PawnTiles[0][0] = before;

        // Act
        game.ExecuteAction("draw", _players[0]);
        var move = new GenericModels.Move("a_s5", "a_H", 0);
        game.ExecuteAction("move", _players[0], ToJsonElement(new ActionArgs.MovePawnArgs(move)));
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.Equal(ApologiesGame.State.End, snapshot.GameState);
    }

    [Fact]
    public void TwoCard_ShouldLetSamePlayerMoveTwice()
    {
        // Arrange
        var game = CreateGameWithCards([CardDeck.CardTypes.Two]);
        var gameBoard = (GameBoard)typeof(ApologiesGame)
            .GetField("_gameBoard", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(game)!;
        
        gameBoard.PawnTiles[0][0] = GameBoardTest.BoardTileDfs(gameBoard, "a_10")!;
        
        game.ExecuteAction("draw", _players[0]);
        var firstMove = new GenericModels.Move("a_10", "a_12", 0);

        // Act
        game.ExecuteAction("move", _players[0], ToJsonElement(new ActionArgs.MovePawnArgs(firstMove)));
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.Equal(ApologiesGame.State.P1Draw, snapshot.GameState);
    }

    [Fact]
    public void InvalidMove_ShouldMaintainCurrentMoveState()
    {
        // Arrange
        var game = CreateGameWithCards([CardDeck.CardTypes.Two]);
        typeof(ApologiesGame)
            .GetProperty("GameState", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(game, ApologiesGame.State.P1Draw);
        game.ExecuteAction("draw", _players[0]);
        var invalidMove = new GenericModels.Move("invalid", "invalid", 0);
        
        // Act
        Assert.Throws<InvalidMoveException>(() => 
            game.ExecuteAction("move", _players[0], ToJsonElement(new ActionArgs.MovePawnArgs(invalidMove))));
        var snapshot = game.GetSnapshot();

        // Assert
        Assert.Equal(ApologiesGame.State.P1Move, snapshot.GameState);
    }
    
    private ApologiesGame CreateGameWithCards(IEnumerable<CardDeck.CardTypes> cards)
    {
        var game = new ApologiesGame(_players);
        var cardDeck = new CardDeck();
        typeof(CardDeck).GetField("_cards", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(cardDeck, new List<CardDeck.CardTypes>(cards));
        typeof(ApologiesGame).GetField("_cardDeck", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(game, cardDeck);
        return game;
    }
    
    private static JsonElement? ToJsonElement(object? obj)
    {
        if (obj is null) return null;

        var json = JsonSerializer.Serialize(obj);
        return JsonDocument.Parse(json).RootElement;
    }
}