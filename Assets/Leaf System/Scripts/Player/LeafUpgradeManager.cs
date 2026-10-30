
using System;
using UnityEngine;

public enum LeafUpgradeType
{
    BagSize,
    Dexterity,
    GraspAmount
}

public class LeafUpgradeManager : MonoBehaviour
{
    [Header("References")]
    public LeafWallet wallet;
    public LeafInventory inventory;
    public LeafPickupController pickupController;

    [Header("Bag Size")]
    public int startingBagCapacity = 25;
    public int capacityPerLevel = 10;
    public int bagBasePriceCents = 100;
    public float bagPriceMultiplier = 1.6f;

    [Header("Dexterity")]
    public float startingCooldown = 0.4f;
    public float cooldownReductionPerLevel = 0.035f;
    public float minimumCooldown = 0.1f;
    public int dexterityBasePriceCents = 150;
    public float dexterityPriceMultiplier = 1.7f;

    [Header("Grasp Amount")]
    public int startingGraspAmount = 1;
    public int graspIncreasePerLevel = 1;
    public int graspBasePriceCents = 250;
    public float graspPriceMultiplier = 2f;

    [Header("Upgrade Limits")]
    public int maxBagLevel = 50;
    public int maxDexterityLevel = 9;
    public int maxGraspLevel = 20;

    public int BagLevel { get; private set; }
    public int DexterityLevel { get; private set; }
    public int GraspLevel { get; private set; }

    public event Action OnUpgradesChanged;

    private void Start()
    {
        ApplyUpgrades();
    }

    public int GetLevel(LeafUpgradeType type)
    {
        switch (type)
        {
            case LeafUpgradeType.BagSize:
                return BagLevel;

            case LeafUpgradeType.Dexterity:
                return DexterityLevel;

            case LeafUpgradeType.GraspAmount:
                return GraspLevel;

            default:
                return 0;
        }
    }

    public int GetMaxLevel(LeafUpgradeType type)
    {
        switch (type)
        {
            case LeafUpgradeType.BagSize:
                return maxBagLevel;

            case LeafUpgradeType.Dexterity:
                return maxDexterityLevel;

            case LeafUpgradeType.GraspAmount:
                return maxGraspLevel;

            default:
                return 0;
        }
    }

    public int GetPrice(LeafUpgradeType type)
    {
        int level = GetLevel(type);

        if (level >= GetMaxLevel(type))
            return -1;

        int basePrice;
        float multiplier;

        switch (type)
        {
            case LeafUpgradeType.BagSize:
                basePrice = bagBasePriceCents;
                multiplier = bagPriceMultiplier;
                break;

            case LeafUpgradeType.Dexterity:
                basePrice = dexterityBasePriceCents;
                multiplier = dexterityPriceMultiplier;
                break;

            case LeafUpgradeType.GraspAmount:
                basePrice = graspBasePriceCents;
                multiplier = graspPriceMultiplier;
                break;

            default:
                return -1;
        }

        double price =
            basePrice * Math.Pow(multiplier, level);

        if (double.IsNaN(price) ||
            double.IsInfinity(price) ||
            price > int.MaxValue)
            return -1;

        return Math.Max(1, (int)Math.Round(
            price, MidpointRounding.AwayFromZero));
    }

    public bool CanPurchase(LeafUpgradeType type)
    {
        int price = GetPrice(type);

        return wallet != null &&
               price >= 0 &&
               wallet.BalanceCents >= price;
    }

    public bool Purchase(LeafUpgradeType type)
    {
        if (!CanPurchase(type))
            return false;

        int price = GetPrice(type);

        if (!wallet.TrySpend(price))
            return false;

        switch (type)
        {
            case LeafUpgradeType.BagSize:
                BagLevel++;
                break;

            case LeafUpgradeType.Dexterity:
                DexterityLevel++;
                break;

            case LeafUpgradeType.GraspAmount:
                GraspLevel++;
                break;
        }

        ApplyUpgrades();
        OnUpgradesChanged?.Invoke();

        Debug.Log(
            $"Purchased {type} level {GetLevel(type)} " +
            $"for ${(price / 100m):F2}"
        );

        return true;
    }

    public void ApplyUpgrades()
    {
        if (inventory != null)
        {
            int capacity =
                startingBagCapacity +
                BagLevel * capacityPerLevel;

            inventory.SetCapacity(capacity);
        }

        if (pickupController != null)
        {
            float cooldown =
                startingCooldown -
                DexterityLevel * cooldownReductionPerLevel;

            pickupController.pickupCooldown = Mathf.Max(
                minimumCooldown, cooldown
            );

            pickupController.leavesPerPickup =
                startingGraspAmount +
                GraspLevel * graspIncreasePerLevel;
        }
    }

    public string GetEffectDescription(LeafUpgradeType type)
    {
        switch (type)
        {
            case LeafUpgradeType.BagSize:
                return $"{inventory.maxCapacity} leaves";

            case LeafUpgradeType.Dexterity:
                return $"{pickupController.pickupCooldown:F2}s cooldown";

            case LeafUpgradeType.GraspAmount:
                return $"{pickupController.leavesPerPickup} leaves per pickup";

            default:
                return "";
        }
    }
}
