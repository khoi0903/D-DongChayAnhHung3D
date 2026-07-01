using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public sealed class BlessingManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<BlessingDefinition> allBlessings = new List<BlessingDefinition>();

    [Header("Runtime")]
    [SerializeField] private BlessingRuntimeController playerEffects;

    [Header("Choice UI")]
    [SerializeField] private GameObject choiceRoot;
    [SerializeField] private BlessingChoiceUI[] choiceCards;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text resultText;

    private readonly Dictionary<string, int> ownedStacks = new Dictionary<string, int>();
    private Action selectionComplete;
    private bool selectionOpen;

    public bool IsSelectionOpen => selectionOpen;

    private void Awake()
    {
        ownedStacks.Clear();
        foreach (BlessingDefinition blessing in allBlessings)
        {
            if (blessing != null)
                blessing.SetRuntimeStack(0);
        }

        if (choiceRoot != null)
            choiceRoot.SetActive(false);
    }

    public void Configure(
        List<BlessingDefinition> definitions,
        BlessingRuntimeController effects,
        GameObject root,
        BlessingChoiceUI[] cards,
        TMP_Text title,
        TMP_Text result)
    {
        allBlessings = definitions ?? new List<BlessingDefinition>();
        playerEffects = effects;
        choiceRoot = root;
        choiceCards = cards;
        titleText = title;
        resultText = result;
    }

    public void PresentChoices(Action onComplete)
    {
        if (selectionOpen)
            return;

        selectionComplete = onComplete;
        List<BlessingDefinition> choices = RollDistinctChoices(3);
        if (choices.Count == 0)
        {
            FinishSelection();
            return;
        }

        selectionOpen = true;
        playerEffects?.SetChoiceMode(true);

        if (choiceRoot != null)
            choiceRoot.SetActive(true);
        if (titleText != null)
            titleText.text = "CHỌN CHÚC PHÚC ANH LINH";
        if (resultText != null)
            resultText.text = "Chọn 1 trong 3 sức mạnh để định hình lối chơi.";

        ApplyChoicePromptText();

        if (choiceCards == null || choiceCards.Length == 0)
        {
            SelectBlessing(choices[0]);
            return;
        }

        for (int i = 0; i < choiceCards.Length; i++)
        {
            BlessingChoiceUI card = choiceCards[i];
            if (card == null)
                continue;

            BlessingDefinition blessing = i < choices.Count ? choices[i] : null;
            card.Bind(blessing, blessing != null ? GetStack(blessing.Id) : 0, SelectBlessing);
        }
    }

    public int GetStack(string blessingId)
    {
        if (string.IsNullOrWhiteSpace(blessingId))
            return 0;
        return ownedStacks.TryGetValue(blessingId, out int stack) ? stack : 0;
    }

    public int GetEffectStack(BlessingEffectType effectType)
    {
        int total = 0;
        foreach (BlessingDefinition blessing in allBlessings)
        {
            if (blessing != null && blessing.EffectType == effectType)
                total += GetStack(blessing.Id);
        }
        return total;
    }

    public bool HasBlessing(BlessingEffectType effectType)
    {
        return GetEffectStack(effectType) > 0;
    }

    private List<BlessingDefinition> RollDistinctChoices(int count)
    {
        List<BlessingDefinition> pool = allBlessings
            .Where(blessing => blessing != null && !blessing.IsUltimate && GetStack(blessing.Id) < blessing.MaxStack)
            .GroupBy(blessing => blessing.Id)
            .Select(group => group.First())
            .ToList();

        List<BlessingDefinition> results = new List<BlessingDefinition>(count);
        while (pool.Count > 0 && results.Count < count)
        {
            int pickedIndex = PickWeightedIndex(pool);
            results.Add(pool[pickedIndex]);
            pool.RemoveAt(pickedIndex);
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
            if (roll <= 0f)
                return i;
        }

        return pool.Count - 1;
    }

    private static float GetRarityWeight(BlessingRarity rarity)
    {
        switch (rarity)
        {
            case BlessingRarity.Common: return 1f;
            case BlessingRarity.Rare: return 0.72f;
            case BlessingRarity.Epic: return 0.42f;
            case BlessingRarity.Legendary: return 0.18f;
            default: return 1f;
        }
    }

    private void SelectBlessing(BlessingDefinition blessing)
    {
        if (!selectionOpen || blessing == null)
            return;

        selectionOpen = false;
        SetCardsInteractable(false);

        int newStack = Mathf.Min(GetStack(blessing.Id) + 1, blessing.MaxStack);
        ownedStacks[blessing.Id] = newStack;
        blessing.SetRuntimeStack(newStack);
        playerEffects?.ApplyBlessing(blessing, newStack);

        BlessingDefinition ultimate = TryUnlockUltimate(blessing.HeroType);
        string message = "Đã nhận: " + blessing.Name + "  [" + newStack + "/" + blessing.MaxStack + "]";
        if (ultimate != null)
            message += "\nMỞ KHÓA TỐI THƯỢNG: " + ultimate.Name;

        if (resultText != null)
            resultText.text = message;

        ApplySelectionMessage(blessing, ultimate, newStack);
        StartCoroutine(FinishAfterFeedback());
    }

    private BlessingDefinition TryUnlockUltimate(HeroType hero)
    {
        int distinctNormalBlessings = allBlessings.Count(blessing =>
            blessing != null &&
            blessing.HeroType == hero &&
            !blessing.IsUltimate &&
            GetStack(blessing.Id) > 0);

        if (distinctNormalBlessings < 3)
            return null;

        BlessingDefinition ultimate = allBlessings.FirstOrDefault(blessing =>
            blessing != null && blessing.HeroType == hero && blessing.IsUltimate);
        if (ultimate == null || GetStack(ultimate.Id) > 0)
            return null;

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
        if (choiceRoot != null)
            choiceRoot.SetActive(false);
        playerEffects?.SetChoiceMode(false);

        Action callback = selectionComplete;
        selectionComplete = null;
        callback?.Invoke();
    }

    private void SetCardsInteractable(bool interactable)
    {
        if (choiceCards == null)
            return;

        for (int i = 0; i < choiceCards.Length; i++)
        {
            if (choiceCards[i] != null)
                choiceCards[i].SetInteractable(interactable);
        }
    }

    private void ApplyChoicePromptText()
    {
        if (titleText != null)
            titleText.text = "CH\u1eccN CH\u00daC PH\u00daC ANH LINH";
        if (resultText != null)
            resultText.text = "Ch\u1ecdn 1 trong 3 s\u1ee9c m\u1ea1nh \u0111\u1ec3 \u0111\u1ecbnh h\u00ecnh l\u1ed1i ch\u01a1i.";
    }

    private void ApplySelectionMessage(BlessingDefinition blessing, BlessingDefinition ultimate, int newStack)
    {
        if (resultText == null || blessing == null)
            return;

        string safeMessage = "\u0110\u00e3 nh\u1eadn: " + blessing.Name + "  [" + newStack + "/" + blessing.MaxStack + "]";
        if (ultimate != null)
            safeMessage += "\nM\u1ede KH\u00d3A T\u1ed0I TH\u01af\u1ee2NG: " + ultimate.Name;

        resultText.text = safeMessage;
    }
}
