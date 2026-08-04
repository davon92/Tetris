using UnityEngine;

public enum SaveSlotMenuMode
{
    Save,
    Load
}

public enum SaveSlotIntent
{
    None,

    /// <summary>The cursor slot was activated; the caller reads <see cref="SaveSlotMenuModel.Cursor"/>.</summary>
    Use,

    /// <summary>Leave the slot list.</summary>
    Back
}

/// <summary>
/// Cursor rules for the ten-slot browser shared by the title screen's load
/// route and story mode's pause menu. Slots run down two columns of five, with
/// a back item below them; pure C# so every navigation edge is unit-testable.
/// </summary>
public sealed class SaveSlotMenuModel
{
    public const int Rows = 5;
    public const int Columns = 2;
    public const int BackIndex = SaveSlotCatalog.SlotCount;

    /// <summary>Column the cursor was in when it dropped onto the back item.</summary>
    private int lastColumn;

    public SaveSlotMenuMode Mode { get; private set; } = SaveSlotMenuMode.Load;

    public int Cursor { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public bool IsBackSelected => Cursor == BackIndex;

    public void Begin(SaveSlotMenuMode mode, int cursor = 0)
    {
        Mode = mode;
        Message = string.Empty;
        MoveTo(cursor);
    }

    public void SetMessage(string message)
    {
        Message = message ?? string.Empty;
    }

    public void MoveTo(int index)
    {
        Cursor = Mathf.Clamp(index, 0, BackIndex);
        if (Cursor != BackIndex)
            lastColumn = Cursor / Rows;

        Message = string.Empty;
    }

    /// <summary>
    /// Grid navigation. Vertical movement past either end of a column lands on
    /// the back item, so the whole page can be walked with one axis.
    /// </summary>
    public void Move(int deltaX, int deltaY)
    {
        Message = string.Empty;

        if (IsBackSelected)
        {
            MoveFromBack(deltaX, deltaY);
            return;
        }

        int row = Cursor % Rows;
        int column = Cursor / Rows;

        if (deltaX != 0)
            column = (column + deltaX % Columns + Columns) % Columns;

        if (deltaY != 0)
        {
            row += deltaY;
            if (row < 0 || row >= Rows)
            {
                lastColumn = column;
                Cursor = BackIndex;
                return;
            }
        }

        lastColumn = column;
        Cursor = column * Rows + row;
    }

    public SaveSlotIntent Confirm()
    {
        return IsBackSelected ? SaveSlotIntent.Back : SaveSlotIntent.Use;
    }

    private void MoveFromBack(int deltaX, int deltaY)
    {
        if (deltaX != 0)
        {
            lastColumn = (lastColumn + deltaX % Columns + Columns) % Columns;
            return;
        }

        if (deltaY < 0)
            Cursor = lastColumn * Rows + Rows - 1;
        else if (deltaY > 0)
            Cursor = lastColumn * Rows;
    }
}
