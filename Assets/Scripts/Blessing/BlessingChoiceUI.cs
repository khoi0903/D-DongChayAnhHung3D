using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BlessingChoiceUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text heroText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text stackText;

    private BlessingDefinition definition;
    private Action<BlessingDefinition> onSelected;

    public void ConfigureReferences(
        Button choiceButton,
        Image choiceFrame,
        Image choiceIcon,
        TMP_Text choiceHero,
        TMP_Text choiceName,
        TMP_Text choiceDescription,
        TMP_Text choiceRarity,
        TMP_Text choiceStack)
    {
        button = choiceButton;
        frame = choiceFrame;
        icon = choiceIcon;
        heroText = choiceHero;
        nameText = choiceName;
        descriptionText = choiceDescription;
        rarityText = choiceRarity;
        stackText = choiceStack;
    }

    public void Bind(BlessingDefinition blessing, int ownedStack, Action<BlessingDefinition> selectionCallback)
    {
        definition = blessing;
        onSelected = selectionCallback;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
            button.interactable = blessing != null;
        }

        if (blessing == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        Color heroColor = GetHeroColor(blessing.HeroType);
        Color rarityColor = GetRarityColor(blessing.Rarity);

        if (frame != null)
            frame.color = Color.Lerp(heroColor, rarityColor, 0.42f);

        if (icon != null)
        {
            icon.sprite = blessing.Icon;
            icon.color = blessing.Icon != null ? Color.white : heroColor;
        }

        if (heroText != null)
            heroText.text = GetHeroName(blessing.HeroType);
        if (nameText != null)
            nameText.text = blessing.Name;
        if (descriptionText != null)
            descriptionText.text = blessing.Description;
        if (rarityText != null)
        {
            rarityText.text = GetRarityName(blessing.Rarity);
            rarityText.color = rarityColor;
        }

        if (stackText != null)
        {
            int nextStack = Mathf.Min(ownedStack + 1, blessing.MaxStack);
            stackText.text = blessing.MaxStack > 1
                ? "Bậc " + nextStack + "/" + blessing.MaxStack
                : (blessing.IsUltimate ? "Tối thượng" : "Độc nhất");
        }

        ApplySafeDisplayText(blessing, ownedStack);
    }

    private void Select()
    {
        if (definition == null)
            return;

        if (button != null)
            button.interactable = false;
        onSelected?.Invoke(definition);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable && definition != null;
    }

    private void ApplySafeDisplayText(BlessingDefinition blessing, int ownedStack)
    {
        if (blessing == null)
            return;

        if (heroText != null)
            heroText.text = GetSafeHeroName(blessing.HeroType);
        if (nameText != null)
            nameText.text = blessing.Name;
        if (descriptionText != null)
            descriptionText.text = blessing.Description;
        if (rarityText != null)
            rarityText.text = GetSafeRarityName(blessing.Rarity);
        if (stackText != null)
        {
            int nextStack = Mathf.Min(ownedStack + 1, blessing.MaxStack);
            stackText.text = blessing.MaxStack > 1
                ? "B\u1eadc " + nextStack + "/" + blessing.MaxStack
                : (blessing.IsUltimate ? "T\u1ed1i th\u01b0\u1ee3ng" : "\u0110\u1ed9c nh\u1ea5t");
        }
    }

    private static string GetSafeHeroName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return "AN D\u01af\u01a0NG V\u01af\u01a0NG";
            case HeroType.TrungTrac: return "TR\u01afNG TR\u1eaeC";
            case HeroType.TrungNhi: return "TR\u01afNG NH\u1eca";
            case HeroType.QuangTrung: return "QUANG TRUNG";
            default: return hero.ToString().ToUpperInvariant();
        }
    }

    private static string GetSafeRarityName(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return "TH\u01af\u1edcNG";
            case BlessingRarity.Rare: return "HI\u1ebeM";
            case BlessingRarity.Epic: return "S\u1eec THI";
            case BlessingRarity.Legendary: return "HUY\u1ec0N THO\u1ea0I";
            default: return rarity.ToString().ToUpperInvariant();
        }
    }

    public static string GetHeroName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return "AN DƯƠNG VƯƠNG";
            case HeroType.TrungTrac: return "TRƯNG TRẮC";
            case HeroType.TrungNhi: return "TRƯNG NHỊ";
            case HeroType.QuangTrung: return "QUANG TRUNG";
            default: return hero.ToString().ToUpperInvariant();
        }
    }

    public static Color GetHeroColor(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.AnDuongVuong: return new Color(0.12f, 0.68f, 0.88f, 1f);
            case HeroType.TrungTrac: return new Color(0.82f, 0.18f, 0.32f, 1f);
            case HeroType.TrungNhi: return new Color(0.25f, 0.82f, 0.58f, 1f);
            case HeroType.QuangTrung: return new Color(0.96f, 0.62f, 0.12f, 1f);
            default: return Color.white;
        }
    }

    public static Color GetRarityColor(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return new Color(0.72f, 0.76f, 0.8f, 1f);
            case BlessingRarity.Rare: return new Color(0.18f, 0.58f, 1f, 1f);
            case BlessingRarity.Epic: return new Color(0.68f, 0.24f, 0.96f, 1f);
            case BlessingRarity.Legendary: return new Color(1f, 0.62f, 0.08f, 1f);
            default: return Color.white;
        }
    }

    private static string GetRarityName(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return "THƯỜNG";
            case BlessingRarity.Rare: return "HIẾM";
            case BlessingRarity.Epic: return "SỬ THI";
            case BlessingRarity.Legendary: return "HUYỀN THOẠI";
            default: return rarity.ToString().ToUpperInvariant();
        }
    }
}
