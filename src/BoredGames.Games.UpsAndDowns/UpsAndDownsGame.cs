using System.Collections.Immutable;
using BoredGames.Core;
using BoredGames.Core.Game;
using BoredGames.Core.Game.Attributes;
using BoredGames.Core.Game.Components.Dice;
using BoredGames.Games.UpsAndDowns.Board;
using BoredGames.Games.UpsAndDowns.Models;
using JetBrains.Annotations;

namespace BoredGames.Games.UpsAndDowns;

[BoredGame("UpsAndDowns")]
[GamePlayerCount(minPlayers: 2, maxPlayers: 8)]
public class UpsAndDownsGame : GameBase {
    private readonly StandardDie _die = new();
    private readonly GameBoard _gameBoard;

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum State
    {
        P1Turn, P2Turn,
        P3Turn, P4Turn,
        P5Turn, P6Turn,
        P7Turn, P8Turn,
        End
    }

    public UpsAndDownsGame(UpsAndDownsGameConfig _, ImmutableList<Player> playerList) : base(playerList)
    {
        _gameBoard = GameBoard.CreateWithDefaultWarpTiles(playerList.Count);
    }
    
    private State GameState { get; set; } = State.P1Turn;

    public override bool HasEnded() => GameState is State.End;

    public override IGameSnapshot GetSnapshot(Player player)
    {
        var turnOrder = Players.Select(p => p.Username).ToArray();
        var boardLayout = _gameBoard.WarpTiles
            .Select(pair => new GenericModels.WarpTileInfo(pair.Key, pair.Value));

        return new UpsAndDownsSnapshot
        {
            TurnOrder = turnOrder,
            GameState = GameState,
            PlayerLocations = _gameBoard.PlayerPositions,
            BoardLayout = boardLayout,
            LastDieRoll = _die.LastRollValue
        };
    }

    [GameAction("move")]
    private void PlayerMoveAction(Player player)
    {
        var playerIndex = Players.IndexOf(player);
        if (playerIndex != (int)GameState) throw new InvalidPlayerException();
        
        var rollValue = _die.Roll();
        _gameBoard.MovePlayer(playerIndex, rollValue);
        AdvanceGameState();
    }

    private void AdvanceGameState()
    {
        if (GameState is State.End) return;
        
        var nextGameState = (State)(((int)GameState + 1) % Players.Count);

        if (_gameBoard.IsPlayerOnEnd) {
            nextGameState = State.End;
        }
        
        GameState = nextGameState;
    }
}