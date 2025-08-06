namespace BoredGames.Games.Apologies.Deck;

public class CardDeck
{
    public Card LastDrawn { get; private set; } = Card.Undefined;
    private readonly List<Card> _cards = [];

    public enum Card
    {
        Apologies = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Seven = 7,
        Eight = 8,
        Ten = 10,
        Eleven = 11,
        Twelve = 12,
        Undefined = -1
    }

    public CardDeck()
    {
        ResetAndShuffle();
    }

    public Card DrawCard()
    {
        LastDrawn = _cards.First();
        _cards.RemoveAt(0);

        if (_cards.Count == 0) ResetAndShuffle();

        return LastDrawn;
    }

    private void ResetAndShuffle()
    {
        // Reset
        for (var i = 0; i <= 12; i++)
        {
            if (i is 6 or 9) continue;
            _cards.AddRange(Enumerable.Repeat((Card)i, 4));
        }
        
        var r = new Random();
        for (var n = _cards.Count - 1; n > 0; --n)
        {
            var k = r.Next(n + 1);
            (_cards[n], _cards[k]) = (_cards[k], _cards[n]);
        }
    }
}