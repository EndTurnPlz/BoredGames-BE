using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

using BoredGames.Apologies.Deck;
using Xunit;

namespace BoredGames.Apologies.Tests.Deck;

[TestSubject(typeof(CardDeck))]
public class CardDeckTest
{
    [Fact] 
    public void Constructor_ShouldCreateDeckWithCorrectCardDistribution()
    {
        // Arrange & Act
        var deck = new CardDeck();
        var drawnCards = new List<CardDeck.CardTypes>();

        // Draw all cards from the deck
        while (drawnCards.Count < 44)
        {
            drawnCards.Add(deck.DrawCard());
        }

        // Assert - verify 4 of each card type
        foreach (var cardType in Enum.GetValues<CardDeck.CardTypes>())
        {
            if (cardType != CardDeck.CardTypes.Undefined)
            {
                Assert.Equal(4, drawnCards.Count(c => c == cardType));
            }
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
        Assert.NotEqual(CardDeck.CardTypes.Undefined, card);
        Assert.Equal(card, deck.LastDrawn);
    }

    [Fact]
    public void DrawCard_ShouldReturnDifferentConsecutiveCards()
    {
        // Arrange
        var deck = new CardDeck();
        var drawnCards = new HashSet<CardDeck.CardTypes>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            drawnCards.Add(deck.DrawCard());
        }

        // Assert 
        Assert.InRange(drawnCards.Count, 2, 5); // At least 1 card was different
    }

    [Fact]
    public void DrawCard_ShouldAutoReshuffle_WhenDeckEmpty()
    {
        // Arrange
        var deck = new CardDeck();
        var firstRoundCards = new List<CardDeck.CardTypes>();
        var secondRoundCards = new List<CardDeck.CardTypes>();

        // Act - Draw full deck
        for (int i = 0; i < 44; i++) 
        {
            firstRoundCards.Add(deck.DrawCard());
        }

        // Draw some more after auto-reshuffle
        for (int i = 0; i < 10; i++)
        {
            secondRoundCards.Add(deck.DrawCard());
        }

        // Assert
        Assert.Equal(44, firstRoundCards.Count);
        Assert.Equal(10, secondRoundCards.Count);
        Assert.All(secondRoundCards, card => Assert.NotEqual(CardDeck.CardTypes.Undefined, card));
    }

    [Fact]
    public void Shuffle_ShouldRandomizeCardOrder()
    {
        // Arrange
        var deck1 = new CardDeck();
        var deck2 = new CardDeck();
        var cards1 = new List<CardDeck.CardTypes>();
        var cards2 = new List<CardDeck.CardTypes>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            cards1.Add(deck1.DrawCard());
            cards2.Add(deck2.DrawCard());
        }

        // Assert
        Assert.False(cards1.SequenceEqual(cards2)); // Very unlikely to get same order
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
        Assert.NotEqual(card1, card2);
    }
}