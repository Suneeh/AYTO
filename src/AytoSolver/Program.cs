using System.Diagnostics;
using System.Text.Json;
using AytoSolver.Models;
using AytoSolver.Services;

namespace AytoSolver;

public class Program
{
    private static readonly string JsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "season_data.json");

    // In-memory state
    private static byte[][]? _permutations;
    private static bool[]? _eliminated;
    private static SeasonData? _seasonData;

    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        PrintBanner();

        if (!File.Exists(JsonPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ERROR: season_data.json not found at: {JsonPath}");
            Console.ResetColor();
            return;
        }

        _seasonData = LoadSeasonData();
        Console.WriteLine($"  Season {_seasonData.Season}: {_seasonData.Men.Count} men, {_seasonData.Women.Count} women");
        Console.WriteLine($"  {_seasonData.MatchboxResults.Count} matchbox results, {_seasonData.MatchingNights.Count} matching nights loaded");
        Console.WriteLine();

        while (true)
        {
            PrintMenu();
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    Generate();
                    break;
                case "2":
                    ApplyFilters();
                    break;
                case "3":
                    ShowProbabilities();
                    break;
                case "4":
                    ShowStatus();
                    break;
                case "5":
                    RunFullPipeline();
                    break;
                case "0":
                    Console.WriteLine("  Bye! 👋");
                    return;
                default:
                    Console.WriteLine("  Invalid option, please try again.");
                    break;
            }
        }
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ╔═══════════════════════════════════════════════╗");
        Console.WriteLine("  ║       ARE YOU THE ONE? — SOLVER 2026         ║");
        Console.WriteLine("  ║      10 Men × 11 Women × 39.9M Configs      ║");
        Console.WriteLine("  ╚═══════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void PrintMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  ┌─────────────────────────────────┐");
        Console.WriteLine("  │  1 │ Generate Permutations      │");
        Console.WriteLine("  │  2 │ Apply Filters              │");
        Console.WriteLine("  │  3 │ Show Probabilities         │");
        Console.WriteLine("  │  4 │ Show Status                │");
        Console.WriteLine("  │  5 │ Full Pipeline (1 → 2 → 3) │");
        Console.WriteLine("  │  0 │ Exit                       │");
        Console.WriteLine("  └─────────────────────────────────┘");
        Console.ResetColor();
        Console.Write("  > ");
    }

    private static SeasonData LoadSeasonData()
    {
        var json = File.ReadAllText(JsonPath);
        return JsonSerializer.Deserialize<SeasonData>(json)
               ?? throw new InvalidOperationException("Failed to deserialize season_data.json");
    }

    private static void Generate()
    {
        Console.WriteLine();
        Console.WriteLine("  🔄 Generating all 39,916,800 permutations in memory...");
        Console.WriteLine("  (11 women × 11 slots = 11! permutations, ~440 MB RAM)");
        Console.WriteLine();

        var sw = Stopwatch.StartNew();

        var progress = new Progress<long>(count =>
        {
            var pct = (double)count / 39_916_800 * 100;
            Console.Write($"\r  Progress: {count:N0} / 39,916,800 ({pct:F1}%)  [{sw.Elapsed:mm\\:ss}]  ");
        });

        _permutations = PermutationGenerator.GenerateAll(11, progress);
        _eliminated = new bool[_permutations.Length];

        sw.Stop();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ Generated {_permutations.Length:N0} configurations in {sw.Elapsed:mm\\:ss}");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void ApplyFilters()
    {
        if (_permutations == null || _eliminated == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ❌ Generate permutations first (option 1).");
            Console.ResetColor();
            return;
        }

        var before = FilteringService.CountRemaining(_eliminated);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ═══ APPLYING FILTERS ═══");
        Console.ResetColor();
        Console.WriteLine($"  Configurations before: {before:N0}");
        Console.WriteLine();

        var sw = Stopwatch.StartNew();
        var filter = new FilteringService(_seasonData!);

        var progress = new Progress<string>(msg =>
        {
            Console.WriteLine($"  {msg}");
        });

        // Reset elimination mask to reapply from scratch
        Array.Fill(_eliminated, false);
        _eliminated = filter.ApplyAllFilters(_permutations, progress);

        sw.Stop();
        var after = FilteringService.CountRemaining(_eliminated);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✅ Filtering complete in {sw.Elapsed:mm\\:ss}");
        Console.WriteLine($"  Remaining: {after:N0} / {_permutations.Length:N0} ({(double)after / _permutations.Length * 100:F2}%)");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void ShowProbabilities()
    {
        if (_permutations == null || _eliminated == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ❌ Generate and filter first.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("  🔄 Calculating probabilities...");

        var sw = Stopwatch.StartNew();
        var probService = new ProbabilityService(_seasonData!);
        var (matrix, totalRemaining) = probService.CalculateProbabilities(_permutations, _eliminated);
        sw.Stop();

        Console.WriteLine($"  Calculated in {sw.Elapsed:mm\\:ss}");
        probService.PrintProbabilityMatrix(matrix, totalRemaining);
    }

    private static void ShowStatus()
    {
        if (_permutations == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ❌ No data loaded. Generate permutations first.");
            Console.ResetColor();
            return;
        }

        var remaining = FilteringService.CountRemaining(_eliminated!);
        var total = _permutations.Length;
        var eliminated = total - remaining;

        Console.WriteLine();
        Console.WriteLine($"  Total configurations:  {total:N0}");
        Console.WriteLine($"  Eliminated:            {eliminated:N0}");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  Remaining:             {remaining:N0} ({(double)remaining / total * 100:F2}%)");
        Console.ResetColor();
        Console.WriteLine($"  Memory usage:          ~{GC.GetTotalMemory(false) / 1024 / 1024} MB");
        Console.WriteLine();
    }

    private static void RunFullPipeline()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine("  ═══ FULL PIPELINE ═══");
        Console.ResetColor();

        var totalSw = Stopwatch.StartNew();

        Generate();
        ApplyFilters();
        ShowProbabilities();

        totalSw.Stop();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  🏁 Full pipeline completed in {totalSw.Elapsed:mm\\:ss}");
        Console.ResetColor();
        Console.WriteLine();
    }
}
