
using System.Collections.Generic;
using UnityEngine;

public class LeafShopCardsUI : MonoBehaviour
{
    [Header("References")]
    public LeafUpgradeManager handUpgrades;
    public LeafRakeUpgradeManager rakeUpgrades;
    public LeafWallet wallet;

    [Header("Prefab")]
    public LeafUpgradeCardUI cardPrefab;

    [Header("Containers")]
    public Transform handContainer;
    public Transform rakeContainer;

    private struct HandCard
    {
        public LeafUpgradeType type;
        public LeafUpgradeCardUI card;
    }

    private struct RakeCard
    {
        public RakeUpgradeType type;
        public LeafUpgradeCardUI card;
    }

    private readonly List<HandCard> handCards =
        new List<HandCard>();

    private readonly List<RakeCard> rakeCards =
        new List<RakeCard>();

    private LeafUpgradeCardUI rakeUnlockCard;
    private bool rakeUpgradeCardsCreated;

    private void OnEnable()
    {
        if (wallet != null)
            wallet.OnBalanceChanged += RefreshCards;

        if (handUpgrades != null)
            handUpgrades.OnUpgradesChanged += RefreshCards;

        if (rakeUpgrades != null)
            rakeUpgrades.OnRakeUpgradesChanged += RefreshCards;

        RefreshCards();
    }

    private void OnDisable()
    {
        if (wallet != null)
            wallet.OnBalanceChanged -= RefreshCards;

        if (handUpgrades != null)
            handUpgrades.OnUpgradesChanged -= RefreshCards;

        if (rakeUpgrades != null)
            rakeUpgrades.OnRakeUpgradesChanged -= RefreshCards;
    }

    private void Start()
    {
        if (cardPrefab == null ||
            handContainer == null ||
            rakeContainer == null ||
            handUpgrades == null ||
            rakeUpgrades == null ||
            wallet == null)
        {
            Debug.LogError(
                "LeafShopCardsUI: Missing required references."
            );
            return;
        }

        CreateHandCards();
        UpdateRakeCards();
        RefreshCards();
    }

    private void CreateHandCards()
    {
        if (handCards.Count > 0)
            return;

        CreateHandCard(
            LeafUpgradeType.BagSize,
            "Bag Size",
            "Carry more leaves around."
        );

        CreateHandCard(
            LeafUpgradeType.Dexterity,
            "Dexterity",
            "Pick up leaves more quickly."
        );

        CreateHandCard(
            LeafUpgradeType.GraspAmount,
            "Grasp",
            "Pick up more leaves at a time."
        );
    }

    private void CreateHandCard(
        LeafUpgradeType type,
        string name,
        string description)
    {
        LeafUpgradeCardUI card =
            Instantiate(cardPrefab, handContainer, false);

        card.gameObject.name = name + " Upgrade Card";

        card.Initialize(
            name,
            description,
            () =>
            {
                handUpgrades.Purchase(type);
                RefreshCards();
            }
        );

        handCards.Add(new HandCard
        {
            type = type,
            card = card
        });
    }

    private void UpdateRakeCards()
    {
        if (rakeUpgrades == null ||
            rakeContainer == null ||
            cardPrefab == null)
            return;

        if (!rakeUpgrades.IsRakeOwned)
        {
            if (rakeUnlockCard == null)
                CreateRakeUnlockCard();

            return;
        }

        // Rake has been purchased.
        if (rakeUnlockCard != null)
        {
            // Hide immediately, destroy at end of frame.
            rakeUnlockCard.gameObject.SetActive(false);
            Destroy(rakeUnlockCard.gameObject);
            rakeUnlockCard = null;
        }

        if (!rakeUpgradeCardsCreated)
        {
            CreateRakeUpgradeCards();
            rakeUpgradeCardsCreated = true;
        }
    }

    private void CreateRakeUnlockCard()
    {
        rakeUnlockCard =
            Instantiate(cardPrefab, rakeContainer, false);

        rakeUnlockCard.gameObject.name =
            "Unlock Rake Card";

        rakeUnlockCard.Initialize(
            "Unlock Rake",
            "Purchase the rake to gather leaves into piles.",
            () =>
            {
                if (rakeUpgrades.PurchaseRake())
                {
                    UpdateRakeCards();
                    RefreshCards();
                }
            }
        );
    }

    private void CreateRakeUpgradeCards()
    {
        CreateRakeCard(
            RakeUpgradeType.Speed,
            "Speed",
            "Reduce the delay between rake strokes."
        );

        CreateRakeCard(
            RakeUpgradeType.Width,
            "Width",
            "Pull leaves from a wider area."
        );

        CreateRakeCard(
            RakeUpgradeType.Strength,
            "Strength",
            "Pull leaves toward you with more force."
        );
    }

    private void CreateRakeCard(
        RakeUpgradeType type,
        string name,
        string description)
    {
        LeafUpgradeCardUI card =
            Instantiate(cardPrefab, rakeContainer, false);

        card.gameObject.name = name + " Rake Upgrade Card";

        card.Initialize(
            name,
            description,
            () =>
            {
                rakeUpgrades.Purchase(type);
                RefreshCards();
            }
        );

        rakeCards.Add(new RakeCard
        {
            type = type,
            card = card
        });
    }

    public void RefreshCards()
    {
        if (handUpgrades == null ||
            rakeUpgrades == null ||
            wallet == null)
            return;

        UpdateRakeCards();

        foreach (HandCard item in handCards)
        {
            if (item.card == null)
                continue;

            item.card.Refresh(
                handUpgrades.GetNextEffectDescription(item.type),
                handUpgrades.GetPrice(item.type),
                handUpgrades.CanPurchase(item.type)
            );
        }

        if (rakeUnlockCard != null)
        {
            bool canBuy =
                !rakeUpgrades.IsRakeOwned &&
                wallet.BalanceCents >=
                rakeUpgrades.rakePriceCents;

            rakeUnlockCard.Refresh(
                "",
                rakeUpgrades.rakePriceCents,
                canBuy
            );
        }

        foreach (RakeCard item in rakeCards)
        {
            if (item.card == null)
                continue;

            item.card.Refresh(
                rakeUpgrades.GetNextEffectDescription(item.type),
                rakeUpgrades.GetPrice(item.type),
                rakeUpgrades.CanPurchase(item.type)
            );
        }
    }
}