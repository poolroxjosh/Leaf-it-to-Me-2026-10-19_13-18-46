
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class LeafShopUI : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeRow
    {
        public TMP_Text levelText;
        public TMP_Text effectText;
        public TMP_Text priceText;
        public Button buyButton;
    }

    [Header("References")]
    public LeafWallet wallet;
    public LeafUpgradeManager upgrades;
    public LeafRakeUpgradeManager rakeUpgrades;

    [Header("Main UI")]
    public GameObject shopPanel;
    public TMP_Text hudMoneyText;
    public TMP_Text shopBalanceText;

    [Header("Tabs")]
    public GameObject handUpgradesPanel;
    public GameObject rakeUpgradesPanel;
    public Button handTabButton;
    public Button rakeTabButton;

    [Header("Hand Upgrades")]
    public UpgradeRow bagRow;
    public UpgradeRow dexterityRow;
    public UpgradeRow graspRow;

    [Header("Rake Purchase")]
    public GameObject rakePurchasePanel;
    public GameObject rakeUpgradeRows;
    public Button purchaseRakeButton;
    public TMP_Text rakePurchasePriceText;

    [Header("Rake Upgrades")]
    public UpgradeRow rakeSpeedRow;
    public UpgradeRow rakeWidthRow;
    public UpgradeRow rakeStrengthRow;

    [Header("Wheelbarrow Movement")]
    public WheelbarrowMovementController wheelbarrowMovement;

    [Header("Tab Appearance")]
    public Color activeTabColor =
        new Color32(181, 166, 151, 255);

    public Color inactiveTabColor =
        new Color32(255, 249, 239, 255);

    public bool IsOpen { get; private set; }

    private bool showingRakeTab;

    private void Awake()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (wallet != null)
            wallet.OnBalanceChanged += Refresh;

        if (upgrades != null)
            upgrades.OnUpgradesChanged += Refresh;

        if (rakeUpgrades != null)
            rakeUpgrades.OnRakeUpgradesChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.OnBalanceChanged -= Refresh;

        if (upgrades != null)
            upgrades.OnUpgradesChanged -= Refresh;

        if (rakeUpgrades != null)
            rakeUpgrades.OnRakeUpgradesChanged -= Refresh;
    }

    private void Start()
    {
        SetupButtons();
        ShowHandTab();
        Refresh();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleShop();
        }
    }

    private void SetupButtons()
    {
        if (handTabButton != null)
            handTabButton.onClick.AddListener(ShowHandTab);

        if (rakeTabButton != null)
            rakeTabButton.onClick.AddListener(ShowRakeTab);

        if (bagRow.buyButton != null)
            bagRow.buyButton.onClick.AddListener(
                () => PurchaseHand(LeafUpgradeType.BagSize));

        if (dexterityRow.buyButton != null)
            dexterityRow.buyButton.onClick.AddListener(
                () => PurchaseHand(LeafUpgradeType.Dexterity));

        if (graspRow.buyButton != null)
            graspRow.buyButton.onClick.AddListener(
                () => PurchaseHand(LeafUpgradeType.GraspAmount));

        if (purchaseRakeButton != null)
            purchaseRakeButton.onClick.AddListener(PurchaseRake);

        if (rakeSpeedRow.buyButton != null)
            rakeSpeedRow.buyButton.onClick.AddListener(
                () => PurchaseRakeUpgrade(RakeUpgradeType.Speed));

        if (rakeWidthRow.buyButton != null)
            rakeWidthRow.buyButton.onClick.AddListener(
                () => PurchaseRakeUpgrade(RakeUpgradeType.Width));

        if (rakeStrengthRow.buyButton != null)
            rakeStrengthRow.buyButton.onClick.AddListener(
                () => PurchaseRakeUpgrade(RakeUpgradeType.Strength));
    }

    public void ToggleShop()
    {
        if (wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return;

        SetShopOpen(!IsOpen);
    }

    public void CloseShop()
    {
        SetShopOpen(false);
    }

    public void SetShopOpen(bool open)
    {
        if (open &&
            wheelbarrowMovement != null &&
            wheelbarrowMovement.IsPushing)
            return;
        IsOpen = open;

        if (shopPanel != null)
            shopPanel.SetActive(open);

        Cursor.lockState = open
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = open;

        Refresh();
    }

    public void ShowHandTab()
    {
        showingRakeTab = false;
        RefreshTabs();
    }

    public void ShowRakeTab()
    {
        showingRakeTab = true;
        RefreshTabs();
    }
    
    private void RefreshTabs()
    {
        // Switch between Hand and Rake content.
        if (handUpgradesPanel != null)
            handUpgradesPanel.SetActive(!showingRakeTab);

        if (rakeUpgradesPanel != null)
            rakeUpgradesPanel.SetActive(showingRakeTab);

        // Highlight the currently selected tab.
        if (handTabButton != null)
        {
            handTabButton.image.color =
                showingRakeTab
                    ? inactiveTabColor
                    : activeTabColor;
        }

        if (rakeTabButton != null)
        {
            rakeTabButton.image.color =
                showingRakeTab
                    ? activeTabColor
                    : inactiveTabColor;
        }

        Refresh();
    }

    private void PurchaseHand(LeafUpgradeType type)
    {
        if (upgrades == null)
            return;

        upgrades.Purchase(type);
        Refresh();
    }

    private void PurchaseRake()
    {
        if (rakeUpgrades == null)
            return;

        rakeUpgrades.PurchaseRake();
        Refresh();
    }

    private void PurchaseRakeUpgrade(RakeUpgradeType type)
    {
        if (rakeUpgrades == null)
            return;

        rakeUpgrades.Purchase(type);
        Refresh();
    }

    private void Refresh()
    {
        if (wallet != null)
        {
            string balance = wallet.GetFormattedBalance();

            if (hudMoneyText != null)
                hudMoneyText.text = balance;

            if (shopBalanceText != null)
                shopBalanceText.text = $"Balance: {balance}";
        }

        if (upgrades != null)
        {
            RefreshHandRow(bagRow, LeafUpgradeType.BagSize);
            RefreshHandRow(dexterityRow, LeafUpgradeType.Dexterity);
            RefreshHandRow(graspRow, LeafUpgradeType.GraspAmount);
        }
    }

    private void RefreshHandRow(
        UpgradeRow row,
        LeafUpgradeType type)
    {
        int level = upgrades.GetLevel(type);
        int price = upgrades.GetPrice(type);

        if (row.levelText != null)
            row.levelText.text = $"Level {level}";

        if (row.effectText != null)
            row.effectText.text =
                upgrades.GetEffectDescription(type);

        if (row.priceText != null)
            row.priceText.text = price < 0
                ? "MAX"
                : $"${price / 100m:F2}";

        if (row.buyButton != null)
            row.buyButton.interactable =
                upgrades.CanPurchase(type);
    }

    private void RefreshRakeRow(
        UpgradeRow row,
        RakeUpgradeType type)
    {
        int level = rakeUpgrades.GetLevel(type);
        int price = rakeUpgrades.GetPrice(type);

        if (row.levelText != null)
            row.levelText.text = $"Level {level}";

        if (row.effectText != null)
            row.effectText.text =
                rakeUpgrades.GetEffectDescription(type);

        if (row.priceText != null)
            row.priceText.text = price < 0
                ? "MAX"
                : $"${price / 100m:F2}";

        if (row.buyButton != null)
            row.buyButton.interactable =
                rakeUpgrades.CanPurchase(type);
    }
}