using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PlayerHealthUI.cs
/// Cập nhật thanh máu player trên UI mỗi frame:
///   - Đồng bộ giá trị Slider theo currentHP / maxHP.
///   - Hiển thị text "currentHP / maxHP".
///   - Đổi màu thanh fill từ đỏ tươi (máu đầy) sang đỏ tối (máu thấp).
/// Gắn vào root panel của thanh máu player trong Canvas.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    // ── Serialized References ────────────────────────────────────────
    [Tooltip("PlayerHealth3D trên Player (tự resolve nếu null).")]
    public PlayerHealth3D playerHealth;

    [Tooltip("Slider hiển thị phần trăm máu.")]
    public Slider healthSlider;

    [Tooltip("Text hiển thị dạng 'currentHP / maxHP'.")]
    public TMP_Text healthText;

    [Tooltip("Image fill của slider để đổi màu theo máu còn lại.")]
    public Image fillImage;

    [Header("Màu sắc thanh máu")]
    [Tooltip("Màu khi máu đầy hoặc gần đầy.")]
    public Color healthyColor = new Color(0.86f, 0.1f, 0.13f, 1f);

    [Tooltip("Màu khi máu thấp (nguy hiểm).")]
    public Color dangerColor  = new Color(0.55f, 0.02f, 0.04f, 1f);

    // ──────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Tự tìm PlayerHealth3D nếu chưa gán
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth3D>();

        if (playerHealth == null || healthSlider == null)
            return;

        healthSlider.minValue = 0;
        healthSlider.maxValue = playerHealth.maxHP;
        Refresh();
    }

    private void Update()
    {
        if (playerHealth == null || healthSlider == null)
            return;

        Refresh();
    }

    // ──────────────────────────────────────────────────────────────────
    //  Private – UI Refresh
    // ──────────────────────────────────────────────────────────────────
    private void Refresh()
    {
        healthSlider.maxValue = playerHealth.maxHP;
        healthSlider.value    = playerHealth.currentHP;

        // Cập nhật text HP
        if (healthText != null)
            healthText.text = playerHealth.currentHP + " / " + playerHealth.maxHP;

        // Đổi màu fill theo tỉ lệ máu
        if (fillImage != null && playerHealth.maxHP > 0)
        {
            float normalizedHealth = Mathf.Clamp01(playerHealth.currentHP / (float)playerHealth.maxHP);
            fillImage.color = Color.Lerp(dangerColor, healthyColor, normalizedHealth);
        }
    }
}