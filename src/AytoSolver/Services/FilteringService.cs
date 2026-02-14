using AytoSolver.Models;

namespace AytoSolver.Services;

/// <summary>
/// Applies Matchbox and Matching Night constraints to eliminate impossible configurations.
/// Works entirely in-memory using a bool[] elimination mask for maximum speed.
/// </summary>
public class FilteringService
{
    private readonly SeasonData _seasonData;
    private readonly Dictionary<string, int> _womanIndex;
    private readonly Dictionary<string, int> _manIndex;

    private const int NobodyIndex = 10;

    public FilteringService(SeasonData seasonData)
    {
        _seasonData = seasonData;

        _womanIndex = new Dictionary<string, int>();
        for (int i = 0; i < seasonData.Women.Count; i++)
            _womanIndex[seasonData.Women[i]] = i;

        _manIndex = new Dictionary<string, int>();
        for (int i = 0; i < seasonData.Men.Count; i++)
            _manIndex[seasonData.Men[i]] = i;
        _manIndex["Nobody"] = NobodyIndex;
    }

    /// <summary>
    /// Apply all filters (matchbox + matching nights) to the permutation universe.
    /// Returns the elimination mask: eliminated[i] = true means permutations[i] is invalid.
    /// </summary>
    public bool[] ApplyAllFilters(byte[][] permutations, IProgress<string>? progress = null)
    {
        var eliminated = new bool[permutations.Length];

        // 1. Apply Matchbox filters
        progress?.Report("Applying Matchbox filters...");
        ApplyMatchboxFilters(permutations, eliminated, progress);

        // 2. Apply Matching Night filters
        progress?.Report("Applying Matching Night filters...");
        ApplyMatchingNightFilters(permutations, eliminated, progress);

        return eliminated;
    }

    /// <summary>
    /// Matchbox filter: for each confirmed No Match or Perfect Match,
    /// eliminate all configurations that contradict the result.
    /// </summary>
    private void ApplyMatchboxFilters(byte[][] permutations, bool[] eliminated, IProgress<string>? progress)
    {
        foreach (var mb in _seasonData.MatchboxResults)
        {
            if (!_womanIndex.TryGetValue(mb.Woman, out int wi) || !_manIndex.TryGetValue(mb.Man, out int mi))
            {
                Console.WriteLine($"  ⚠ Skipping matchbox: unknown name {mb.Woman} or {mb.Man}");
                continue;
            }

            long count = 0;
            byte manByte = (byte)mi;

            if (mb.Result == "NoMatch")
            {
                // Eliminate all where config[wi] == mi
                Parallel.For(0, permutations.Length, i =>
                {
                    if (!eliminated[i] && permutations[i][wi] == manByte)
                    {
                        eliminated[i] = true;
                        Interlocked.Increment(ref count);
                    }
                });
            }
            else if (mb.Result == "PerfectMatch")
            {
                // Eliminate all where config[wi] != mi
                Parallel.For(0, permutations.Length, i =>
                {
                    if (!eliminated[i] && permutations[i][wi] != manByte)
                    {
                        eliminated[i] = true;
                        Interlocked.Increment(ref count);
                    }
                });
            }
            else
            {
                progress?.Report($"  Matchbox {mb.Woman} × {mb.Man}: SOLD (no info, skipped)");
                continue;
            }

            progress?.Report($"  Matchbox {mb.Woman} × {mb.Man} ({mb.Result}): eliminated {count:N0}");
        }
    }

    /// <summary>
    /// Matching Night filter: for each night, count how many seated pairs match
    /// the configuration. If count != lights, eliminate.
    /// </summary>
    private void ApplyMatchingNightFilters(byte[][] permutations, bool[] eliminated, IProgress<string>? progress)
    {
        foreach (var mn in _seasonData.MatchingNights)
        {
            // Pre-compute the night's pairings as arrays for fast access
            var womanIndices = new int[mn.Pairs.Count];
            var manIndices = new byte[mn.Pairs.Count];

            for (int i = 0; i < mn.Pairs.Count; i++)
            {
                womanIndices[i] = _womanIndex[mn.Pairs[i].Woman];
                manIndices[i] = (byte)_manIndex[mn.Pairs[i].Man];
            }

            int expectedLights = mn.Lights;
            int pairCount = mn.Pairs.Count;
            long count = 0;

            Parallel.For(0, permutations.Length, i =>
            {
                if (eliminated[i]) return;

                var config = permutations[i];
                int matches = 0;

                for (int p = 0; p < pairCount; p++)
                {
                    if (config[womanIndices[p]] == manIndices[p])
                        matches++;
                }

                if (matches != expectedLights)
                {
                    eliminated[i] = true;
                    Interlocked.Increment(ref count);
                }
            });

            progress?.Report($"  MN #{mn.NightNumber} ({mn.Lights} lights, {mn.SitsOut} sat out): eliminated {count:N0}");
        }
    }

    /// <summary>
    /// Count remaining non-eliminated configurations.
    /// </summary>
    public static long CountRemaining(bool[] eliminated)
    {
        long count = 0;
        for (int i = 0; i < eliminated.Length; i++)
        {
            if (!eliminated[i]) count++;
        }
        return count;
    }
}
