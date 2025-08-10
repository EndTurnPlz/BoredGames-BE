using BoredGames.Games.Warlocks;
using Xunit;
using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Games.Warlocks.Deck;
using BoredGames.Games.Warlocks.Models;

namespace BoredGames.UnitTests.Warlocks;

public class WarlocksGameTest
{
    private readonly List<Player> _players;
    private readonly WarlocksGameConfig _config;
    private readonly WarlocksGame _game;

    public WarlocksGameTest()
    {
        // Setup standard test configuration
        _players =
        [
            new Player("Player1"),
            new Player("Player2"),
            new Player("Player3")
        ];

        _config = new WarlocksGameConfig
        {
            ShuffleTurnOrder = false, // For predictable testing
            NumRounds = 3,            // Use fewer rounds for faster tests
            RevealBids = true
        };

        _game = new WarlocksGame(_config, _players.ToImmutableList());
    }

    [Fact]
    public void Constructor_InitializesGameWithCorrectState()
    {
        // Assert
        Assert.False(_game.HasEnded());

        // Check the first player's snapshot to verify the initial state
        var snapshot = _game.GetSnapshot(_players[0]) as WarlocksBidSnapshot;
        Assert.NotNull(snapshot);
        Assert.Equal("Bid", snapshot.GameState);
        Assert.Equal(1, snapshot.RoundNumber);
        Assert.Equal(WarlocksDeck.Suit.None, snapshot.TrumpSuite); // Default before first card drawn
        Assert.Equal(3, snapshot.TurnOrder.Count());
        Assert.Equal(0, snapshot.PlayerPoints.Sum()); // No points initially
    }

    [Fact]
    public void BidAction_ValidBid_UpdatesPlayerBid()
    {
        // Arrange
        var player = _players[0];
        var bidArgs = new ActionArgs.BidArgs { Bid = 1 };

        // Act - get the initial snapshot, then bid
        var initialSnapshot = _game.GetSnapshot(player) as WarlocksBidSnapshot;
        Assert.NotNull(initialSnapshot);
        Assert.Equal(-1, initialSnapshot.ThisPlayerBid); // The initial bid is -1 (unset)

        // Use reflection to call the private BidAction method
        var method = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_game, [player, bidArgs]);

        // Assert
        var updatedSnapshot = _game.GetSnapshot(player) as WarlocksBidSnapshot;
        Assert.NotNull(updatedSnapshot);
        Assert.Equal(1, updatedSnapshot.ThisPlayerBid);
    }

    [Fact]
    public void BidAction_InvalidBidTooHigh_ThrowsException()
    {
        // Arrange
        var player = _players[0];
        var bidArgs = new ActionArgs.BidArgs { Bid = 2 }; // Invalid in round 1 (max is 1)

        // Act & Assert
        var method = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => method!.Invoke(_game, [player, bidArgs]));

        Assert.IsType<BadActionArgsException>(exception.InnerException);
    }

    [Fact]
    public void BidAction_InvalidBidNegative_ThrowsException()
    {
        // Arrange
        var player = _players[0];
        var bidArgs = new ActionArgs.BidArgs { Bid = -1 }; // Negative bid

        // Act & Assert
        var method = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => method!.Invoke(_game, [player, bidArgs]));

        Assert.IsType<BadActionArgsException>(exception.InnerException);
    }

    [Fact]
    public void BidAction_AllPlayersBid_AdvancesToPlayTrickState()
    {
        // Arrange
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act - All players bid
        foreach (var p in _players) {
            var bidArgs = new ActionArgs.BidArgs { Bid = 0 };
            bidMethod!.Invoke(_game, [p, bidArgs]);
        }

        // Assert
        var snapshot = _game.GetSnapshot(_players[0]);
        Assert.IsType<WarlocksPlayTrickSnapshot>(snapshot);
        Assert.Equal("PlayTrick", ((WarlocksPlayTrickSnapshot)snapshot).GameState);
    }

    [Fact]
    public void PlayCardAction_ValidCard_AdvancesTurn()
    {
        // Arrange - First, get all players to bid to advance to PlayTrickState.
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var t in _players) {
            var bidArgs = new ActionArgs.BidArgs { Bid = 0 };
            bidMethod!.Invoke(_game, [t, bidArgs]);
        }

        // Get the current player's snapshot to find a card to play
        var snapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
        Assert.NotNull(snapshot);

        // Find the current player
        var currentPlayerIndex = snapshot.CurrentTrick.CurrentPlayerIndex;
        var currentPlayer = _players[currentPlayerIndex];

        // Get the current player's hand
        var currentPlayerSnapshot = _game.GetSnapshot(currentPlayer) as WarlocksPlayTrickSnapshot;
        Assert.NotNull(currentPlayerSnapshot);
        Assert.NotEmpty(currentPlayerSnapshot.ThisPlayerHand);

        // Get a valid card to play
        var cardToPlay = currentPlayerSnapshot.ThisPlayerHand.First().card;

        // Act - Play the card
        var playCardMethod = typeof(WarlocksGame).GetMethod("PlayCardAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playCardArgs = new ActionArgs.PlayCardArgs { Card = cardToPlay };
        playCardMethod!.Invoke(_game, [currentPlayer, playCardArgs]);

        // Assert - Check that turn advanced to the next player
        var newSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
        Assert.NotNull(newSnapshot);

        // The current player index should have changed
        Assert.NotEqual(currentPlayerIndex, newSnapshot.CurrentTrick.CurrentPlayerIndex);

        // The card should be in the trick cards played
        Assert.Contains(cardToPlay, newSnapshot.CurrentTrick.CardsPlayed);
    }

    [Fact]
    public void PlayCardAction_WrongPlayer_ThrowsException()
    {
        // Arrange - First, get all players to bid to advance to PlayTrickState
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var t in _players) {
            var bidArgs = new ActionArgs.BidArgs { Bid = 0 };
            bidMethod!.Invoke(_game, [t, bidArgs]);
        }

        // Get the current player's snapshot
        var snapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
        Assert.NotNull(snapshot);

        // Find a player who is NOT the current player
        var currentPlayerIndex = snapshot.CurrentTrick.CurrentPlayerIndex;
        var wrongPlayer = _players.First(p => _players.IndexOf(p) != currentPlayerIndex);

        // Get a card to play (doesn't matter which one for this test)
        var wrongPlayerSnapshot = _game.GetSnapshot(wrongPlayer) as WarlocksPlayTrickSnapshot;
        var cardToPlay = wrongPlayerSnapshot!.ThisPlayerHand.First().card;

        // Act & Assert - Try to play a card with the wrong player
        var playCardMethod = typeof(WarlocksGame).GetMethod("PlayCardAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playCardArgs = new ActionArgs.PlayCardArgs { Card = cardToPlay };

        var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => playCardMethod!.Invoke(_game, [wrongPlayer, playCardArgs]));

        Assert.IsType<InvalidPlayerException>(exception.InnerException);
    }

    [Fact]
    public void TrickWinner_WarlocksWinTricks()
    {
        // This test requires a controlled environment with specific cards
        // Create a mock game with a customized deck to test Warlock winning logic

        // Setup game with controlled bids
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var t in _players) {
            var bidArgs = new ActionArgs.BidArgs { Bid = 0 };
            bidMethod!.Invoke(_game, [t, bidArgs]);
        }

        // Play a complete trick where a Warlock is played
        var playCardMethod = typeof(WarlocksGame).GetMethod("PlayCardAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // We'll need to play 3 cards (one for each player)
        // The first two players play normal cards
        // The third player plays a Warlock which should win

        // Find the current player
        var snapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
        Assert.NotNull(snapshot);

        // Play cards for all three players
        for (var i = 0; i < _players.Count; i++)
        {
            // Get the current player's snapshot after each card play
            var currentSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
            Assert.NotNull(currentSnapshot);

            var currentPlayerIndex = currentSnapshot.CurrentTrick.CurrentPlayerIndex;
            var currentPlayer = _players[currentPlayerIndex];

            // Get the player's hand
            var playerSnapshot = _game.GetSnapshot(currentPlayer) as WarlocksPlayTrickSnapshot;
            Assert.NotNull(playerSnapshot);

            // Find a card to play (preferring a Warlock for the last player if available)
            WarlocksDeck.Card cardToPlay;
            if (i == _players.Count - 1)
            {
                // Try to find a Warlock for the last player
                cardToPlay = playerSnapshot.ThisPlayerHand
                    .FirstOrDefault(c => c.card.Rank == WarlocksDeck.Rank.Warlock).card;

                // If no Warlock is available, just play any card
            }
            else
            {
                // For other players, explicitly avoid playing Warlocks
                cardToPlay = playerSnapshot.ThisPlayerHand
                    .FirstOrDefault(c => c.card.Rank != WarlocksDeck.Rank.Warlock).card;

                // If only Warlocks are available, play any card
            }

            if (cardToPlay == null)
            {
                cardToPlay = playerSnapshot.ThisPlayerHand.First().card;
            }

            // Play the card
            var playCardArgs = new ActionArgs.PlayCardArgs { Card = cardToPlay };
            playCardMethod!.Invoke(_game, [currentPlayer, playCardArgs]);
        }

        // Get the last trick result
        var finalSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayingSnapshot;
        Assert.NotNull(finalSnapshot);
        Assert.NotNull(finalSnapshot.LastTrickResult);

        // Check if the last trick contains a Warlock
        var warlockPlayed = finalSnapshot.LastTrickResult.Cards.Any(c => c.Rank == WarlocksDeck.Rank.Warlock);

        if (!warlockPlayed) return;
        // If a Warlock was played, verify that the winner is the player who played it
        var warlockIndex = finalSnapshot.LastTrickResult.Cards
            .Select((card, index) => new { Card = card, Index = index })
            .First(item => item.Card.Rank == WarlocksDeck.Rank.Warlock)
            .Index;

        var expectedWinnerIndex = (warlockIndex + finalSnapshot.LastTrickResult.Leader) % _players.Count;
        Assert.Equal(expectedWinnerIndex, finalSnapshot.LastTrickResult.Winner);
    }

    [Fact]
    public void TrickWinner_TrumpSuitWinsOverNonTrump()
    {
        // Similar to the Warlock test, but we'll control the play to ensure
        // a trump card wins over non-trump cards

        // Setup game with controlled bids
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var t in _players) {
            var bidArgs = new ActionArgs.BidArgs { Bid = 0 };
            bidMethod!.Invoke(_game, [t, bidArgs]);
        }

        // Get the current trump suit
        var initialSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
        Assert.NotNull(initialSnapshot);
        var trumpSuit = initialSnapshot.TrumpSuite;

        // Skip this test if no trump suit is available
        if (trumpSuit == WarlocksDeck.Suit.None)
        {
            return;
        }

        var playCardMethod = typeof(WarlocksGame).GetMethod("PlayCardAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Play cards for all three players
        for (var i = 0; i < _players.Count; i++)
        {
            // Get the current player's snapshot after each card play
            var currentSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot;
            Assert.NotNull(currentSnapshot);

            var currentPlayerIndex = currentSnapshot.CurrentTrick.CurrentPlayerIndex;
            var currentPlayer = _players[currentPlayerIndex];

            // Get the player's hand
            var playerSnapshot = _game.GetSnapshot(currentPlayer) as WarlocksPlayTrickSnapshot;
            Assert.NotNull(playerSnapshot);

            // Find a card to play
            WarlocksDeck.Card cardToPlay;

            if (i == 1) // The second player tries to play a trump card
            {
                // Try to find a trump card
                cardToPlay = playerSnapshot.ThisPlayerHand
                    .FirstOrDefault(c => c.card.Suit == trumpSuit && 
                                    c.card.Rank != WarlocksDeck.Rank.Warlock && 
                                    c.card.Rank != WarlocksDeck.Rank.Joker).card;

                // If no suitable trump card is available, just play any card
            }
            else
            {
                // Other players try to avoid playing trump cards
                cardToPlay = playerSnapshot.ThisPlayerHand
                    .FirstOrDefault(c => c.card.Suit != trumpSuit && 
                                    c.card.Rank != WarlocksDeck.Rank.Warlock).card;

                // If only trump cards or Warlocks are available, play any card
            }

            if (cardToPlay == null)
            {
                cardToPlay = playerSnapshot.ThisPlayerHand.First().card;
            }

            // Play the card
            var playCardArgs = new ActionArgs.PlayCardArgs { Card = cardToPlay };
            playCardMethod!.Invoke(_game, [currentPlayer, playCardArgs]);
        }

        // Get the last trick result
        var finalSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayingSnapshot;
        Assert.NotNull(finalSnapshot);
        Assert.NotNull(finalSnapshot.LastTrickResult);

        // Check if trump cards were played and no Warlocks
        var trumpCardsPlayed = finalSnapshot.LastTrickResult.Cards
            .Where(c => c.Suit == trumpSuit && c.Rank != WarlocksDeck.Rank.Warlock)
            .ToList();

        var noWarlocksPlayed = finalSnapshot.LastTrickResult.Cards.All(c => c.Rank != WarlocksDeck.Rank.Warlock);

        if (trumpCardsPlayed.Any() && noWarlocksPlayed)
        {
            // Find the highest trump card played
            var highestTrump = trumpCardsPlayed.OrderByDescending(c => c.Rank).First();

            // Find the index of the player who played it
            var trumpCardIndex = finalSnapshot.LastTrickResult.Cards
                .Select((card, index) => new { Card = card, Index = index })
                .First(item => item.Card.Equals(highestTrump))
                .Index;

            var expectedWinnerIndex = (trumpCardIndex + finalSnapshot.LastTrickResult.Leader) % _players.Count;
            Assert.Equal(expectedWinnerIndex, finalSnapshot.LastTrickResult.Winner);
        }
    }

    [Fact]
    public void RoundOver_CorrectScoring_AwardsPointsBasedOnBids()
    {
        // Play through an entire round and verify scoring
        // We'll need to play multiple tricks to complete a round

        // Setup game with known bids
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Player 1 bids 0, Player 2 bids 1, Player 3 bids 0
        bidMethod!.Invoke(_game, [_players[0], new ActionArgs.BidArgs { Bid = 0 }]);
        bidMethod.Invoke(_game, [_players[1], new ActionArgs.BidArgs { Bid = 1 }]);
        bidMethod.Invoke(_game, [_players[2], new ActionArgs.BidArgs { Bid = 0 }]);

        // Now play cards for the trick
        var playCardMethod = typeof(WarlocksGame).GetMethod("PlayCardAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Play through the entire round (1 card per player since it's round 1)
        foreach (var currentSnapshot in _players.Select(_ => _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot)) {
            Assert.NotNull(currentSnapshot);

            var currentPlayerIndex = currentSnapshot.CurrentTrick.CurrentPlayerIndex;
            var currentPlayer = _players[currentPlayerIndex];

            var playerSnapshot = _game.GetSnapshot(currentPlayer) as WarlocksPlayTrickSnapshot;
            Assert.NotNull(playerSnapshot);

            // Play the first card in the player's hand
            var cardToPlay = playerSnapshot.ThisPlayerHand.First().card;
            playCardMethod!.Invoke(_game, [currentPlayer, new ActionArgs.PlayCardArgs { Card = cardToPlay }]);
        }

        // Round should be complete now, and we should be back in BidState for round 2
        var finalSnapshot = _game.GetSnapshot(_players[0]);
        Assert.IsType<WarlocksBidSnapshot>(finalSnapshot);
        Assert.Equal(2, ((WarlocksBidSnapshot)finalSnapshot).RoundNumber);

        // Check scores
        var playerPoints = ((WarlocksBidSnapshot)finalSnapshot).PlayerPoints.ToArray();

        // Player 1 bid 0
        // Player 2 bid 1
        // Player 3 bid 0
        // Depending on who won the trick, the points should reflect that

        var previousSnapshot = _game.GetSnapshot(_players[0]) as WarlocksPlayingSnapshot;
        var trickWinner = previousSnapshot!.LastTrickResult!.Winner;

        // Player who won the trick should either get 30 points (if they bid 1) or lose 10 points (if they bid 0)
        // Players who didn't win a trick should get 20 points (if they bid 0) or lose 10 points (if they bid 1)

        for (int i = 0; i < _players.Count; i++)
        {
            if (i == trickWinner)
            {
                // This player won the trick
                if (i == 1) // Player 2 bid 1
                {
                    Assert.Equal(30, playerPoints[i]); // 20 for a correct bid + 10 for the trick
                }
                else // Player 1 or 3 bid 0
                {
                    Assert.Equal(-10, playerPoints[i]); // -10 for being wrong by 1 trick
                }
            }
            else
            {
                // This player didn't win any tricks
                if (i == 1) // Player 2 bid 1
                {
                    Assert.Equal(-10, playerPoints[i]); // -10 for being wrong by 1 trick
                }
                else // Player 1 or 3 bid 0
                {
                    Assert.Equal(20, playerPoints[i]); // 20 for a correct bid of 0
                }
            }
        }
    }

    [Fact]
    public void GameEnd_AfterConfiguredRounds_StateChangesToEnd()
    {
        // Play through all rounds and verify the game ends
        var bidMethod = typeof(WarlocksGame).GetMethod("BidAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var playCardMethod = typeof(WarlocksGame).GetMethod("PlayCardAction", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Play through 3 rounds (as configured in the test setup)
        for (var round = 0; round < _config.NumRounds; round++) {
            // All players bid 0 for simplicity
            foreach (var t in _players) {
                bidMethod!.Invoke(_game, [t, new ActionArgs.BidArgs { Bid = 0 }]);
            }

            // Play through the current round (each player plays one card per trick)
            for (var trick = 0; trick < round + 1; trick++) {
                foreach (var currentSnapshot in _players.Select(_ => _game.GetSnapshot(_players[0]) as WarlocksPlayTrickSnapshot)) {
                    Assert.NotNull(currentSnapshot);

                    var currentPlayerIndex = currentSnapshot.CurrentTrick.CurrentPlayerIndex;
                    var currentPlayer = _players[currentPlayerIndex];

                    var playerSnapshot = _game.GetSnapshot(currentPlayer) as WarlocksPlayTrickSnapshot;
                    var cardToPlay = playerSnapshot!.ThisPlayerHand.First().card;

                    playCardMethod!.Invoke(_game, [currentPlayer, new ActionArgs.PlayCardArgs { Card = cardToPlay }]);
                }
            }
        }

        // After all rounds, the game should be in EndState
        Assert.True(_game.HasEnded());

        var finalSnapshot = _game.GetSnapshot(_players[0]);
        Assert.IsType<WarlocksEndSnapshot>(finalSnapshot);
        Assert.Equal("End", ((WarlocksEndSnapshot)finalSnapshot).GameState);
    }
}