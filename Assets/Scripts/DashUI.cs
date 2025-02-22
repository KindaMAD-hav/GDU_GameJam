using UnityEngine;
using UnityEngine.UI;

public class DashUI : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement; // Drag your Player here
    public Image dashFillImage;           // Drag the Image from the Canvas

    private void Update()
    {
        // Avoid division by zero if dashCooldown is 0
        if (playerMovement.dashCooldown <= 0) return;

        // Example: fillAmount goes from 0 to 1 as the dash recharges
        // dashTimer is how much time is LEFT on cooldown
        // so we invert it: fill = 1 - (dashTimer / dashCooldown)

        float fill = 1f - (playerMovement.dashTimer / playerMovement.dashCooldown);
        dashFillImage.fillAmount = fill;
    }
}
