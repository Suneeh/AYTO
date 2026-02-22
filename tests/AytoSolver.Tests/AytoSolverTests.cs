using AwesomeAssertions;
using AytoSolver.Models;
using AytoSolver.Services;

namespace AytoSolver.Tests;

public class AytoSolverTests
{
    private readonly SeasonData _mockSeason = new()
    {
        Season = "TestSet",
        Men = ["M1", "M2", "M3", "M4"],
        Women = ["W1", "W2", "W3", "W4", "W5"]
    };

    // Setup 4 men and 5 women (M5 is dummy "Nobody")

    [Fact]
    public void PermutationGenerator_GeneratesCorrectCountForSmallSet()
    {
        // Arrange & Act
        var result = PermutationGenerator.GenerateAll(5); // 5! = 120

        // Assert
        result.Length.Should().Be(120);
        
        // Ensure all permutations are distinct
        var uniquePerms = new HashSet<string>(result.Select(b => string.Join(",", b)));
        uniquePerms.Count.Should().Be(120);
    }

    [Fact]
    public void MatchboxFilter_NoMatch_EliminatesCorrectly()
    {
        // Arrange
        var perms = PermutationGenerator.GenerateAll(5);
        _mockSeason.MatchboxResults = [
            new MatchboxResult { Woman = "W1", Man = "M1", Result = "NoMatch" }
        ];
        var service = new FilteringService(_mockSeason);

        // Act
        var eliminated = service.ApplyAllFilters(perms);

        // Assert
        // In 5! = 120 permutations, 4! = 24 have W1 paired with M1 (index 0)
        // So 120 - 24 = 96 should remain
        FilteringService.CountRemaining(eliminated).Should().Be(96);
        
        // Verify that all remaining configs do NOT have W1 paired with M1
        for (int i = 0; i < perms.Length; i++)
        {
            if (!eliminated[i])
            {
                perms[i][0].Should().NotBe(0); // W1 (index 0) should NOT be paired with M1 (value 0)
            }
        }
    }

    [Fact]
    public void MatchboxFilter_PerfectMatch_EliminatesCorrectly()
    {
        // Arrange
        var perms = PermutationGenerator.GenerateAll(5);
        _mockSeason.MatchboxResults = [
            new MatchboxResult { Woman = "W1", Man = "M1", Result = "PerfectMatch" }
        ];
        var service = new FilteringService(_mockSeason);

        // Act
        var eliminated = service.ApplyAllFilters(perms);

        // Assert
        // Only 4! = 24 should remain (those with W1 paired with M1)
        FilteringService.CountRemaining(eliminated).Should().Be(24);
        
        // Verify all remaining configs have W1 paired with M1
        for (int i = 0; i < perms.Length; i++)
        {
            if (!eliminated[i])
            {
                perms[i][0].Should().Be(0); // W1 (index 0) MUST be paired with M1 (value 0)
            }
        }
    }

    [Fact]
    public void MatchingNightFilter_ZeroLights_EliminatesCorrectly()
    {
        // Arrange
        var perms = PermutationGenerator.GenerateAll(5);
        _mockSeason.MatchingNights = [
            new MatchingNight 
            { 
                NightNumber = 1, 
                Lights = 0, 
                SitsOut = "W5",
                Pairs = [
                    new Pair { Woman = "W1", Man = "M1" },
                    new Pair { Woman = "W2", Man = "M2" },
                    new Pair { Woman = "W3", Man = "M3" },
                    new Pair { Woman = "W4", Man = "M4" }
                ]
            }
        ];
        var service = new FilteringService(_mockSeason);

        // Act
        var eliminated = service.ApplyAllFilters(perms);

        // Assert
        // Identity permutation [0,1,2,3,4] has all 4 pairs matching = 4 lights, so it should be eliminated
        eliminated[0].Should().BeTrue();
        
        // Verify remaining configs have exactly 0 matching pairs
        for (int i = 0; i < perms.Length; i++)
        {
            if (!eliminated[i])
            {
                int matches = 0;
                if (perms[i][0] == 0) matches++; // W1 x M1
                if (perms[i][1] == 1) matches++; // W2 x M2
                if (perms[i][2] == 2) matches++; // W3 x M3
                if (perms[i][3] == 3) matches++; // W4 x M4
                
                matches.Should().Be(0);
            }
        }
    }

    [Fact]
    public void ProbabilityService_CalculatesCorrectPercentages()
    {
        // Arrange
        var perms = PermutationGenerator.GenerateAll(3); // 3! = 6
        var eliminated = new bool[6]; // No eliminations
        
        var season = new SeasonData 
        { 
            Men = ["M1", "M2"], 
            Women = ["W1", "W2", "W3"] 
        };
        var service = new ProbabilityService(season);

        // Act
        var (matrix, total) = service.CalculateProbabilities(perms, eliminated);

        // Assert
        total.Should().Be(6);
        
        // Each woman-man pair should appear in exactly 2 of the 6 permutations
        // 2/6 = 33.33%
        matrix[0, 0].Should().BeApproximately(33.33, 0.1); // W1 x M1
        matrix[1, 1].Should().BeApproximately(33.33, 0.1); // W2 x M2
        matrix[2, 2].Should().BeApproximately(33.33, 0.1); // W3 x Nobody
    }
}
