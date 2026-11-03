
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

    [Header("UI")]
    public GameObject shopPanel;
    public TMP_Text hudMoneyText;
    public TMP_Text shopBalanceText;

    [Header("Upgrade Rows")]
    public UpgradeRow bagRow;
    public UpgradeRow dexterityRow;
    public UpgradeRow graspRow;

    public bool IsOpen { get; private set; }

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

        Refresh();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.OnBalanceChanged -= Refresh;

        if (upgrades != null)
            upgrades.OnUpgradesChanged -= Refresh;
    }

    private void Start()
    {
        SetupButtons();
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
        if (bagRow.buyButton != null)
        {
            bagRow.buyButton.onClick.AddListener(
                () => Purchase(LeafUpgradeType.BagSize)
            );
        }

        if (dexterityRow.buyButton != null)
        {
            dexterityRow.buyButton.onClick.AddListener(
                () => Purchase(LeafUpgradeType.Dexterity)
            );
        }

        if (graspRow.buyButton != null)
        {
            graspRow.buyButton.onClick.AddListener(
                () => Purchase(LeafUpgradeType.GraspAmount)
            );
        }
    }

    public void ToggleShop()
    {
        SetShopOpen(!IsOpen);
    }

    public void CloseShop()
    {
        SetShopOpen(false);
    }

    public void SetShopOpen(bool open)
    {
        IsOpen = open;

        if (shopPanel != null)
            shopPanel.SetActive(open);

        Cursor.lockState = open
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = open;

        Refresh();
    }

    private void Purchase(LeafUpgradeType type)
    {
        if (upgrades == null)
            return;

        upgrades.Purchase(type);
        Refresh();
    }

    private void Refresh()
    {
        if (wallet == null)
            return;

        string balance = wallet.GetFormattedBalance();

        if (hudMoneyText != null)
            hudMoneyText.text = balance;

        if (shopBalanceText != null)
            shopBalanceText.text = $"Balance: {balance}";

        if (upgrades == null)
            return;

        RefreshRow(bagRow, LeafUpgradeType.BagSize);
        RefreshRow(dexterityRow, LeafUpgradeType.Dexterity);
        RefreshRow(graspRow, LeafUpgradeType.GraspAmount);
    }

    private void RefreshRow(
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
        {
            row.priceText.text = price < 0
                ? "MAX"
                : $"${(price / 100m):F2}";
        }

        if (row.buyButton != null)
            row.buyButton.interactable =
                upgrades.CanPurchase(type);
    }
}
