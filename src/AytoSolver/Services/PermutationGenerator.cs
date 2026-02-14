namespace AytoSolver.Services;

/// <summary>
/// Generates all 11! = 39,916,800 permutations of {0..10} in memory.
/// Each permutation represents a possible assignment of 11 women to 11 men (10 real + 1 dummy "Nobody").
/// Uses Heap's algorithm for efficient iterative generation.
/// </summary>
public class PermutationGenerator
{
    /// <summary>
    /// Generate all N! permutations of {0..N-1} in memory.
    /// For N=11: exactly 39,916,800 permutations, ~440MB total.
    /// </summary>
    public static byte[][] GenerateAll(int n = 11, IProgress<long>? progress = null)
    {
        long factorial = 1;
        for (int i = 2; i <= n; i++) factorial *= i;

        var results = new byte[factorial][];
        var perm = new byte[n];
        for (byte i = 0; i < n; i++) perm[i] = i;

        var c = new int[n];
        long index = 0;

        // First permutation
        results[index++] = (byte[])perm.Clone();

        int idx = 0;
        while (idx < n)
        {
            if (c[idx] < idx)
            {
                if (idx % 2 == 0)
                    (perm[0], perm[idx]) = (perm[idx], perm[0]);
                else
                    (perm[c[idx]], perm[idx]) = (perm[idx], perm[c[idx]]);

                results[index++] = (byte[])perm.Clone();

                if (index % 5_000_000 == 0)
                    progress?.Report(index);

                c[idx]++;
                idx = 0;
            }
            else
            {
                c[idx] = 0;
                idx++;
            }
        }

        progress?.Report(index);
        return results;
    }
}
