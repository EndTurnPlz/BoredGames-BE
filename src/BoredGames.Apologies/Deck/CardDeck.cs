namespace BoredGames.Apologies.Deck;

public class CardDeck
{
    public CardTypes LastDrawn { get; private set; } = CardTypes.Undefined;
    private readonly List<CardTypes> _cards = new();

    public enum CardTypes
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
        _cards.Insert(0, CardTypes.Eleven);
        _cards.Insert(0, CardTypes.Two);
        _cards.Insert(0, CardTypes.Two);
        _cards.Insert(0, CardTypes.Two);
        _cards.Insert(0, CardTypes.Four);
        _cards.Insert(0, CardTypes.One);
        _cards.Insert(0, CardTypes.One);
        _cards.Insert(0, CardTypes.Four);
        _cards.Insert(0, CardTypes.Two);
    }

    public CardTypes DrawCard()
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
            _cards.AddRange(Enumerable.Repeat((CardTypes)i, 4));
        }
        
        var r = new Random();
        //Step 1: For each unshuffled item in the collection
        for (var n = _cards.Count - 1; n > 0; --n)
        {
            //Step 2: Randomly pick an item which has not been shuffled
            var k = r.Next(n + 1);
            //Step 3: Swap the selected item with the last "unstruck" letter in the collection
            (_cards[n], _cards[k]) = (_cards[k], _cards[n]);
        }
    }
}