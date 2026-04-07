namespace DataQueryExplorer.Console.UI;

/// <summary>
/// Displays the strategy selection menu and returns the user's choice.
/// </summary>
public sealed class ConsoleMenu
{
    private static readonly (QueryStrategyType Type, string Description)[] Options =
    {
        (QueryStrategyType.SingleContainerQuery,        "1. Single container query (with optional @param Excel input)"),
        (QueryStrategyType.TwoLevelJoinAllResults,      "2. Two-level join — all results"),
        (QueryStrategyType.TwoLevelJoinOrphansOnly,     "3. Two-level join — orphans only (no child found)"),
        (QueryStrategyType.TwoLevelJoinFindDuplicates,  "4. Two-level join — find duplicate child records"),
        (QueryStrategyType.ThreeLevelJoinAllResults,    "5. Three-level join — all results"),
        (QueryStrategyType.ThreeLevelJoinInnerMatchOnly,"6. Three-level join — inner match only (all three levels found)"),
    };

    public QueryStrategyType SelectStrategy()
    {
        while (true)
        {
            System.Console.WriteLine("\n=== Select Query Type ===");
            foreach ((QueryStrategyType _, string desc) in Options)
                System.Console.WriteLine($"  {desc}");

            System.Console.Write("\nEnter option number: ");
            string? input = System.Console.ReadLine();

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= Options.Length)
                return Options[choice - 1].Type;

            System.Console.WriteLine("Invalid selection. Please enter a number between 1 and " + Options.Length);
        }
    }
}
