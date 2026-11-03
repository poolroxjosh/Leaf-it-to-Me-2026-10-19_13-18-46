
using System;
using UnityEngine;

public class LeafWallet : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int balanceCents = 0;

    [SerializeField, Min(0)]
    private int lifetimeEarningsCents = 0;

    public int BalanceCents => balanceCents;
    public int LifetimeEarningsCents => lifetimeEarningsCents;

    public event Action OnBalanceChanged;

    public void EarnFromLeaves(int leafCount)
    {
        if (leafCount <= 0)
            return;

        // One cent per delivered leaf.
        checked
        {
            balanceCents += leafCount;
            lifetimeEarningsCents += leafCount;
        }

        OnBalanceChanged?.Invoke();
    }

    public bool TrySpend(int cents)
    {
        if (cents < 0 || balanceCents < cents)
            return false;

        balanceCents -= cents;
        OnBalanceChanged?.Invoke();
        return true;
    }

    public string GetFormattedBalance()
    {
        return (balanceCents / 100m).ToString("C2");
    }
}
