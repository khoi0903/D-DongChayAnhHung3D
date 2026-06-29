using UnityEngine;

/// <summary>
/// BlessingDefinition.cs
/// ScriptableObject định nghĩa một Blessing (Chúc Phúc Anh Linh) trong hệ thống S03.
/// Mỗi asset được tạo bằng menu: Create > Dong Chay Anh Hung > Blessing
/// Được dùng bởi BlessingManager và BlessingRuntimeController.
/// </summary>
[CreateAssetMenu(fileName = "Blessing", menuName = "Dong Chay Anh Hung/Blessing")]
public sealed class BlessingDefinition : ScriptableObject
{
    // ── Identification ──────────────────────────────────────────────
    [Tooltip("ID duy nhất dùng trong code (ví dụ: 'adv_no_than'). Nếu để trống thì dùng tên asset.")]
    [SerializeField] private string id;

    [Tooltip("Tên hiển thị trên UI (ví dụ: 'Nỏ Thần').")]
    [SerializeField] private string displayName;

    [Tooltip("Anh hùng lịch sử sở hữu blessing này.")]
    [SerializeField] private HeroType heroType;

    // ── Presentation ────────────────────────────────────────────────
    [Tooltip("Icon hiển thị trên card chọn Blessing (tùy chọn).")]
    [SerializeField] private Sprite icon;

    [TextArea(2, 5)]
    [Tooltip("Mô tả ngắn gọn hiệu ứng để hiển thị trên card UI.")]
    [SerializeField] private string description;

    // ── Gameplay Config ─────────────────────────────────────────────
    [Tooltip("Độ hiếm: Common, Rare, Epic hoặc Legendary.")]
    [SerializeField] private BlessingRarity rarity;

    [Min(1)]
    [Tooltip("Số lần tối đa có thể nhặt cùng 1 blessing (stack). Ultimate thường = 1.")]
    [SerializeField] private int maxStack = 3;

    [Tooltip("Loại hiệu ứng gameplay mà blessing này ánh xạ vào.")]
    [SerializeField] private BlessingEffectType effectType;

    [Tooltip("Đánh dấu là Ultimate – chỉ mở khóa khi có đủ 3 blessing thường của cùng nhánh anh hùng.")]
    [SerializeField] private bool ultimate;

    // ── Runtime (NonSerialized) ──────────────────────────────────────
    [System.NonSerialized] private int currentStack;

    // ── Public Properties ────────────────────────────────────────────
    public string Id          => string.IsNullOrWhiteSpace(id) ? name : id;
    public string Name        => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
    public HeroType HeroType  => heroType;
    public Sprite Icon        => icon;
    public string Description => description;
    public BlessingRarity Rarity => rarity;
    public int MaxStack       => Mathf.Max(1, maxStack);
    public int CurrentStack   => currentStack;
    public BlessingEffectType EffectType => effectType;
    public bool IsUltimate    => ultimate;

    // ── Runtime API ──────────────────────────────────────────────────
    /// <summary>Gọi từ BlessingManager mỗi khi người chơi nhặt 1 stack.</summary>
    public void SetRuntimeStack(int value)
    {
        currentStack = Mathf.Clamp(value, 0, MaxStack);
    }

    // ── Editor-only helper ───────────────────────────────────────────
#if UNITY_EDITOR
    /// <summary>Cho phép S03CoLoaArenaBuilder cấu hình asset qua code.</summary>
    public void Configure(
        string blessingId,
        string blessingName,
        HeroType hero,
        string blessingDescription,
        BlessingRarity blessingRarity,
        int stackLimit,
        BlessingEffectType effect,
        bool isUltimate)
    {
        id          = blessingId;
        displayName = blessingName;
        heroType    = hero;
        description = blessingDescription;
        rarity      = blessingRarity;
        maxStack    = Mathf.Max(1, stackLimit);
        effectType  = effect;
        ultimate    = isUltimate;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
