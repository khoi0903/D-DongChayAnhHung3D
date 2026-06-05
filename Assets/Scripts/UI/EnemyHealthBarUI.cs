using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    public EnemyHealth3D enemyHealth;
    public Slider healthSlider;
    public Transform cameraTransform;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (enemyHealth != null && healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = enemyHealth.maxHP;
            healthSlider.value = enemyHealth.currentHP;
        }
    }

    private void LateUpdate()
    {
        if (enemyHealth == null || healthSlider == null)
            return;

        healthSlider.maxValue = enemyHealth.maxHP;
        healthSlider.value = enemyHealth.currentHP;

        if (cameraTransform != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - cameraTransform.position
            );
        }
    }
}