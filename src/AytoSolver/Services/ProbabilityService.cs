using System.Collections.Concurrent;
using AytoSolver.Models;

namespace AytoSolver.Services;

/// <summary>
/// Calculates and displays per-pair match probabilities based on remaining valid configurations.
/// Fully in-memory — iterates the byte[][] array with the bool[] elimination mask.
/// </summary>
public class ProbabilityService
{
    private readonly SeasonData _seasonData;

    public ProbabilityService(SeasonData seasonData)
    {
        _seasonData = seasonData;
    }

    /// <summary>
    /// Calculate the probability matrix from in-memory permutations + elimination mask.
    /// </summary>
    public (double[,] Matrix, long TotalRemaining) CalculateProbabilities(
        byte[][] permutations, bool[] eliminated, IProgress<long>? progress = null)
    {
        int numWomen = _seasonData.Women.Count; // 11
        int numSlots = numWomen;                // 11 (10 real men + Nobody)

        // Use thread-local counts for parallel aggregation
        var globalCounts = new long[numWomen, numSlots];
        long totalRemaining = 0;

        // Parallel aggregation with thread-local accumulators
        var lockObj = new object();

        Parallel.ForEach(
            Partitioner.Create(0, permutations.Length),
            () => (counts: new long[numWomen, numSlots], localTotal: 0L),
            (range, _, local) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    if (eliminated[i]) continue;

                    var config = permutations[i];
                    for (int w = 0; w < numWomen; w++)
                    {
                        local.counts[w, config[w]]++;
                    }
                    local.localTotal++;
                }
                return local;
            },
            local =>
            {
                lock (lockObj)
                {
                    for (int w = 0; w < numWomen; w++)
                    {
                        for (int m = 0; m < numSlots; m++)
                        {
                            globalCounts[w, m] += local.counts[w, m];
                        }
                    }
                    Interlocked.Add(ref totalRemaining, local.localTotal);
                }
            }
        );

        // Convert to percentages
        var matrix = new double[numWomen, numSlots];
        if (totalRemaining > 0)
        {
            for (int w = 0; w < numWomen; w++)
            {
                for (int m = 0; m < numSlots; m++)
                {
                    matrix[w, m] = (double)globalCounts[w, m] / totalRemaining * 100.0;
                }
            }
        }

        return (matrix, totalRemaining);
    }

    /// <summary>
    /// Print a nicely formatted probability matrix to the console.
    /// </summary>
    public void PrintProbabilityMatrix(double[,] matrix, long totalRemaining)
    {
        var women = _seasonData.Women;
        var menLabels = new List<string>(_seasonData.Men) { "Nobody" };

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  ═══ PROBABILITY MATRIX ═══  ({totalRemaining:N0} valid configurations remaining)");
        Console.ResetColor();
        Console.WriteLine();

        int nameWidth = Math.Max(women.Max(w => w.Length), 10) + 1;
        int colWidth = Math.Max(menLabels.Max(m => m.Length), 6) + 1;

        // Header row
        Console.Write(new string(' ', nameWidth + 2));
        foreach (var man in menLabels)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(man.PadLeft(colWidth));
        }
        Console.ResetColor();
        Console.WriteLine();

        // Separator
        Console.Write("  ");
        Console.Write(new string('─', nameWidth + colWidth * menLabels.Count));
        Console.WriteLine();

        // Data rows
        for (int w = 0; w < women.Count; w++)
        {
            Console.Write("  ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(women[w].PadRight(nameWidth));
            Console.ResetColor();

            for (int m = 0; m < menLabels.Count; m++)
            {
                double prob = matrix[w, m];
                string display;

                if (prob == 0.0)
                    display = "─";
                else if (prob >= 99.9)
                    display = "★★★";
                else
                    display = $"{prob:F1}%";

                if (prob >= 99.9)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (prob >= 20.0)
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (prob == 0.0)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                else
                    Console.ResetColor();

                Console.Write(display.PadLeft(colWidth));
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        PrintTopCandidates(matrix, women, menLabels);
    }

    private void PrintTopCandidates(double[,] matrix, List<string> women, List<string> menLabels)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ═══ TOP 20 MOST LIKELY PAIRS ═══");
        Console.ResetColor();
        Console.WriteLine();

        var pairs = new List<(string Woman, string Man, double Prob)>();

        for (int w = 0; w < women.Count; w++)
        {
            for (int m = 0; m < menLabels.Count; m++)
            {
                if (matrix[w, m] > 0.0 && m < 10) // Skip "Nobody"
                {
                    pairs.Add((women[w], menLabels[m], matrix[w, m]));
                }
            }
        }

        foreach (var (woman, man, prob) in pairs.OrderByDescending(p => p.Prob).Take(20))
        {
            var barLen = Math.Max(1, (int)(prob / 2.5));
            var bar = new string('█', barLen);

            if (prob >= 99.9)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (prob >= 20.0)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ResetColor();

            Console.WriteLine($"  {woman,-12} × {man,-12} {prob,6:F1}%  {bar}");
            Console.ResetColor();
        }

        Console.WriteLine();

        // Summary
        var confirmed = pairs.Count(p => p.Prob >= 99.9);
        var eliminatedPairs = 0;
        for (int w = 0; w < women.Count; w++)
            for (int m = 0; m < 10; m++)
                if (matrix[w, m] == 0.0)
                    eliminatedPairs++;

        if (confirmed > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ★ {confirmed} confirmed Perfect Match(es)!");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  ✗ {eliminatedPairs} pair(s) eliminated (0% probability)");
        Console.ResetColor();
        Console.WriteLine();
    }
}
