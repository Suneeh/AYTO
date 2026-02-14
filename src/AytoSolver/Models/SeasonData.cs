using System.Text.Json.Serialization;

namespace AytoSolver.Models;

public class SeasonData
{
    [JsonPropertyName("season")]
    public string Season { get; set; } = "";

    [JsonPropertyName("men")]
    public List<string> Men { get; set; } = [];

    [JsonPropertyName("women")]
    public List<string> Women { get; set; } = [];

    [JsonPropertyName("matchboxResults")]
    public List<MatchboxResult> MatchboxResults { get; set; } = [];

    [JsonPropertyName("matchingNights")]
    public List<MatchingNight> MatchingNights { get; set; } = [];
}

public class MatchboxResult
{
    [JsonPropertyName("woman")]
    public string Woman { get; set; } = "";

    [JsonPropertyName("man")]
    public string Man { get; set; } = "";

    [JsonPropertyName("result")]
    public string Result { get; set; } = ""; // "PerfectMatch" or "NoMatch"
}

public class MatchingNight
{
    [JsonPropertyName("nightNumber")]
    public int NightNumber { get; set; }

    [JsonPropertyName("lights")]
    public int Lights { get; set; }

    [JsonPropertyName("sitsOut")]
    public string SitsOut { get; set; } = "";

    [JsonPropertyName("pairs")]
    public List<Pair> Pairs { get; set; } = [];
}

public class Pair
{
    [JsonPropertyName("woman")]
    public string Woman { get; set; } = "";

    [JsonPropertyName("man")]
    public string Man { get; set; } = "";
}
