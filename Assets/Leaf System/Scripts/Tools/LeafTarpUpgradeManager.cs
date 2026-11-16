
using System;
using UnityEngine;

public class LeafTarpUpgradeManager : MonoBehaviour
{
    [Header("References")]
    public LeafWallet wallet;
    public LeafRakeUpgradeManager rakeUpgrades;
    public PlayerToolController toolController;

    [Header("Purchase")]
    [Min(1)]
    public int tarpPriceCents = 1000;

    public bool IsTarpOwned { get; private set; }

    public event Action OnTarpChanged;

    public bool CanPurchaseTarp()
    {
        return !IsTarpOwned &&
               wallet != null &&
               rakeUpgrades != null &&
               rakeUpgrades.IsRakeOwned &&
               wallet.BalanceCents >= tarpPriceCents;
    }

    public bool PurchaseTarp()
    {
        if (!CanPurchaseTarp())
            return false;

        if (!wallet.TrySpend(tarpPriceCents))
            return false;

        IsTarpOwned = true;

        if (toolController != null)
            toolController.UnlockTarp();

        OnTarpChanged?.Invoke();

        Debug.Log("Tarp purchased and unlocked!");

        return true;
    }
}