# AYTO Solver 🔍💕

A high-performance C# solver for the reality TV show "Are You The One?" that calculates match probabilities based on matchbox results and matching night data.

## Overview

This solver generates and filters through **39,916,800 permutations** (11!) to determine the probability of each potential match between contestants. It uses in-memory computation to efficiently process millions of configurations and narrow down the possible perfect matches.

## Features

- **🚀 High Performance**: Generates all 39.9M permutations in memory (~440 MB RAM)
- **🎯 Smart Filtering**: Applies matchbox results and matching night data to eliminate impossible configurations
- **📊 Probability Analysis**: Calculates and displays probability matrix for all potential matches
- **⚡ Server GC**: Optimized for large-scale data processing with server garbage collection
- **🎨 Interactive CLI**: User-friendly console interface with progress tracking

## Requirements

- .NET 10.0 SDK or later
- ~500 MB RAM (for permutation generation and filtering)

## Project Structure

```
ayto/
├── src/
│   └── AytoSolver/
│       ├── Data/
│       │   ├── season_2025.json      # Season 2025 data
│       │   └── season_2026.json      # Season 2026 data
│       ├── Models/
│       │   └── SeasonData.cs         # Data models
│       ├── Services/
│       │   ├── PermutationGenerator.cs
│       │   ├── FilteringService.cs
│       │   └── ProbabilityService.cs
│       ├── Program.cs                # Main application
│       └── AytoSolver.csproj
└── tests/
    └── AytoSolver.Tests/
        ├── AytoSolverTests.cs
        └── AytoSolver.Tests.csproj
```

## Getting Started

### Installation

1. Clone the repository:
```bash
git clone https://github.com/Suneeh/AYTO
cd AYTO
```

2. Restore dependencies:
```bash
dotnet restore
```

### Configuration

The solver comes with data for two seasons:
- `src/AytoSolver/Data/season_2025.json` - Season 2025 data
- `src/AytoSolver/Data/season_2026.json` - Season 2026 data

You can edit either file or add new season files with the following structure:

```json
{
  "season": "2025",
  "men": ["Man1", "Man2", ...],
  "women": ["Woman1", "Woman2", ...],
  "matchboxResults": [
    {
      "woman": "Woman1",
      "man": "Man1",
      "result": "PerfectMatch"
    }
  ],
  "matchingNights": [
    {
      "nightNumber": 1,
      "lights": 3,
      "sitsOut": "Woman11",
      "pairs": [
        { "woman": "Woman1", "man": "Man1" }
      ]
    }
  ]
}
```

### Running the Solver

```bash
dotnet run --project src/AytoSolver
```

When you start the application, you'll be prompted to select which season to analyze:
- **1** - Season 2025
- **2** - Season 2026
- **0** - Exit

### Menu Options

1. **Generate Permutations** - Creates all 39,916,800 possible configurations
2. **Apply Filters** - Eliminates impossible matches based on known results
3. **Show Probabilities** - Displays probability matrix for all potential matches
4. **Show Status** - Shows current state and memory usage
5. **Full Pipeline** - Runs all steps sequentially (Generate → Filter → Probabilities)
0. **Exit** - Quit the application

## How It Works

### 1. Permutation Generation
The solver generates all possible permutations (11!) of women matched to men. Each permutation represents a potential complete matching configuration.

### 2. Filtering
The solver eliminates configurations that contradict known information:
- **Matchbox Results**: Confirmed perfect matches or confirmed non-matches
- **Matching Nights**: Configurations must produce the correct number of "lights" (correct matches) for each matching night

### 3. Probability Calculation
For each remaining valid configuration, the solver counts how many times each man-woman pair appears and calculates the probability as:

```
P(Man, Woman) = Count(Man, Woman in valid configs) / Total valid configs
```

## Running Tests

```bash
dotnet test
```

## Performance

- **Generation**: ~1-2 seconds for 39.9M permutations
- **Filtering**: Varies based on number of constraints (typically 1-5 seconds)
- **Probability Calculation**: ~1-2 seconds
- **Memory Usage**: ~440-500 MB

## Technical Details

- **Target Framework**: .NET 10.0
- **Language Features**: C# with implicit usings and nullable reference types
- **GC Mode**: Server garbage collection for optimized throughput
- **Data Structure**: Byte arrays for memory-efficient permutation storage

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the MIT License.

## Acknowledgments

Inspired by the reality TV show "Are You The One?" where contestants must find their perfect matches through a series of matchbox ceremonies and matching nights.

---

**Note**: This solver is for entertainment and educational purposes. The algorithm assumes all input data is accurate and that there exists exactly one perfect match for each contestant.
