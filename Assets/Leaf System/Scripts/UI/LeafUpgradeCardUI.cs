
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeafUpgradeCardUI : MonoBehaviour
{
    [Header("Text References")]
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text effectText;
    public TMP_Text priceText;

    [Header("Button")]
    public Button buyButton;

    private Action purchaseAction;

    public void Initialize(
        string upgradeName,
        string description,
        Action onPurchase)
    {
        purchaseAction = onPurchase;

        if (nameText != null)
            nameText.text = upgradeName;

        if (descriptionText != null)
            descriptionText.text = description;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandlePurchase);
            buyButton.onClick.AddListener(HandlePurchase);
        }
    }

    public void Refresh(
        string effect,
        int priceCents,
        bool canPurchase)
    {
        if (effectText != null)
        {
            effectText.text = effect;
            effectText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(effect)
            );
        }

        if (priceText != null)
        {
            priceText.text = priceCents < 0
                ? "MAX"
                : $"${priceCents / 100m:F2}";
        }

        if (buyButton != null)
            buyButton.interactable = canPurchase;
    }

    private void HandlePurchase()
    {
        purchaseAction?.Invoke();
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(HandlePurchase);
    }
}