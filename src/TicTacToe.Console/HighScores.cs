using System.IO;
using Newtonsoft.Json;

namespace TicTacToe.Console;

/// <summary>
/// Persistent win/loss/draw counters, stored as JSON in highscores.json
/// next to the running program.
/// </summary>
public class HighScores
{
    private const string FilePath = "highscores.json";

    public int HumanWins { get; set; }

    public int ComputerWins { get; set; }

    public int Draws { get; set; }

    public static HighScores Load()
    {
        if (!File.Exists(FilePath))
        {
            return new HighScores();
        }

        var json = File.ReadAllText(FilePath);
        return JsonConvert.DeserializeObject<HighScores>(json) ?? new HighScores();
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }
}
