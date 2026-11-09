
using System;
using UnityEngine;

public enum RakeUpgradeType
{
    Speed,
    Width,
    Strength
}

public class LeafRakeUpgradeManager : MonoBehaviour
{
    [Header("References")]
    public LeafWallet wallet;
    public PlayerToolController toolController;
    public LeafRakeController rakeController;

    [Header("Rake Purchase")]
    public int rakePriceCents = 500;

    [Header("Speed")]
    public float startingCooldown = 0.8f;
    public float cooldownReduction = 0.06f;
    public float minimumCooldown = 0.2f;
    public int speedBasePrice = 150;

    [Header("Width")]
    public float startingWidth = 1.2f;
    public float widthPerLevel = 0.15f;
    public int widthBasePrice = 200;

    [Header("Strength")]
    public float startingPullDistance = 0.6f;
    public float pullDistancePerLevel = 0.2f;
    public int strengthBasePrice = 250;

    [Header("Pricing")]
    public float priceMultiplier = 1.6f;
    public int maxUpgradeLevel = 10;

    public int SpeedLevel { get; private set; }
    public int WidthLevel { get; private set; }
    public int StrengthLevel { get; private set; }

    public bool IsRakeOwned =>
        toolController != null && toolController.HasRake;

    public event Action OnRakeUpgradesChanged;

    private void Start()
    {
        ApplyUpgrades();
    }

    public bool PurchaseRake()
    {
        if (IsRakeOwned || wallet == null ||
            toolController == null)
            return false;

        if (!wallet.TrySpend(rakePriceCents))
            return false;

        toolController.UnlockRake();
        ApplyUpgrades();

        OnRakeUpgradesChanged?.Invoke();
        return true;
    }

    public int GetLevel(RakeUpgradeType type)
    {
        switch (type)
        {
            case RakeUpgradeType.Speed:
                return SpeedLevel;

            case RakeUpgradeType.Width:
                return WidthLevel;

            case RakeUpgradeType.Strength:
                return StrengthLevel;
        }

        return 0;
    }

    public int GetPrice(RakeUpgradeType type)
    {
        int level = GetLevel(type);

        if (level >= maxUpgradeLevel)
            return -1;

        int basePrice = type == RakeUpgradeType.Speed
            ? speedBasePrice
            : type == RakeUpgradeType.Width
                ? widthBasePrice
                : strengthBasePrice;

        double price = basePrice *
            Math.Pow(Math.Max(1f, priceMultiplier), level);

        if (double.IsNaN(price) ||
            double.IsInfinity(price) ||
            price > int.MaxValue)
            return -1;

        return Math.Max(
            1,
            (int)Math.Round(
                price,
                MidpointRounding.AwayFromZero
            )
        );
    }

    public bool CanPurchase(RakeUpgradeType type)
    {
        int price = GetPrice(type);

        return IsRakeOwned &&
               wallet != null &&
               price >= 0 &&
               wallet.BalanceCents >= price;
    }

    public bool Purchase(RakeUpgradeType type)
    {
        if (!CanPurchase(type))
            return false;

        int price = GetPrice(type);

        if (!wallet.TrySpend(price))
            return false;

        switch (type)
        {
            case RakeUpgradeType.Speed:
                SpeedLevel++;
                break;

            case RakeUpgradeType.Width:
                WidthLevel++;
                break;

            case RakeUpgradeType.Strength:
                StrengthLevel++;
                break;
        }

        ApplyUpgrades();
        OnRakeUpgradesChanged?.Invoke();

        return true;
    }

    public void ApplyUpgrades()
    {
        if (rakeController == null)
            return;

        rakeController.strokeCooldown = Mathf.Max(
            minimumCooldown,
            startingCooldown - SpeedLevel * cooldownReduction
        );

        rakeController.rakeWidth =
            startingWidth + WidthLevel * widthPerLevel;

        rakeController.pullDistance =
            startingPullDistance +
            StrengthLevel * pullDistancePerLevel;
    }

    public string GetEffectDescription(RakeUpgradeType type)
    {
        if (rakeController == null)
            return "";

        switch (type)
        {
            case RakeUpgradeType.Speed:
                return $"{rakeController.strokeCooldown:F2}s cooldown";

            case RakeUpgradeType.Width:
                return $"{rakeController.rakeWidth:F2}m width";

            case RakeUpgradeType.Strength:
                return $"{rakeController.pullDistance:F2}m pull";
        }

        return "";
    }
}