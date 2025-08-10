using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Game.Attributes;
using BoredGames.Games.Warlocks.Deck;
using BoredGames.Games.Warlocks.Models;

namespace BoredGames.Games.Warlocks;

[BoredGame("Warlocks")]
[GamePlayerCount(3, 6)]
public partial class WarlocksGame : GameBase
{
    private readonly WarlocksGameConfig _config;
    private State _state;
    private readonly WarlocksDeck _deck = new();
    
    // Round info
    private int _currentRoundNum;
    private WarlocksDeck.Suit _currentTrumpSuit = WarlocksDeck.Suit.None;
    private readonly int[] _currentPlayerBids;
    private readonly List<WarlocksDeck.Card>[] _currentPlayerHands;
    private readonly int[] _playerPoints;
    private readonly int[] _currentTricksWon;
    
    // Trick info
    private int _currentTrickNum;
    private GenericModels.TrickResult? _lastTrickResult;
    
    public WarlocksGame(WarlocksGameConfig config , ImmutableList<Player> players) : base(config, players)
    {
        _config = config;
        _playerPoints = new int[Players.Count];
        _currentPlayerBids = new int[Players.Count];
        _currentTricksWon = new int[Players.Count];
        _currentPlayerHands = Enumerable.Repeat(new List<WarlocksDeck.Card>(), Players.Count).ToArray();
        _state = new BidState(this);
        _state.Enter();
    }
    
    public override bool HasEnded() => _state is EndState;
    
    public override IGameSnapshot GetSnapshot(Player player)
    {
        var playerIndex = Players.IndexOf(player);
        var turnOrder = Players.Select(p => p.Username).ToArray();
        var thisPlayerBid = _currentPlayerBids[playerIndex];
        var thisPlayerHand = _currentPlayerHands[playerIndex];
        
        return _state switch
        {
            BidState => new WarlocksBidSnapshot
            {
                TurnOrder = turnOrder,
                LastTrickResult = _lastTrickResult,
                TrumpSuite = _currentTrumpSuit,
                ThisPlayerHand = thisPlayerHand,
                ThisPlayerBid = thisPlayerBid,
                PlayerPoints = _playerPoints,
                GameState = _state.Name,
                RoundNumber = _currentRoundNum,
            },
            PlayTrickState playTrickState => new WarlocksPlayTrickSnapshot
            {
                TurnOrder = turnOrder,
                LastTrickResult = _lastTrickResult,
                TrumpSuite = _currentTrumpSuit,
                ThisPlayerHand = thisPlayerHand.Select(c => (c, playTrickState.IsCardValid(c, playerIndex))),
                PlayerPoints = _playerPoints,
                GameState = _state.Name,
                RoundNumber = _currentRoundNum,
                PlayerBids = _currentPlayerBids,
                CurrentTrick = playTrickState.GetTrickInfo()
            },
            EndState => new WarlocksEndSnapshot
            {
                TurnOrder = turnOrder,
                LastTrickResult = _lastTrickResult,
                PlayerPoints = _playerPoints,
                GameState = _state.Name,
            },
            _ => throw new Exception("Invalid state")
        };
    }
    
    [GameAction("bid")]
    private void BidAction(Player player, ActionArgs.BidArgs req)
    {
        if (req.Bid < 0 || req.Bid > _currentRoundNum) throw new BadActionArgsException();
        var playerIndex = Players.IndexOf(player);
        if (playerIndex == -1) {
            throw new InvalidPlayerException();
        }
        
        if (_state is not BidState bidState) throw new InvalidActionException();
        bidState.SetBid(playerIndex, req.Bid);
    }

    [GameAction("card")]
    private void PlayCardAction(Player player, ActionArgs.PlayCardArgs req)
    {
        if (_state is not PlayTrickState playTrickState) throw new InvalidActionException();
        var playerIndex = Players.IndexOf(player);
        if (playerIndex != playTrickState.CurrentPlayerIndex) throw new InvalidPlayerException();

        if (_currentPlayerHands[playerIndex].Contains(req.Card) 
            || !playTrickState.IsCardValid(req.Card, playerIndex)) throw new InvalidMoveException();
        
        playTrickState.PlayCard(req.Card);
    }

    private void AdvanceState(State state)
    {
        _state.Exit();
        _state = state;
        _state.Enter();
    }
}