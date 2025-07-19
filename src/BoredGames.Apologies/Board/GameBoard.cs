using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using BoredGames.Apologies.Deck;
using BoredGames.Apologies.Models;

namespace BoredGames.Apologies.Board;

public class GameBoard
{
    private readonly StartTile[] _startTiles =
    [
        new("a_S", null!),
        new("b_S", null!),
        new("c_S", null!),
        new("d_S", null!)
    ];

    public BoardTile[][] PawnTiles { get; }
    
    private enum MoveEffect
    {
        Forward,
        Backward,
        ExitStart,
        Apologies,
        Swap,
        Split1,
        Split2,
        Split3,
        Split4,
        Split5,
        Split6,
        NMoveEffects
    }

    public GameBoard()
    {
        BuildGameBoard();

        PawnTiles =
        [
            Enumerable.Repeat<BoardTile>(_startTiles[0], 4).ToArray(),
            Enumerable.Repeat<BoardTile>(_startTiles[1], 4).ToArray(),
            Enumerable.Repeat<BoardTile>(_startTiles[2], 4).ToArray(),
            Enumerable.Repeat<BoardTile>(_startTiles[3], 4).ToArray()
        ];
    }

    public List<GenericComponents.Moveset> GetValidMovesForPlayer(int playerIndex, CardDeck.CardTypes card)
    {
        List<GenericComponents.Moveset> movesets = [];
        var checkedStartWlog = false;
        for (var p = 0; p < 4; p++)
        {
            var currentPawnTile = PawnTiles[playerIndex][p];

            // Pawns at the Home tile can't move
            if (currentPawnTile is HomeTile) continue;

            // Since a start tile is the only place where pawns
            // can share a location, it should be checked wlog.
            if (currentPawnTile is StartTile)
            {
                if (checkedStartWlog) continue;
                checkedStartWlog = true;
            }

            List<(BoardTile, MoveEffect)> validTiles =
            [
                ..FindTileForForwardMove(currentPawnTile, card, playerIndex)
                    .ConvertAll(tile => (tile, MoveEffect.Forward)),
                
                ..FindTileForBackwardMove(currentPawnTile, card, playerIndex)
                    .ConvertAll(tile => (tile, MoveEffect.Backward)),
                
                ..FindTileForExitStartMove(currentPawnTile, card, playerIndex)
                    .ConvertAll(tile => (tile, MoveEffect.ExitStart)),
                
                ..FindTilesForApologiesMove(currentPawnTile, card, playerIndex)
                    .ConvertAll(tile => (tile, MoveEffect.Apologies)),
                
                ..FindTilesForSwapMove(currentPawnTile, card, playerIndex)
                    .ConvertAll(tile => (tile, MoveEffect.Swap)),
                
                ..FindTilesForSplitMove(currentPawnTile, card, playerIndex)
                    .Select(pair => (pair.Item1, MoveEffect.Split1 + pair.Item2 - 1))
            ];

            var moveList = validTiles
                .GroupBy(x => x.Item1)
                .Select(x =>
                    new GenericComponents.MoveOpts(currentPawnTile.Name, x.Key.Name, x.Select(y => (int)y.Item2)))
                .ToImmutableList();

            if (moveList.Count == 0) continue;
            movesets.Add(new GenericComponents.Moveset(currentPawnTile.Name, moveList));
        }

        return movesets;
    }

    public bool TryExecuteSplitMove(GenericComponents.Move firstMove, GenericComponents.Move secondMove, int playerIndex)
    {
        var firstMoveEffect = (MoveEffect)firstMove.Effect;
        var secondMoveEffect = (MoveEffect)secondMove.Effect;

        if (firstMoveEffect is MoveEffect.NMoveEffects
            || secondMoveEffect is MoveEffect.NMoveEffects)
        {
            return false;
        }
        
        ImmutableList<MoveEffect> splitEffects =
        [
            MoveEffect.NMoveEffects,
            MoveEffect.Split1,
            MoveEffect.Split2,
            MoveEffect.Split3,
            MoveEffect.Split4,
            MoveEffect.Split5,
            MoveEffect.Split6
        ];
        
        // Make sure both effects have split effects 
        var firstMoveEffectIndex = splitEffects.IndexOf(firstMoveEffect);
        var secondMoveEffectIndex = splitEffects.IndexOf(secondMoveEffect);

        if (firstMoveEffectIndex == -1 || secondMoveEffectIndex == -1) return false;

        // Both move effects should have a sum of 7
        if (firstMoveEffectIndex + secondMoveEffectIndex != 7) return false;
        
        // Find both source tiles
        var firstPawnIndex = Array.FindIndex(PawnTiles[playerIndex], x => x.Name == firstMove.From);
        if (firstPawnIndex == -1) return false;
        ref var firstSourcePawn = ref PawnTiles[playerIndex][firstPawnIndex];
        var firstSourceTile = firstSourcePawn;
        
        var secondPawnIndex = Array.FindIndex(PawnTiles[playerIndex], x => x.Name == secondMove.From);
        if (secondPawnIndex == -1) return false;
        ref var secondSourcePawn = ref PawnTiles[playerIndex][secondPawnIndex];
        var secondSourceTile = secondSourcePawn;
        
        // Verify each move individually and get their destination tiles
        var firstDestTile = ValidateAndFindDestinationTile(firstSourceTile, firstMove, CardDeck.CardTypes.Seven, playerIndex);
        var secondDestTile = ValidateAndFindDestinationTile(secondSourceTile, secondMove, CardDeck.CardTypes.Seven, playerIndex);

        // Make sure both moves have distinct destinations (except for the home tile)
        if (firstDestTile is WalkableTile && secondDestTile is WalkableTile && firstMove.To == secondMove.To)
            return false;
        
        // Handle other pawns existing at the destination tiles.
        // It's ok if a destination is the other source since
        // they will get overwritten later anyway.
        ProcessDestinationTile(firstSourceTile, firstDestTile);
        ProcessDestinationTile(secondSourceTile, secondDestTile);
        
        // Move both pawns to the destination tiles
        firstSourcePawn = firstDestTile;
        secondSourcePawn = secondDestTile;
        return true;
    }

    public bool TryExecuteMovePawn(GenericComponents.Move move, CardDeck.CardTypes drawnCard, int playerIndex)
    {
        var isSwap = (MoveEffect)move.Effect == MoveEffect.Swap;
        
        // Find source tile
        var pawnIndex = Array.FindIndex(PawnTiles[playerIndex], x => x.Name == move.From);
        if (pawnIndex == -1) return false;
        ref var sourcePawn = ref PawnTiles[playerIndex][pawnIndex];
        var sourceTile = sourcePawn;
        
        // Verify the destination and handle if another pawn exists there
        var destTile = ValidateAndFindDestinationTile(sourceTile, move, drawnCard, playerIndex);
        ProcessDestinationTile(sourceTile, destTile, isSwap);
        
        // Move the pawn to the destination
        sourcePawn = destTile;
        return true;
    }
    
    public void ExecuteAnyAvailableSlides()
    {
        for (var playerIndex = 0; playerIndex < 4; playerIndex++)
        {
            for (var pawnIndex = 0; pawnIndex < 4; pawnIndex++)
            {
                ref var pawn = ref PawnTiles[playerIndex][pawnIndex];
                if (pawn is not SliderTile sliderTile) continue;
                if (sliderTile.PlayerSide == playerIndex) continue;
        
                // Get a list of all the tiles on the slider
                BasicTile? current = sliderTile;
                var namesOfTilesOnSlide = new HashSet<string>();
                while (current != sliderTile.TargetTile)
                {
                    current = current.EvaluateNextTile(playerIndex) as BasicTile;
                    if (current is null) throw new Exception("Server Error Board Generated Incorrectly");
                    namesOfTilesOnSlide.Add(current.Name);
                }
        
                // Move any pawns on a tile within the slider back to start
                for (var i = 0; i < 4; i++)
                {
                    for (var j = 0; j < 4; j++)
                    {
                        if (namesOfTilesOnSlide.Contains(PawnTiles[i][j].Name)) PawnTiles[i][j] = _startTiles[i];
                    }
                }
                
                // Move the pawn on the slider to the target tile
                pawn = sliderTile.TargetTile;
            }
        }
    }

    private BoardTile ValidateAndFindDestinationTile(BoardTile sourceTile, GenericComponents.Move move, 
        CardDeck.CardTypes drawnCard, int playerIndex)
    {

        // Using the correct effect check if the end tile exists
        var destTileCandidateList = (MoveEffect)move.Effect switch
        {
            MoveEffect.Forward => 
                FindTileForForwardMove(sourceTile, drawnCard, playerIndex),
            MoveEffect.Backward => 
                FindTileForBackwardMove(sourceTile, drawnCard, playerIndex),
            MoveEffect.ExitStart => 
                FindTileForExitStartMove(sourceTile, drawnCard, playerIndex),
            MoveEffect.Apologies => 
                FindTilesForApologiesMove(sourceTile, drawnCard, playerIndex),
            MoveEffect.Swap => 
                FindTilesForSwapMove(sourceTile, drawnCard, playerIndex),
            >= MoveEffect.Split1 and <= MoveEffect.Split6 =>
                FindTilesForSplitMove(sourceTile, drawnCard, playerIndex).Select( x => x.Item1),
            _ => 
                throw new Exception("Invalid move effect")
        };

        return destTileCandidateList.FirstOrDefault(x => x.Name == move.To) ?? throw new Exception("Invalid move");
    }
    
    // Check if any pawns occupy the destination tile. If so, handle them.
    private void ProcessDestinationTile(BoardTile sourceTile, BoardTile destTile, bool isSwap = false)
    {
        if (destTile is not WalkableTile) return;
        
        var destPlayerIndex = Array.FindIndex(PawnTiles, x => x.Any(y => y.Name == destTile.Name));
        if (destPlayerIndex == -1) return;
        
        var destPawnIndex = Array.FindIndex(PawnTiles[destPlayerIndex], x => x.Name == destTile.Name);
        ref var destPawn = ref PawnTiles[destPlayerIndex][destPawnIndex];
        
        destPawn = isSwap ? sourceTile : _startTiles[destPlayerIndex];
    }

    [Pure]
    private List<BoardTile> FindTileForForwardMove(BoardTile sourceTile, CardDeck.CardTypes card, int playerIndex)
    {
        if (card is CardDeck.CardTypes.Apologies or CardDeck.CardTypes.Four) return [];

        var current = sourceTile;
        for (var i = 0; i < (int)card; i++)
        {
            if (current is not WalkableTile currentWalkable) return [];
            current = currentWalkable.EvaluateNextTile(playerIndex);
        }
        
        return NoTeammateOnTargetTile(current, playerIndex) ? [current] : [];
    }

    [Pure]
    private List<BoardTile> FindTileForBackwardMove(BoardTile sourceTile, CardDeck.CardTypes card, int playerIndex)
    {
        if (card is not CardDeck.CardTypes.Ten and not CardDeck.CardTypes.Four) return [];

        var current = sourceTile;
        var dist = card is CardDeck.CardTypes.Four ? 4 : 1;
        for (var i = 0; i < dist; i++)
        {
            if (current is not WalkableTile currentWalkable) return [];
            current = currentWalkable.PrevTile;
        }

        return NoTeammateOnTargetTile(current, playerIndex) ? [current] : [];
    }

    [Pure]
    private List<BoardTile> FindTileForExitStartMove(BoardTile sourceTile, CardDeck.CardTypes card, int playerIndex)
    {
        if (card is not CardDeck.CardTypes.One and not CardDeck.CardTypes.Two) return [];
        if (sourceTile is not StartTile tile) return [];
        
        if (!NoTeammateOnTargetTile(tile.NextTile, playerIndex)) return [];
        return [tile.NextTile];
    }

    [Pure]
    private List<BoardTile> FindTilesForApologiesMove(BoardTile sourceTile, CardDeck.CardTypes card, int playerIndex)
    {
        if (sourceTile is not StartTile) return [];
        if (card is not CardDeck.CardTypes.Apologies) return [];

        List<BoardTile> targets = [];
        for (var p = 0; p < 4; p++)
        {
            if (p == playerIndex) continue;
            targets.AddRange(PawnTiles[p].Where(x => NoTeammateOnTargetTile(x, playerIndex) && x is BasicTile));
        }

        return targets;
    }

    [Pure]
    private List<BoardTile> FindTilesForSwapMove(BoardTile sourceTile, CardDeck.CardTypes card, int playerIndex)
    {
        if (sourceTile is not BasicTile) return [];
        if (card is not CardDeck.CardTypes.Eleven) return [];

        List<BoardTile> targets = [];
        for (var p = 0; p < 4; p++)
        {
            targets.AddRange(PawnTiles[p].Where(x => NoTeammateOnTargetTile(x, playerIndex) && x is BasicTile));
        }

        return targets;
    }

    [Pure]
    private List<(BoardTile, int)> FindTilesForSplitMove(BoardTile sourceTile, CardDeck.CardTypes card, int playerIndex)
    {
        if (card is not CardDeck.CardTypes.Seven) return [];

        // Make sure there are at least 2 pawns on the board
        var walkableTileCount = PawnTiles[playerIndex].Count(tile => tile is WalkableTile);
        if (walkableTileCount < 2) return [];
        
        // Get a set of possible split move distances by checking the other three pawns
        HashSet<int> possibleDistances = [];
        foreach (var pairPawnTile in PawnTiles[playerIndex])
        {
            if (pairPawnTile == sourceTile) continue;
            
            var pairCurrent = pairPawnTile;
            for (var p = 1; p < 7; p++)
            {
                if (pairCurrent is not WalkableTile pairCurrentWalkable) break;
                pairCurrent = pairCurrentWalkable.EvaluateNextTile(playerIndex);
                
                if (pairCurrent != sourceTile && !NoTeammateOnTargetTile(pairCurrent, playerIndex)) continue;
                possibleDistances.Add(7 - p);
            }
        }
        
        // Find the possible split moves based on the possible distances
        List<(BoardTile, int)> targets = [];
        var current = sourceTile;
        for (var p = 1; p < 7; p++)
        {
            if (current is not WalkableTile walkableTile) return targets;
            current = walkableTile.EvaluateNextTile(playerIndex);
            if (possibleDistances.Contains(p) && NoTeammateOnTargetTile(current, playerIndex)) targets.Add((current, p));
        }

        return targets;
    }

    [Pure]
    private bool NoTeammateOnTargetTile(BoardTile tile, int playerIndex)
    {
        if (tile is not WalkableTile) return true;
        return !Array.Exists(PawnTiles[playerIndex], x => x.Name == tile.Name);
    }

    // Helper that builds the game board
    private void BuildGameBoard()
    {
        var initTile = new BasicTile("temp", null!, null!);
        var prevTile = initTile;

        for (var i = 0; i < 4; i++)
        {
            
            SliderTile slideTile = null!;
            // Build row of board
            for (var j = 1; j <= 15; j++)
            {
                var currentTileName = $"{(char)('a' + i)}_{j}";
                var currentTile = new BasicTile(currentTileName, null!, prevTile);

                // Tile is a slider tile
                if (j is 1 or 9)
                {
                    slideTile = new SliderTile(currentTileName, null!, prevTile, null!, i);
                    currentTile = slideTile;
                }

                // Tile is a slide target tile
                if (j is 4 or 13)
                {
                    slideTile.TargetTile = currentTile;
                }

                // Safety zone junction
                if (j is 2)
                {
                    var safetyZoneBase = BuildSafetyZone(i);
                    currentTile = new JunctionTile(
                        currentTileName,
                        null!,
                        prevTile,
                        safetyZoneBase,
                        i
                    );
                    safetyZoneBase.PrevTile = currentTile;
                }

                // Attach start tiles
                if (j is 4)
                {
                    _startTiles[i].NextTile = currentTile;
                }

                prevTile.NextTile = currentTile;
                prevTile = currentTile;
            }
        }

        // Connect ends of board
        var firstTile = initTile.EvaluateNextTile(0);
        (firstTile as BasicTile)!.PrevTile = prevTile;
        prevTile.NextTile = firstTile;
    }

    // Helper that creates a safety zone 
    private static SafetyZoneTile BuildSafetyZone(int playerIndex)
    {
        var baseTile = new SafetyZoneTile("temp", null!, null!);
        var prevTile = baseTile;
        for (var i = 1; i <= 5; i++)
        {
            var nextTileString = $"{(char)('a' + playerIndex)}_s{i}";
            var nextTile = new SafetyZoneTile(nextTileString, null!, prevTile);
            prevTile.NextTile = nextTile;
            prevTile = nextTile;
        }

        prevTile.NextTile = new HomeTile($"{(char)('a' + playerIndex)}_H");
        return (baseTile.EvaluateNextTile(playerIndex) as SafetyZoneTile)!;
    }
}