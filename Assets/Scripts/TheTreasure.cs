using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TheTreasure : MonoBehaviour
{
    [SerializeField]
    Collider col;

    [SerializeField]
    Tile myTile;

    string message = "Pick up <color=\"red\">The Treasure</color>";

    private void OnEnable()
    {
        PlayerController.OnEnterTile += PlayerController_OnEnterTile;
        myTile.NonInteractable = true;
    }

    private void OnDisable()
    {
        PlayerController.OnEnterTile += PlayerController_OnEnterTile;
        myTile.NonInteractable = false;
    }

    bool wasValid;

    private void PlayerController_OnEnterTile(PlayerController player)
    {
        var valid = myTile.Neighbours.Any(t => t == player.currentTile);
        if (valid != wasValid)
        {
            col.enabled = valid;
            if (valid)
            {
                HintUI.instance.SetText(message);
            } else
            {
                HintUI.instance.RemoveText(message);
                hovered = false;
            }

            wasValid = valid;
        }
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.performed && hovered)
        {
            SceneManager.LoadScene("WinScene");
        }
    }

    bool hovered;

    private void OnMouseEnter()
    {
        hovered = true;
    }

    private void OnMouseExit()
    {
        hovered = false;
    }
}
