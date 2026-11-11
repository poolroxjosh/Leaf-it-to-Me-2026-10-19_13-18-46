using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MenuActions : MonoBehaviour
{
    public string sceneToLoad;

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                MenuActions board = hit.collider.GetComponent<MenuActions>();

                if (board != null)
                {
                    SceneManager.LoadScene(board.sceneToLoad);
                }
            }
        }
    }
}