using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth3D playerHealth;
    public Slider healthSlider;

    private void Start()
    {
        if (playerHealth == null || healthSlider == null)
            return;

        healthSlider.minValue = 0;
        healthSlider.maxValue = playerHealth.maxHP;
        healthSlider.value = playerHealth.currentHP;
    }

    private void Update()
    {
        if (playerHealth == null || healthSlider == null)
            return;

        healthSlider.maxValue = playerHealth.maxHP;
        healthSlider.value = playerHealth.currentHP;
    }
}