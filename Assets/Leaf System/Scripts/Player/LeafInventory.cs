
using System;
using UnityEngine;

public class LeafInventory : MonoBehaviour
{
    [Header("Capacity")]
    [Min(1)]
    public int maxCapacity = 25;

    public int CurrentLeaves { get; private set; }

    // Leaves flying toward the player's bag.
    public int PendingLeaves { get; private set; }

    public int AvailableCapacity =>
        Mathf.Max(
            0,
            maxCapacity - CurrentLeaves - PendingLeaves
        );

    public bool IsFull => AvailableCapacity <= 0;

    public event Action OnInventoryChanged;

    public bool ReserveLeaf()
    {
        if (AvailableCapacity <= 0)
            return false;

        PendingLeaves++;
        OnInventoryChanged?.Invoke();

        return true;
    }

    public void CompleteLeafPickup()
    {
        if (PendingLeaves <= 0)
            return;

        PendingLeaves--;
        CurrentLeaves++;

        OnInventoryChanged?.Invoke();
    }

    public void CancelLeafPickup()
    {
        if (PendingLeaves <= 0)
            return;

        PendingLeaves--;
        OnInventoryChanged?.Invoke();
    }

    public int RemoveLeaves(int amount)
    {
        int removed = Mathf.Clamp(
            amount,
            0,
            CurrentLeaves
        );

        CurrentLeaves -= removed;

        if (removed > 0)
            OnInventoryChanged?.Invoke();

        return removed;
    }

    public void SetCapacity(int capacity)
    {
        maxCapacity = Mathf.Max(1, capacity);
        OnInventoryChanged?.Invoke();
    }
}
