using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// BlessingManager.cs
/// Singleton-like MonoBehaviour điều phối toàn bộ vòng đời của Blessing:
///   1. Lưu pool BlessingDefinition cho cả game.
///   2. Roll 3 blessing ngẫu nhiên có trọng số (weighted) mỗi sau wave.
///   3. Hiển thị UI chọn, chờ người chơi click.
///   4. Apply blessing đã chọn lên BlessingRuntimeController.
///   5. Kiểm tra unlock Ultimate (khi đủ 3 blessing thường cùng nhánh).
/// Được configure từ S03CoLoaArenaBuilder hoặc inspector.
/// </summary>
public sealed class BlessingManager : MonoBehaviour
{
    // ── Serialized Fields ────────────────────────────────────────────
    [Header("Data – danh sách tất cả Blessing trong game")]
    [SerializeField] private List<BlessingDefinition> allBlessings = new List<BlessingDefinition>();

    [Header("Runtime – BlessingRuntimeController trên Player")]
    [SerializeField] private BlessingRuntimeController playerEffects;

    [Header("Choice UI")]
    [SerializeField] private GameObject      choiceRoot;
    [SerializeField] private BlessingChoiceUI[] choiceCards;
    [SerializeField] private TMP_Text        titleText;
    [SerializeField] private TMP_Text        resultText;

    // ── Internal State ───────────────────────────────────────────────
    private readonly Dictionary<string, int> ownedStacks = new Dictionary<string, int>();
    private Action selectionComplete;
    private bool   selectionOpen;

    // ── Public Properties ────────────────────────────────────────────
    public bool IsSelectionOpen => selectionOpen;

    // ──────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Reset tất cả runtime stack khi scene load
        ownedStacks.Clear();
        foreach (BlessingDefinition b in allBlessings)
        {
            if (b != null) b.SetRuntimeStack(0);
        }

        if (choiceRoot != null)
            choiceRoot.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public Config (gọi từ builder)
    // ──────────────────────────────────────────────────────────────────
    public void Configure(
        List<BlessingDefinition>   definitions,
        BlessingRuntimeController  effects,
        GameObject                 root,
        BlessingChoiceUI[]         cards,
        TMP_Text                   title,
        TMP_Text                   result)
    {
        allBlessings  = definitions ?? new List<BlessingDefinition>();
        playerEffects = effects;
        choiceRoot    = root;
        choiceCards   = cards;
        titleText     = title;
        resultText    = result;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public – Main Entry Point
    // ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Gọi từ S03ArenaDirector sau khi wave kết thúc.
    /// Mở UI chọn Blessing, gọi onComplete khi người chơi đã chọn xong.
    /// </summary>
    public void PresentChoices(Action onComplete)
    {
        if (selectionOpen) return;

        selectionComplete = onComplete;
        List<BlessingDefinition> choices = RollDistinctChoices(3);

        // Không có blessing nào để chọn → bỏ qua
        if (choices.Count == 0)
        {
            FinishSelection();
            return;
        }

        selectionOpen = true;
        playerEffects?.SetChoiceMode(true);

        // Hiện UI backdrop + tiêu đề
        if (choiceRoot  != null) choiceRoot.SetActive(true);
        ApplyChoicePromptText();

        // Nếu không có card UI → tự chọn lựa chọn đầu tiên
        if (choiceCards == null || choiceCards.Length == 0)
        {
            SelectBlessing(choices[0]);
            return;
        }

        // Bind blessing vào từng card
        for (int i = 0; i < choiceCards.Length; i++)
        {
            BlessingChoiceUI card = choiceCards[i];
            if (card == null) continue;

            BlessingDefinition blessing = i < choices.Count ? choices[i] : null;
            card.Bind(blessing, blessing != null ? GetStack(blessing.Id) : 0, SelectBlessing);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public – Stack Query API
    // ──────────────────────────────────────────────────────────────────
    public int GetStack(string blessingId)
    {
        if (string.IsNullOrWhiteSpace(blessingId)) return 0;
        return ownedStacks.TryGetValue(blessingId, out int stack) ? stack : 0;
    }

    public int GetEffectStack(BlessingEffectType effectType)
    {
        int total = 0;
        foreach (BlessingDefinition b in allBlessings)
        {
            if (b != null && b.EffectType == effectType)
                total += GetStack(b.Id);
        }
        return total;
    }

    public bool HasBlessing(BlessingEffectType effectType) => GetEffectStack(effectType) > 0;

    // ──────────────────────────────────────────────────────────────────
    //  Private – Roll Logic (Weighted Random)
    // ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Roll `count` blessing phân biệt, có trọng số theo rarity.
    /// Chỉ lấy các blessing chưa đạt maxStack và không phải Ultimate.
    /// </summary>
    private List<BlessingDefinition> RollDistinctChoices(int count)
    {
        // Pool: các blessing chưa maxStack, không phải Ultimate
        List<BlessingDefinition> pool = allBlessings
            .Where(b => b != null && !b.IsUltimate && GetStack(b.Id) < b.MaxStack)
            .GroupBy(b => b.Id)        // Đảm bảo không trùng ID
            .Select(g => g.First())
            .ToList();

        List<BlessingDefinition> results = new List<BlessingDefinition>(count);
        while (pool.Count > 0 && results.Count < count)
        {
            int picked = PickWeightedIndex(pool);
            results.Add(pool[picked]);
            pool.RemoveAt(picked);
        }

        return results;
    }

    private static int PickWeightedIndex(IReadOnlyList<BlessingDefinition> pool)
    {
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += GetRarityWeight(pool[i].Rarity);

        float roll = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= GetRarityWeight(pool[i].Rarity);
            if (roll <= 0f) return i;
        }

        return pool.Count - 1;
    }

    private static float GetRarityWeight(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common:    return 1f;
            case BlessingRarity.Rare:      return 0.72f;
            case BlessingRarity.Epic:      return 0.42f;
            case BlessingRarity.Legendary: return 0.18f;
            default:                       return 1f;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Private – Selection Handlers
    // ──────────────────────────────────────────────────────────────────
    private void SelectBlessing(BlessingDefinition blessing)
    {
        if (!selectionOpen || blessing == null) return;

        selectionOpen = false;
        SetCardsInteractable(false);

        // Cập nhật stack
        int newStack = Mathf.Min(GetStack(blessing.Id) + 1, blessing.MaxStack);
        ownedStacks[blessing.Id] = newStack;
        blessing.SetRuntimeStack(newStack);

        // Apply lên player
        playerEffects?.ApplyBlessing(blessing, newStack);

        // Kiểm tra unlock Ultimate
        BlessingDefinition ultimate = TryUnlockUltimate(blessing.HeroType);

        // Hiện kết quả
        ApplySelectionMessage(blessing, ultimate, newStack);
        StartCoroutine(FinishAfterFeedback());
    }

    /// <summary>
    /// Kiểm tra và mở khóa Ultimate nếu người chơi có đủ 3 blessing thường của cùng nhánh.
    /// </summary>
    private BlessingDefinition TryUnlockUltimate(HeroType hero)
    {
        // Đếm số loại blessing thường của nhánh hero mà người chơi đã có
        int distinctNormal = allBlessings.Count(b =>
            b != null &&
            b.HeroType == hero &&
            !b.IsUltimate &&
            GetStack(b.Id) > 0);

        if (distinctNormal < 3) return null;

        // Tìm Ultimate của nhánh chưa được mở
        BlessingDefinition ultimate = allBlessings.FirstOrDefault(b =>
            b != null && b.HeroType == hero && b.IsUltimate);

        if (ultimate == null || GetStack(ultimate.Id) > 0) return null;

        // Mở khóa Ultimate
        ownedStacks[ultimate.Id] = 1;
        ultimate.SetRuntimeStack(1);
        playerEffects?.ApplyBlessing(ultimate, 1);
        return ultimate;
    }

    private IEnumerator FinishAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(0.65f);
        FinishSelection();
    }

    private void FinishSelection()
    {
        selectionOpen = false;
        if (choiceRoot != null) choiceRoot.SetActive(false);
        playerEffects?.SetChoiceMode(false);

        Action callback   = selectionComplete;
        selectionComplete = null;
        callback?.Invoke();
    }

    private void SetCardsInteractable(bool interactable)
    {
        if (choiceCards == null) return;
        foreach (BlessingChoiceUI card in choiceCards)
        {
            if (card != null) card.SetInteractable(interactable);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Private – UI Text Helpers
    // ──────────────────────────────────────────────────────────────────
    private void ApplyChoicePromptText()
    {
        if (titleText  != null) titleText.text  = "CHỌN CHÚC PHÚC ANH LINH";
        if (resultText != null) resultText.text = "Chọn 1 trong 3 sức mạnh để định hình lối chơi.";
    }

    private void ApplySelectionMessage(BlessingDefinition blessing, BlessingDefinition ultimate, int newStack)
    {
        if (resultText == null || blessing == null) return;

        string msg = "Đã nhận: " + blessing.Name + "  [" + newStack + "/" + blessing.MaxStack + "]";
        if (ultimate != null)
            msg += "\nMỞ KHÓA TỐI THƯỢNG: " + ultimate.Name;

        resultText.text = msg;
    }
}
