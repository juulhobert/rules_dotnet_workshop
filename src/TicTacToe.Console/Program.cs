using System.Linq;
using Spectre.Console;
using TicTacToe.Console;
using TicTacToe.Engine;

var board = new Board();
var computer = new ComputerPlayer(Player.O, Player.X);
var scores = HighScores.Load();

AnsiConsole.Write(new FigletText("Tic-Tac-Toe").Color(Color.Blue));
AnsiConsole.MarkupLine("You are [bold yellow]X[/], the computer is [bold green]O[/].");
AnsiConsole.WriteLine();

while (!board.IsGameOver())
{
    Render(board);

    // Human move: pick one of the empty cells.
    var choices = board.EmptyCells().Select(cell => (cell + 1).ToString());
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Your move:")
            .AddChoices(choices));
    board.Place(int.Parse(choice) - 1, Player.X);

    if (board.IsGameOver())
    {
        break;
    }

    // Computer move.
    var computerMove = computer.ChooseMove(board);
    board.Place(computerMove, Player.O);
    AnsiConsole.MarkupLine($"The computer plays cell [bold green]{computerMove + 1}[/].");
}

Render(board);

var winner = board.Winner();
if (winner == Player.X)
{
    AnsiConsole.MarkupLine("[bold yellow]You win![/]");
    scores.HumanWins++;
}
else if (winner == Player.O)
{
    AnsiConsole.MarkupLine("[bold green]The computer wins![/]");
    scores.ComputerWins++;
}
else
{
    AnsiConsole.MarkupLine("[bold]It's a draw![/]");
    scores.Draws++;
}

scores.Save();
RenderScores(scores);

static void Render(Board board)
{
    var grid = new Grid();
    grid.AddColumn();
    grid.AddColumn();
    grid.AddColumn();

    for (var row = 0; row < 3; row++)
    {
        var cells = new Markup[3];
        for (var col = 0; col < 3; col++)
        {
            var index = row * 3 + col;
            cells[col] = board.GetCell(index) switch
            {
                Player.X => new Markup("[bold yellow]X[/]"),
                Player.O => new Markup("[bold green]O[/]"),
                _ => new Markup($"[grey]{index + 1}[/]"),
            };
        }

        grid.AddRow(cells);
    }

    AnsiConsole.Write(grid);
    AnsiConsole.WriteLine();
}

static void RenderScores(HighScores scores)
{
    var table = new Table();
    table.AddColumn("You");
    table.AddColumn("Computer");
    table.AddColumn("Draws");
    table.AddRow(
        scores.HumanWins.ToString(),
        scores.ComputerWins.ToString(),
        scores.Draws.ToString());
    AnsiConsole.Write(table);
}
