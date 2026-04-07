namespace DataQueryExplorer.Console.UI;

/// <summary>
/// Arrow-key list selector reusable for databases, containers, or any string list.
/// Press Up/Down to move, Enter to confirm, T to switch to text input, Esc to exit.
/// </summary>
public sealed class ConsoleSelectorUI
{
    private readonly IApplicationLogger _logger;

    public ConsoleSelectorUI(IApplicationLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Presents an interactive arrow-key menu and returns the selected item.
    /// If <paramref name="items"/> contains only one entry it is auto-selected silently.
    /// </summary>
    public string Select(IReadOnlyList<string> items, string title)
    {
        if (items is null || items.Count == 0)
            throw new ArgumentException("No items available for selection.", nameof(items));

        if (items.Count == 1)
        {
            _logger.LogToConsole($"Auto-selected: {items[0]}");
            return items[0];
        }

        int selectedIndex = 0;
        bool confirmed = false;
        System.Console.CursorVisible = false;

        try
        {
            do
            {
                System.Console.Clear();
                System.Console.WriteLine($"=== {title} ===");
                System.Console.WriteLine("Up/Down: navigate   Enter: confirm   T: text input   Esc: exit\n");

                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        System.Console.BackgroundColor = ConsoleColor.Gray;
                        System.Console.ForegroundColor = ConsoleColor.Black;
                        System.Console.WriteLine($" → {items[i]} ");
                        System.Console.ResetColor();
                    }
                    else
                    {
                        System.Console.WriteLine($"   {items[i]}");
                    }
                }

                System.Console.WriteLine($"\nTotal: {items.Count}");

                ConsoleKeyInfo keyInfo = System.Console.ReadKey(true);
                ConsoleKey key = keyInfo.Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = selectedIndex > 0 ? selectedIndex - 1 : items.Count - 1;
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = selectedIndex < items.Count - 1 ? selectedIndex + 1 : 0;
                        break;
                    case ConsoleKey.Enter:
                        confirmed = true;
                        break;
                    case ConsoleKey.T:
                        System.Console.CursorVisible = true;
                        return SelectByTextInput(items, title);
                    case ConsoleKey.Escape:
                        System.Console.CursorVisible = true;
                        throw new OperationCanceledException("User cancelled selection.");
                }
            } while (!confirmed);

            System.Console.Clear();
            _logger.LogToConsole($"Selected: {items[selectedIndex]}");
            return items[selectedIndex];
        }
        finally
        {
            System.Console.CursorVisible = true;
        }
    }

    private string SelectByTextInput(IReadOnlyList<string> items, string title)
    {
        System.Console.Clear();
        System.Console.WriteLine($"=== Text Input — {title} ===");
        for (int i = 0; i < items.Count; i++)
            System.Console.WriteLine($"  {i + 1}. {items[i]}");

        System.Console.WriteLine("\nType the name or number, or press Enter to return to arrow selection.");

        while (true)
        {
            System.Console.Write("\nYour choice: ");
            string? input = System.Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                return Select(items, title);

            if (int.TryParse(input, out int num) && num >= 1 && num <= items.Count)
                return items[num - 1];

            var exact = items.FirstOrDefault(c =>
                string.Equals(c, input, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;

            var suggestions = items
                .Where(c => c.Contains(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (suggestions.Count > 0)
            {
                System.Console.WriteLine("Did you mean:");
                suggestions.ForEach(s => System.Console.WriteLine($"  {s}"));
            }
            else
            {
                System.Console.WriteLine($"'{input}' not found. Try again.");
            }
        }
    }
}
