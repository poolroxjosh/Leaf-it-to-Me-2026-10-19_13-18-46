using UnityEngine;
using UnityEngine.InputSystem;

public class ClickPanel : MonoBehaviour
{
    public GameObject panel;

    private void Update()
    {
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (Physics.Raycast(ray, out RaycastHit hit) &&
            hit.collider.GetComponentInParent<ClickPanel>() == this)
            panel.SetActive(!panel.activeSelf);
    }
}