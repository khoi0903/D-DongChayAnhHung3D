using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BlessingChoiceUI.cs
/// Component gắn trên mỗi card chọn Blessing trong UI.
/// Nhận BlessingDefinition và hiển thị: tên anh hùng, tên blessing,
/// mô tả, độ hiếm, số stack hiện tại.
/// Xử lý click → gọi callback lên BlessingManager.
/// </summary>
public sealed class BlessingChoiceUI : MonoBehaviour
{
    // ── Serialized References ────────────────────────────────────────
    [SerializeField] private Button    button;
    [SerializeField] private Image     frame;
    [SerializeField] private Image     icon;
    [SerializeField] private TMP_Text  heroText;
    [SerializeField] private TMP_Text  nameText;
    [SerializeField] private TMP_Text  descriptionText;
    [SerializeField] private TMP_Text  rarityText;
    [SerializeField] private TMP_Text  stackText;

    // ── Internal State ───────────────────────────────────────────────
    private BlessingDefinition             definition;
    private Action<BlessingDefinition>     onSelected;

    // ──────────────────────────────────────────────────────────────────
    //  Public Config (gọi từ S03CoLoaArenaBuilder)
    // ──────────────────────────────────────────────────────────────────
    public void ConfigureReferences(
        Button    choiceButton,
        Image     choiceFrame,
        Image     choiceIcon,
        TMP_Text  choiceHero,
        TMP_Text  choiceName,
        TMP_Text  choiceDescription,
        TMP_Text  choiceRarity,
        TMP_Text  choiceStack)
    {
        button          = choiceButton;
        frame           = choiceFrame;
        icon            = choiceIcon;
        heroText        = choiceHero;
        nameText        = choiceName;
        descriptionText = choiceDescription;
        rarityText      = choiceRarity;
        stackText       = choiceStack;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public – Bind Blessing vào Card
    // ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Bind một BlessingDefinition vào card này để hiển thị.
    /// </summary>
    /// <param name="blessing">Blessing cần hiển thị (null → ẩn card).</param>
    /// <param name="ownedStack">Số stack người chơi đang có.</param>
    /// <param name="selectionCallback">Callback khi người chơi click chọn.</param>
    public void Bind(BlessingDefinition blessing, int ownedStack, Action<BlessingDefinition> selectionCallback)
    {
        definition = blessing;
        onSelected = selectionCallback;

        // Wire up button
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
            button.interactable = blessing != null;
        }

        // Ẩn card nếu không có blessing
        if (blessing == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Tính màu theo nhánh anh hùng và độ hiếm
        Color heroColor   = GetHeroColor(blessing.HeroType);
        Color rarityColor = GetRarityColor(blessing.Rarity);

        // Frame: blend màu anh hùng + rarity
        if (frame != null)
            frame.color = Color.Lerp(heroColor, rarityColor, 0.42f);

        // Icon sprite
        if (icon != null)
        {
            icon.sprite = blessing.Icon;
            icon.color  = blessing.Icon != null ? Color.white : heroColor;
        }

        // Text (dùng safe helper để đảm bảo encode đúng Unicode)
        ApplySafeDisplayText(blessing, ownedStack);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public – Interactable Toggle
    // ──────────────────────────────────────────────────────────────────
    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable && definition != null;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Private – Click Handler
    // ──────────────────────────────────────────────────────────────────
    private void Select()
    {
        if (definition == null) return;
        if (button != null) button.interactable = false;
        onSelected?.Invoke(definition);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Private – Text Helpers (Unicode-safe)
    // ──────────────────────────────────────────────────────────────────
    private void ApplySafeDisplayText(BlessingDefinition blessing, int ownedStack)
    {
        if (blessing == null) return;

        if (heroText        != null) heroText.text        = GetSafeHeroName(blessing.HeroType);
        if (nameText        != null) nameText.text        = blessing.Name;
        if (descriptionText != null) descriptionText.text = blessing.Description;
        if (rarityText      != null) rarityText.text      = GetSafeRarityName(blessing.Rarity);

        if (stackText != null)
        {
            int nextStack = Mathf.Min(ownedStack + 1, blessing.MaxStack);
            stackText.text = blessing.MaxStack > 1
                ? "B\u1eadc " + nextStack + "/" + blessing.MaxStack
                : (blessing.IsUltimate ? "T\u1ed1i th\u01b0\u1ee3ng" : "\u0110\u1ed9c nh\u1ea5t");
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Static Color + Name Helpers (dùng được từ code khác)
    // ──────────────────────────────────────────────────────────────────
    private static string GetSafeHeroName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return "AN D\u01af\u01a0NG V\u01af\u01a0NG";
            case HeroType.TrungTrac:    return "TR\u01afNG TR\u1eaeC";
            case HeroType.TrungNhi:     return "TR\u01afNG NH\u1eca";
            case HeroType.QuangTrung:   return "QUANG TRUNG";
            default:                    return hero.ToString().ToUpperInvariant();
        }
    }

    private static string GetSafeRarityName(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common:    return "TH\u01af\u1edcNG";
            case BlessingRarity.Rare:      return "HI\u1ebeM";
            case BlessingRarity.Epic:      return "S\u1eec THI";
            case BlessingRarity.Legendary: return "HUY\u1ec0N THO\u1ea0I";
            default:                       return rarity.ToString().ToUpperInvariant();
        }
    }

    public static string GetHeroName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return "AN DƯƠNG VƯƠNG";
            case HeroType.TrungTrac:    return "TRƯNG TRẮC";
            case HeroType.TrungNhi:     return "TRƯNG NHỊ";
            case HeroType.QuangTrung:   return "QUANG TRUNG";
            default:                    return hero.ToString().ToUpperInvariant();
        }
    }

    public static Color GetHeroColor(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return new Color(0.12f, 0.68f, 0.88f, 1f); // Xanh dương
            case HeroType.TrungTrac:    return new Color(0.82f, 0.18f, 0.32f, 1f); // Đỏ
            case HeroType.TrungNhi:     return new Color(0.25f, 0.82f, 0.58f, 1f); // Xanh lá
            case HeroType.QuangTrung:   return new Color(0.96f, 0.62f, 0.12f, 1f); // Cam
            default:                    return Color.white;
        }
    }

    public static Color GetRarityColor(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common:    return new Color(0.72f, 0.76f, 0.8f, 1f);  // Xám bạc
            case BlessingRarity.Rare:      return new Color(0.18f, 0.58f, 1f,    1f);  // Xanh
            case BlessingRarity.Epic:      return new Color(0.68f, 0.24f, 0.96f, 1f);  // Tím
            case BlessingRarity.Legendary: return new Color(1f,    0.62f, 0.08f, 1f);  // Vàng
            default:                       return Color.white;
        }
    }
}
