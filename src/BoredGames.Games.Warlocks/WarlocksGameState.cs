using System.Diagnostics.Contracts;
using BoredGames.Games.Warlocks.Deck;
using BoredGames.Games.Warlocks.Models;

namespace BoredGames.Games.Warlocks;

public partial class WarlocksGame
{
    private abstract class State(WarlocksGame game)
    {
        protected readonly WarlocksGame Game = game;
        public abstract string Name { get; }
        public virtual void Enter() { }
        public virtual void Exit() { }
    }
    
    private class BidState(WarlocksGame game) : State(game)
    {
        public override string Name => "Bid";
        
        public override void Enter()
        {
            Game._currentRoundNum++;
            
            Array.Fill(Game._currentPlayerBids, -1);
            Array.Fill(Game._currentTricksWon, 0);
            Game._currentTrickNum = 0;
            
            Game._deck.ResetAndShuffle();
            DealCards();
            Game._currentTrumpSuit = Game._deck.Peek()?.Suit ?? WarlocksDeck.Suit.None;
        }

        public void SetBid(int playerIndex, int bid)
        {
            Game._currentPlayerBids[playerIndex] = bid;
            if (Game._currentPlayerBids.All(x => x >= 0)) {
                Game.AdvanceState(new PlayTrickState(Game));
            }
        }
        
        private void DealCards()
        {
            for (var i = 0; i < Game._currentRoundNum; ++i) {
                for (var j = 0; j < Game.Players.Count; ++j ) {
                    Game._currentPlayerHands[j].Add(Game._deck.Draw());
                }
            }
        }
    }
    
    private class PlayTrickState(WarlocksGame game) : State(game)
    {
        private int TrickLeader { get; set; }
        public int CurrentPlayerIndex { get; private set; }
        private readonly List<WarlocksDeck.Card> _trickCards = [];
        private WarlocksDeck.Suit LeadSuit => _trickCards.FirstOrDefault()?.Suit ?? WarlocksDeck.Suit.None;

        public override string Name => "PlayTrick";

        public override void Enter()
        {
            Game._currentTrickNum++;
            
            if (Game._currentTrickNum is 1) {
                TrickLeader = (Game._currentRoundNum - 1) % Game.Players.Count;
            }
            else {
                TrickLeader = Game._lastTrickResult!.Winner;
            }
            
            CurrentPlayerIndex = TrickLeader;
        }

        public void PlayCard(WarlocksDeck.Card card)
        {
            _trickCards.Add(card);
            Game._currentPlayerHands[CurrentPlayerIndex].Remove(card);
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Game.Players.Count;
            
            // Check if the trick is over
            if (_trickCards.Count < Game.Players.Count) return;
            
            // The round is over
            if (Game._currentTrickNum == Game._currentRoundNum) {
                Game.AdvanceState(new RoundOverState(Game));
                return;
            }
            
            Game.AdvanceState(new PlayTrickState(Game));
        }
        
        public override void Exit()
        {
            var trickWinner = DetermineTrickWinner();
            Game._currentTricksWon[trickWinner]++;
            Game._lastTrickResult = new GenericModels.TrickResult
            {
                Num = Game._currentTrickNum,
                Leader = TrickLeader,
                Winner = trickWinner,
                Cards = _trickCards
            };
        }

        private int DetermineTrickWinner()
        {
            // Search for first warlock card
            var trickWinner = _trickCards.FindIndex(c => c.Rank is WarlocksDeck.Rank.Warlock);
            
            // Search for the highest trump suite card if no warlocks
            if (trickWinner == -1) {
                if (_trickCards.Exists(c => c.Suit == Game._currentTrumpSuit)) {
                    trickWinner = _trickCards
                        .Select((c, i) => (c, i))
                        .Where(p => p.c.Suit == Game._currentTrumpSuit)
                        .OrderByDescending(p => p.c)
                        .FirstOrDefault((c: _trickCards.First(), i: -1)).i;
                }
            }
            
            // If no cards have the trump check lead suite
            if (trickWinner == -1) {
                var leadSuit = _trickCards.FirstOrDefault(c => c.Suit == WarlocksDeck.Suit.None)?.Suit 
                               ?? WarlocksDeck.Suit.None;
                
                if (leadSuit != WarlocksDeck.Suit.None) {
                    trickWinner = _trickCards
                        .Select((c, i) => (c, i))
                        .Where(p => p.c.Suit == leadSuit)
                        .OrderByDescending(p => p.c)
                        .FirstOrDefault((c: _trickCards.First(), i: -1)).i;
                }
            }
            
            // All Jokers so the first player wins
            if (trickWinner == -1) {
                trickWinner = 0;
            }
            
            // use the trick leader as an offset to get the winner's index
            return (trickWinner + TrickLeader) % Game.Players.Count;
        }
        
        [Pure]
        public GenericModels.CurrentTrickInfo GetTrickInfo()
        {
            return new GenericModels.CurrentTrickInfo
            {
                TrickLeader = TrickLeader, 
                CurrentPlayerIndex = CurrentPlayerIndex, 
                LeadSuit = LeadSuit, 
                CardsPlayed = _trickCards
            };
        }
        
        [Pure]
        public bool IsCardValid(WarlocksDeck.Card card, int playerIndex)
        {
            if (card.Rank is WarlocksDeck.Rank.Warlock or WarlocksDeck.Rank.Joker) return true;
            if (LeadSuit == WarlocksDeck.Suit.None) return true;
            if (Game._currentPlayerHands[playerIndex].Exists(c => c.Suit == LeadSuit)) {
                return card.Suit == LeadSuit;
            }

            return true;
        }
    }

    private class RoundOverState(WarlocksGame game) : State(game)
    {
        public override string Name => "RoundOver";
        
        public override void Enter()
        {
            AdjustPlayerScores();
            
            // The Game is over
            if (Game._currentRoundNum == Game._config.NumRounds) {
                Game.AdvanceState(new EndState(Game));
                return;
            }
            Game.AdvanceState(new BidState(Game));
        }

        private void AdjustPlayerScores()
        {
            for (var i = 0; i < Game.Players.Count; i++ ) {
                int pointDiff;
                if (Game._currentTricksWon[i] == Game._currentPlayerBids[i]) {
                    pointDiff = 20 + 10 * Game._currentTricksWon[i];
                }
                else {
                    pointDiff = -10 * Math.Abs(Game._currentTricksWon[i] - Game._currentPlayerBids[i]);
                }
                Game._playerPoints[i] += pointDiff;
            }
        }
    }
    
    private class EndState(WarlocksGame game) : State(game)
    {
        public override string Name => "End";
    }
}