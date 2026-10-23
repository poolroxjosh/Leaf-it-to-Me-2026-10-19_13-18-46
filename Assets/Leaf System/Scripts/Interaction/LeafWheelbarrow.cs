
using System;
using UnityEngine;

public class LeafWheelbarrow : MonoBehaviour
{
    [Header("References")]
    public LeafManager leafManager;

    [Header("Progress")]
    [SerializeField]
    private int storedLeaves;

    public int StoredLeaves => storedLeaves;

    public int TotalLeaves =>
        leafManager != null ? leafManager.TotalLeaves : 0;

    public float FillPercent =>
        TotalLeaves > 0
            ? Mathf.Clamp01((float)storedLeaves / TotalLeaves)
            : 0f;

    public bool IsComplete =>
        TotalLeaves > 0 && storedLeaves >= TotalLeaves;

    public event Action OnStorageChanged;
    public event Action OnYardCompleted;

    private bool completionTriggered;

    public int Deposit(int amount)
    {
        if (amount <= 0 || IsComplete)
            return 0;

        int accepted = Mathf.Min(
            amount,
            Mathf.Max(0, TotalLeaves - storedLeaves)
        );

        storedLeaves += accepted;

        if (accepted > 0)
        {
            OnStorageChanged?.Invoke();

            if (IsComplete && !completionTriggered)
            {
                completionTriggered = true;
                OnYardCompleted?.Invoke();

                Debug.Log(
                    "All leaves have been delivered!"
                );
            }
        }

        return accepted;
    }

    public void ResetStorage()
    {
        storedLeaves = 0;
        completionTriggered = false;
        OnStorageChanged?.Invoke();
    }
}
