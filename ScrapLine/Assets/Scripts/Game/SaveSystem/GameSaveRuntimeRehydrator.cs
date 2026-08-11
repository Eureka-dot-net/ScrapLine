using System;
using System.Collections.Generic;

/// <summary>
/// Restores runtime-only object identity and rebases absolute Unity timestamps after deserialization.
/// </summary>
public static class GameSaveRuntimeRehydrator
{
    public static bool TryPrepareForResume(
        GameData data,
        float currentRuntimeTime,
        float itemMoveSpeed,
        out string error)
    {
        error = null;
        if (data == null)
        {
            error = "Cannot rehydrate null game data.";
            return false;
        }

        Dictionary<string, ItemData> itemsById = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        HashSet<ItemData> uniqueItems = new HashSet<ItemData>();
        foreach (GridData grid in data.grids)
        {
            foreach (CellData cell in grid.cells)
            {
                foreach (ItemData item in cell.items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.id))
                    {
                        error = $"Cell {cell.x}:{cell.y} contains an item without a stable ID.";
                        return false;
                    }
                    if (!itemsById.TryAdd(item.id, item))
                    {
                        error = $"Item ID '{item.id}' appears in more than one cell item list.";
                        return false;
                    }
                    uniqueItems.Add(item);
                }
            }
        }

        foreach (GridData grid in data.grids)
        {
            foreach (CellData cell in grid.cells)
            {
                for (int index = 0; index < cell.waitingItems.Count; index++)
                {
                    ItemData waitingCopy = cell.waitingItems[index];
                    if (waitingCopy == null || string.IsNullOrWhiteSpace(waitingCopy.id) ||
                        !itemsById.TryGetValue(waitingCopy.id, out ItemData canonical))
                    {
                        error = $"Cell {cell.x}:{cell.y} waiting item at index {index} has no matching grid item.";
                        return false;
                    }
                    cell.waitingItems[index] = canonical;
                    uniqueItems.Add(canonical);
                }
            }
        }

        if (data.hasRuntimeClockAnchor)
        {
            float clockDelta = currentRuntimeTime - data.savedAtRuntimeTime;
            foreach (ItemData item in uniqueItems)
                ShiftTimers(item, clockDelta);
        }
        else
        {
            float safeMoveSpeed = itemMoveSpeed > 0f ? itemMoveSpeed : 1f;
            foreach (ItemData item in uniqueItems)
            {
                if (item.state == ItemState.Moving || item.state == ItemState.Waiting)
                    item.moveStartTime = currentRuntimeTime - Math.Max(0f, item.moveProgress) / safeMoveSpeed;
                else if (item.moveStartTime > 0f)
                    item.moveStartTime = currentRuntimeTime;

                if (item.state == ItemState.Processing)
                    item.processingStartTime = currentRuntimeTime;
                else if (item.processingStartTime > 0f)
                    item.processingStartTime = currentRuntimeTime;

                if (item.state == ItemState.Waiting)
                    item.waitingStartTime = currentRuntimeTime;
                else if (item.waitingStartTime > 0f)
                    item.waitingStartTime = currentRuntimeTime;
            }
        }

        data.hasRuntimeClockAnchor = true;
        data.savedAtRuntimeTime = currentRuntimeTime;
        return true;
    }

    private static void ShiftTimers(ItemData item, float delta)
    {
        // Active timers may legitimately have started at Time.time == 0. After rebasing onto a
        // fresh clock they may also be negative; item state, rather than sign, identifies them.
        if (item.moveStartTime != 0f || item.state == ItemState.Moving || item.state == ItemState.Waiting)
            item.moveStartTime += delta;
        if (item.processingStartTime != 0f || item.state == ItemState.Processing)
            item.processingStartTime += delta;
        if (item.waitingStartTime != 0f || item.state == ItemState.Waiting)
            item.waitingStartTime += delta;
    }
}
