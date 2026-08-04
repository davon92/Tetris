using UnityEngine;

public enum StoryPausePage
{
    Closed,
    Root,
    Slots,
    ConfirmExit,
    ConfirmOverwrite
}

public enum StoryPauseIntent
{
    None,

    /// <summary>Close the menu and hand the chapter back to the player.</summary>
    Resume,

    /// <summary>Write the chapter to <see cref="StoryPauseCommand.Slot"/>.</summary>
    Save,

    /// <summary>Restore the chapter from <see cref="StoryPauseCommand.Slot"/>.</summary>
    Load,

    ReturnToTitle
}

/// <summary>What activating the current pause item asked the game to do.</summary>
public readonly struct StoryPauseCommand
{
    private StoryPauseCommand(StoryPauseIntent intent, int slot)
    {
        Intent = intent;
        Slot = slot;
    }

    public StoryPauseIntent Intent { get; }

    /// <summary>Valid for <see cref="StoryPauseIntent.Save"/> and <see cref="StoryPauseIntent.Load"/>.</summary>
    public int Slot { get; }

    public static StoryPauseCommand None => new StoryPauseCommand(StoryPauseIntent.None, -1);

    public static StoryPauseCommand Resume => new StoryPauseCommand(StoryPauseIntent.Resume, -1);

    public static StoryPauseCommand ReturnToTitle =>
        new StoryPauseCommand(StoryPauseIntent.ReturnToTitle, -1);

    public static StoryPauseCommand Save(int slot) =>
        new StoryPauseCommand(StoryPauseIntent.Save, slot);

    public static StoryPauseCommand Load(int slot) =>
        new StoryPauseCommand(StoryPauseIntent.Load, slot);
}

/// <summary>
/// The story-mode pause menu: a root page, the shared ten-slot browser in save
/// or load mode, and two confirmation modals. Pure C# apart from the slot
/// summaries it reads, so the whole branch table is covered by EditMode tests.
/// </summary>
public sealed class StoryPauseModel
{
    public const int RootItemCount = 4;
    public const int ResumeItem = 0;
    public const int SaveItem = 1;
    public const int LoadItem = 2;
    public const int ExitItem = 3;

    /// <summary>Modal buttons read yes/no left to right; no is the safe default.</summary>
    public const int ConfirmYes = 0;
    public const int ConfirmNo = 1;

    public const string EmptySlotMessage = "THAT SLOT IS EMPTY";
    public const string DamagedSlotMessage = "THAT SAVE CANNOT BE READ";
    public const string SaveFailedMessage = "COULD NOT WRITE THAT SLOT";

    private readonly SaveSlotCatalog catalog;

    public StoryPauseModel(SaveSlotCatalog catalog)
    {
        this.catalog = catalog;
        Slots = new SaveSlotMenuModel();
    }

    public StoryPausePage Page { get; private set; } = StoryPausePage.Closed;

    public SaveSlotMenuModel Slots { get; }

    /// <summary>Cursor for the root page and for both modals.</summary>
    public int Selection { get; private set; }

    public string Message { get; private set; } = string.Empty;

    /// <summary>Slot awaiting an overwrite confirmation, or -1.</summary>
    public int PendingSlot { get; private set; } = -1;

    public bool IsOpen => Page != StoryPausePage.Closed;

    public bool IsModal => Page == StoryPausePage.ConfirmExit || Page == StoryPausePage.ConfirmOverwrite;

    public void Open()
    {
        Page = StoryPausePage.Root;
        Selection = ResumeItem;
        PendingSlot = -1;
        Message = string.Empty;
    }

    public void Close()
    {
        Page = StoryPausePage.Closed;
        Selection = ResumeItem;
        PendingSlot = -1;
        Message = string.Empty;
    }

    public void Move(int deltaX, int deltaY)
    {
        switch (Page)
        {
            case StoryPausePage.Root:
                if (deltaY != 0)
                    Selection = (Selection + deltaY % RootItemCount + RootItemCount) % RootItemCount;

                Message = string.Empty;
                break;

            case StoryPausePage.Slots:
                Slots.Move(deltaX, deltaY);
                Message = string.Empty;
                break;

            case StoryPausePage.ConfirmExit:
            case StoryPausePage.ConfirmOverwrite:
                if (deltaX != 0 || deltaY != 0)
                    Selection = Selection == ConfirmYes ? ConfirmNo : ConfirmYes;

                break;
        }
    }

    /// <summary>Mouse activation: point at an item on the current page, then confirm it.</summary>
    public StoryPauseCommand ConfirmAt(int index)
    {
        if (Page == StoryPausePage.Slots)
            Slots.MoveTo(index);
        else if (Page != StoryPausePage.Closed)
            Selection = index;

        return Confirm();
    }

    public StoryPauseCommand Confirm()
    {
        switch (Page)
        {
            case StoryPausePage.Root:
                return ConfirmRoot();

            case StoryPausePage.Slots:
                return ConfirmSlot();

            case StoryPausePage.ConfirmExit:
                if (Selection == ConfirmYes)
                    return StoryPauseCommand.ReturnToTitle;

                ShowRoot(ExitItem);
                return StoryPauseCommand.None;

            case StoryPausePage.ConfirmOverwrite:
                if (Selection == ConfirmYes && SaveSlotCatalog.IsValidSlot(PendingSlot))
                    return StoryPauseCommand.Save(PendingSlot);

                Page = StoryPausePage.Slots;
                PendingSlot = -1;
                return StoryPauseCommand.None;

            default:
                return StoryPauseCommand.None;
        }
    }

    /// <summary>Escape/B. Backs out one level, and closes the menu from the root.</summary>
    public StoryPauseCommand Back()
    {
        switch (Page)
        {
            case StoryPausePage.Root:
                return StoryPauseCommand.Resume;

            case StoryPausePage.Slots:
                ShowRoot(Slots.Mode == SaveSlotMenuMode.Save ? SaveItem : LoadItem);
                return StoryPauseCommand.None;

            case StoryPausePage.ConfirmExit:
                ShowRoot(ExitItem);
                return StoryPauseCommand.None;

            case StoryPausePage.ConfirmOverwrite:
                Page = StoryPausePage.Slots;
                PendingSlot = -1;
                return StoryPauseCommand.None;

            default:
                return StoryPauseCommand.None;
        }
    }

    /// <summary>Keeps the player on the slot list after a write so they can see the result.</summary>
    public void ReportSaved(int slot)
    {
        Page = StoryPausePage.Slots;
        PendingSlot = -1;
        Message = $"SAVED TO SLOT {SaveSlotCatalog.SlotLabel(slot)}";
    }

    public void ReportSaveFailed()
    {
        Page = StoryPausePage.Slots;
        PendingSlot = -1;
        Message = SaveFailedMessage;
    }

    public void ReportLoadFailed()
    {
        Page = StoryPausePage.Slots;
        PendingSlot = -1;
        Message = DamagedSlotMessage;
    }

    private StoryPauseCommand ConfirmRoot()
    {
        switch (Selection)
        {
            case ResumeItem:
                return StoryPauseCommand.Resume;

            case SaveItem:
                ShowSlots(SaveSlotMenuMode.Save);
                return StoryPauseCommand.None;

            case LoadItem:
                ShowSlots(SaveSlotMenuMode.Load);
                return StoryPauseCommand.None;

            default:
                Page = StoryPausePage.ConfirmExit;
                Selection = ConfirmNo;
                Message = string.Empty;
                return StoryPauseCommand.None;
        }
    }

    private StoryPauseCommand ConfirmSlot()
    {
        if (Slots.Confirm() == SaveSlotIntent.Back)
        {
            ShowRoot(Slots.Mode == SaveSlotMenuMode.Save ? SaveItem : LoadItem);
            return StoryPauseCommand.None;
        }

        int slot = Slots.Cursor;
        SaveSlotInfo info = catalog != null ? catalog.GetSlot(slot) : SaveSlotInfo.Empty(slot);

        if (Slots.Mode == SaveSlotMenuMode.Save)
        {
            if (info.IsEmpty)
                return StoryPauseCommand.Save(slot);

            PendingSlot = slot;
            Page = StoryPausePage.ConfirmOverwrite;
            Selection = ConfirmNo;
            Message = string.Empty;
            return StoryPauseCommand.None;
        }

        if (info.IsEmpty)
        {
            Message = EmptySlotMessage;
            return StoryPauseCommand.None;
        }

        if (!info.IsUsable)
        {
            Message = DamagedSlotMessage;
            return StoryPauseCommand.None;
        }

        return StoryPauseCommand.Load(slot);
    }

    private void ShowRoot(int selection)
    {
        Page = StoryPausePage.Root;
        Selection = Mathf.Clamp(selection, 0, RootItemCount - 1);
        PendingSlot = -1;
        Message = string.Empty;
    }

    private void ShowSlots(SaveSlotMenuMode mode)
    {
        Page = StoryPausePage.Slots;
        Slots.Begin(mode);
        PendingSlot = -1;
        Message = string.Empty;
    }
}
