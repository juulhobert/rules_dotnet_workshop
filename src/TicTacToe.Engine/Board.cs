using System;
using System.Collections.Generic;

namespace TicTacToe.Engine;

/// <summary>
/// A 3x3 tic-tac-toe board. Cells are numbered 0-8, left to right, top to bottom.
/// </summary>
public class Board
{
    private static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, // rows
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, // columns
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }, // diagonals
    };

    private readonly Player?[] _cells = new Player?[9];

    /// <summary>Returns the mark in the given cell, or null when the cell is empty.</summary>
    public Player? GetCell(int index) => _cells[index];

    public bool IsEmpty(int index) => _cells[index] is null;

    /// <summary>The indices of all empty cells, in order.</summary>
    public IEnumerable<int> EmptyCells()
    {
        for (var i = 0; i < 9; i++)
        {
            if (_cells[i] is null)
            {
                yield return i;
            }
        }
    }

    /// <summary>Places a mark. Throws when the cell is already taken.</summary>
    public void Place(int index, Player player)
    {
        if (!IsEmpty(index))
        {
            throw new InvalidOperationException($"Cell {index} is already taken.");
        }

        _cells[index] = player;
    }

    /// <summary>The winning player, or null when there is no winner (yet).</summary>
    public Player? Winner()
    {
        foreach (var line in Lines)
        {
            var first = _cells[line[0]];
            if (first is not null && first == _cells[line[1]] && first == _cells[line[2]])
            {
                return first;
            }
        }

        return null;
    }

    public bool IsFull()
    {
        foreach (var cell in _cells)
        {
            if (cell is null)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsGameOver() => Winner() is not null || IsFull();

    /// <summary>A copy of this board, used by the computer to try moves out.</summary>
    public Board Clone()
    {
        var copy = new Board();
        for (var i = 0; i < 9; i++)
        {
            if (_cells[i] is Player player)
            {
                copy.Place(i, player);
            }
        }

        return copy;
    }
}
