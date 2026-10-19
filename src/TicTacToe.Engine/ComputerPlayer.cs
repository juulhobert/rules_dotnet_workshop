using System;
using System.Collections.Generic;

namespace TicTacToe.Engine;

/// <summary>
/// Picks moves with a simple priority: take a winning move, block the
/// opponent's winning move, otherwise play a random empty cell.
/// </summary>
public class ComputerPlayer
{
    private readonly Random _random = new();
    private readonly Player _me;
    private readonly Player _opponent;

    public ComputerPlayer(Player me, Player opponent)
    {
        _me = me;
        _opponent = opponent;
    }

    public int ChooseMove(Board board)
    {
        return FindWinningMove(board, _me)
            ?? FindWinningMove(board, _opponent)
            ?? RandomMove(board);
    }

    /// <summary>The cell that wins the game for <paramref name="player"/>, if any.</summary>
    private int? FindWinningMove(Board board, Player player)
    {
        foreach (var cell in board.EmptyCells())
        {
            var copy = board.Clone();
            copy.Place(cell, player);
            if (copy.Winner() == player)
            {
                return cell;
            }
        }

        return null;
    }

    private int RandomMove(Board board)
    {
        var empty = new List<int>(board.EmptyCells());
        return empty[_random.Next(empty.Count)];
    }
}
