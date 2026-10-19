using System;
using TicTacToe.Engine;

var board = new Board();
var computer = new ComputerPlayer(Player.O, Player.X);

Console.WriteLine("Tic-Tac-Toe — you are X, the computer is O.");
Console.WriteLine("Cells are numbered 1-9, left to right, top to bottom.");
Console.WriteLine();

while (!board.IsGameOver())
{
    Render(board);

    // Human move.
    int move;
    while (true)
    {
        Console.Write("Your move (1-9): ");
        var input = Console.ReadLine();
        if (input is null)
        {
            Console.WriteLine("Goodbye!");
            return;
        }

        if (int.TryParse(input, out move) && move >= 1 && move <= 9 && board.IsEmpty(move - 1))
        {
            break;
        }

        Console.WriteLine("Invalid move, try again.");
    }

    board.Place(move - 1, Player.X);

    if (board.IsGameOver())
    {
        break;
    }

    // Computer move.
    var computerMove = computer.ChooseMove(board);
    board.Place(computerMove, Player.O);
    Console.WriteLine($"The computer plays cell {computerMove + 1}.");
}

Render(board);

var winner = board.Winner();
Console.WriteLine(winner switch
{
    Player.X => "You win!",
    Player.O => "The computer wins!",
    _ => "It's a draw!",
});

static void Render(Board board)
{
    Console.WriteLine();
    for (var row = 0; row < 3; row++)
    {
        var cells = new string[3];
        for (var col = 0; col < 3; col++)
        {
            var index = row * 3 + col;
            cells[col] = board.GetCell(index) switch
            {
                Player.X => "X",
                Player.O => "O",
                _ => (index + 1).ToString(),
            };
        }

        Console.WriteLine($" {cells[0]} | {cells[1]} | {cells[2]} ");
        if (row < 2)
        {
            Console.WriteLine("---+---+---");
        }
    }

    Console.WriteLine();
}
