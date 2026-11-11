
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeafBagUI : MonoBehaviour
{
    public LeafInventory inventory;
    public Image leafFillImage;
    public TMP_Text capacityText;

    [Header("Counter Colors")]
    public Color normalTextColor = Color.white;

    public Color fullTextColor =
        new Color(1f, 0.25f, 0.2f);

    [Min(0.1f)]
    public float fillSmoothSpeed = 5f;

    private float targetFill;
    private float currentFill;

    private void OnEnable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (inventory == null)
            return;

        int count =
            inventory.CurrentLeaves +
            inventory.PendingLeaves;

        int capacity = Mathf.Max(1, inventory.maxCapacity);

        targetFill = Mathf.Clamp01(
            (float)count / capacity
        );

        if (capacityText != null)
        {
            capacityText.text = count.ToString();

            capacityText.color = count >= capacity
                ? fullTextColor
                : normalTextColor;
        }
    }

    private void Update()
    {
        currentFill = Mathf.MoveTowards(
            currentFill,
            targetFill,
            fillSmoothSpeed * Time.deltaTime
        );

        if (leafFillImage != null)
            leafFillImage.fillAmount = currentFill;
    }
}