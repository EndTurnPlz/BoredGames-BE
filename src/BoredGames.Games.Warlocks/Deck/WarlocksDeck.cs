using JetBrains.Annotations;

namespace BoredGames.Games.Warlocks.Deck;

public class WarlocksDeck
{

    private List<Card> _cards = [];
    
    public enum Suit
    {
        Spades, Hearts,
        Diamonds, Clubs,
        None
    }

    public enum Rank
    {
        Joker, 
        Two, Three, Four, Five, 
        Six, Seven, Eight, Nine, Ten, 
        Jack, Queen, King, Ace, 
        Warlock
    }
    
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Card(Suit suit, Rank rank) : IComparable<Card>
    {
        public Suit Suit { get;} = suit;
        public Rank Rank { get;} = rank;
        
        private bool Equals(Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            
            return Equals((Card)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Suit, (int)Rank);
        }
        
        public int CompareTo(Card? other)
        {
            return other is not null ? Rank.CompareTo(other.Rank) : 1;
        }

        public static bool operator ==(Card a, Card? b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(Card a, Card b)
        {
            return a.CompareTo(b) != 0;
        }

        public static bool operator >(Card a, Card b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator >=(Card a, Card b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <(Card a, Card b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(Card a, Card b)
        {
            return a.CompareTo(b) <= 0;
        }
    }

    public WarlocksDeck()
    {
        ResetAndShuffle();
    }

    public Card Draw()
    {
        var card = _cards.First();
        _cards.RemoveAt(0);
        return card;
    }

    public Card? Peek()
    {
        return _cards.FirstOrDefault();
    }

    private void ResetAndShuffle()
    {
        // Add default cards
        foreach (var suit in Enum.GetValues<Suit>())
        {
            if (suit == Suit.None) continue;
            foreach (var rank in Enum.GetValues<Rank>())
            {
                if (rank == Rank.Warlock || rank == Rank.Joker) continue;
                _cards.Add(new Card(suit, rank));
            }
        }
        // Add warlocks and jokers
        _cards.AddRange(Enumerable.Repeat(new Card(Suit.None, Rank.Joker), 4));
        _cards.AddRange(Enumerable.Repeat(new Card(Suit.None, Rank.Warlock), 4));
        
        // Shuffle
        var random = new Random();
        for (var n = _cards.Count - 1; n > 0; --n)
        {
            var k = random.Next(n + 1);
            (_cards[n], _cards[k]) = (_cards[k], _cards[n]);
        }
    }
}