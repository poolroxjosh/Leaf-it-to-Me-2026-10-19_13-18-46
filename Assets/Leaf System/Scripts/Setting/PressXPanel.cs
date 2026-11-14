using UnityEngine;
using UnityEngine.InputSystem;

public class PressXPanel : MonoBehaviour
{
    public GameObject panel;
    public GameObject viewInstructionsText;

    void Start()
    {
        UpdateInstructionsVisibility();
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.xKey.wasPressedThisFrame)
        {
            panel.SetActive(!panel.activeSelf);
            UpdateInstructionsVisibility();
        }
    }

    void UpdateInstructionsVisibility()
    {
        viewInstructionsText.SetActive(!panel.activeSelf);
    }
}