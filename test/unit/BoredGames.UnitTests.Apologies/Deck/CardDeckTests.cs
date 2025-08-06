using BoredGames.Games.Apologies.Deck;
using JetBrains.Annotations;

namespace BoredGames.UnitTests.Apologies.Deck;

[TestSubject(typeof(CardDeck))]
public class CardDeckTests
{
    private static readonly CardDeck.Card[] ValidCardTypes = 
        Enum.GetValues<CardDeck.Card>()
            .Where(c => c != CardDeck.Card.Undefined)
            .ToArray();

    private static readonly int ExpectedDeckSize = ValidCardTypes.Length * 4;
    
    [Fact] 
    public void Constructor_ShouldCreateDeckWithCorrectCardDistribution()
    {
        // Arrange & Act
        var deck = new CardDeck();
        var drawnCards = new List<CardDeck.Card>();

        // Draw all cards from the deck
        while (drawnCards.Count < ExpectedDeckSize)
        {
            drawnCards.Add(deck.DrawCard());
        }

        // Assert - verify 4 of each card type
        foreach (var cardType in ValidCardTypes)
        {
            Assert.Equal(4, drawnCards.Count(c => c == cardType));
        }
    }

    [Fact]
    public void DrawCard_ShouldReturnValidCardAndUpdateLastDrawn() 
    {
        // Arrange
        var deck = new CardDeck();

        // Act 
        var card = deck.DrawCard();

        // Assert
        Assert.NotEqual(CardDeck.Card.Undefined, card);
        Assert.Contains(card, ValidCardTypes);
        Assert.Equal(card, deck.LastDrawn);
    }

    [Fact]
    public void DrawCard_ShouldReturnValidCardsFromExpectedSet()
    {
        // Arrange
        var deck = new CardDeck();
        var drawnCards = new List<CardDeck.Card>();

        // Act - Draw several cards
        for (int i = 0; i < 10; i++)
        {
            drawnCards.Add(deck.DrawCard());
        }

        // Assert - All cards should be valid
        Assert.All(drawnCards, card => Assert.Contains(card, ValidCardTypes));
        Assert.All(drawnCards, card => Assert.NotEqual(CardDeck.Card.Undefined, card));
    }

    [Fact]
    public void DrawCard_ShouldAutoReshuffle_WhenDeckEmpty()
    {
        // Arrange
        var deck = new CardDeck();
        var firstRoundCards = new List<CardDeck.Card>();
        var secondRoundCards = new List<CardDeck.Card>();

        // Act - Draw full deck
        for (int i = 0; i < ExpectedDeckSize; i++) 
        {
            firstRoundCards.Add(deck.DrawCard());
        }

        // Draw some more after auto-reshuffle
        for (int i = 0; i < 10; i++)
        {
            secondRoundCards.Add(deck.DrawCard());
        }

        // Assert
        Assert.Equal(ExpectedDeckSize, firstRoundCards.Count);
        Assert.Equal(10, secondRoundCards.Count);
        Assert.All(secondRoundCards, card => Assert.NotEqual(CardDeck.Card.Undefined, card));
        Assert.All(secondRoundCards, card => Assert.Contains(card, ValidCardTypes));
    }

    [Fact]
    public void Shuffle_ShouldProduceValidRandomizedOrder()
    {
        // Arrange
        var deck = new CardDeck();
        var cards = new List<CardDeck.Card>();
        var cardCounts = new Dictionary<CardDeck.Card, int>();

        // Act - Draw multiple cards to check distribution
        for (int i = 0; i < 20; i++)
        {
            var card = deck.DrawCard();
            cards.Add(card);
            cardCounts[card] = cardCounts.GetValueOrDefault(card, 0) + 1;
        }

        // Assert - Should have some variety in drawn cards
        Assert.True(cardCounts.Count >= 2, "Should draw at least 2 different card types in 20 draws");
        Assert.All(cards, card => Assert.Contains(card, ValidCardTypes));
    }

    [Fact] 
    public void LastDrawn_ShouldTrackMostRecentCard()
    {
        // Arrange
        var deck = new CardDeck();

        // Act & Assert
        var card1 = deck.DrawCard();
        Assert.Equal(card1, deck.LastDrawn);

        var card2 = deck.DrawCard(); 
        Assert.Equal(card2, deck.LastDrawn);

        // LastDrawn should always reflect the most recent card
        var card3 = deck.DrawCard();
        Assert.Equal(card3, deck.LastDrawn);
        while (card3 == card1) card3 = deck.DrawCard(); // Draw until card 3 is not card 1
        Assert.NotEqual(card1, deck.LastDrawn); // Current should not equal first
    }

    [Fact]
    public void DrawCard_ShouldContinuouslyProvideValidCards()
    {
        // Arrange
        var deck = new CardDeck();
        var drawnCards = new List<CardDeck.Card>();

        // Act - Draw more cards than deck size to test reshuffling
        for (int i = 0; i < ExpectedDeckSize + 10; i++)
        {
            drawnCards.Add(deck.DrawCard());
        }

        // Assert
        Assert.Equal(ExpectedDeckSize + 10, drawnCards.Count);
        Assert.All(drawnCards, card => Assert.NotEqual(CardDeck.Card.Undefined, card));
        Assert.All(drawnCards, card => Assert.Contains(card, ValidCardTypes));
    }

    [Fact]
    public void Constructor_ShouldInitializeLastDrawnAsUndefined()
    {
        // Arrange & Act
        var deck = new CardDeck();

        // Assert
        Assert.Equal(CardDeck.Card.Undefined, deck.LastDrawn);
    }
}