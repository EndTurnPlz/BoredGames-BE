// using JetBrains.Annotations;
//
// namespace BoredGames.Core.Game.Components.Dice;
//
// public class DiceGroup
// {
//     public class RollResult(int value, IReadOnlyDictionary<int, int> rolls);
//     
//     private readonly IEnumerable<IDie> _dice;
//     private RollResult LastRoll { get; set; } = new (0, new Dictionary<int, int>());
//     
//     private DiceGroup(IEnumerable<IDie> dice)
//     {
//         _dice = dice;
//     }
//     
//     [Pure]
//     public RollResult Roll()
//     {
//         var value = 0;
//         Dictionary<int, int> rolls = new();
//         foreach (var die in _dice) {
//             var roll = die.Roll();
//             value += roll;
//             rolls[roll] = rolls.GetValueOrDefault(roll, 0) + 1;
//         }
//         
//         LastRoll = new RollResult(value, rolls);
//         return LastRoll;
//     }
//     
//     public static DiceGroup CreateStandardDiceGroup(int count)
//     {
//         return new DiceGroup(Enumerable.Repeat(new StandardDie(), count));
//     }
// }