using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LoopBackToMainMenu : MonoBehaviour
{
    public void Interact(InputAction.CallbackContext context)
    {

        if (context.performed)
        {
            SceneManager.LoadScene("TitleScene");
        }
    }
}
