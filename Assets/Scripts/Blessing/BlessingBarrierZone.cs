using UnityEngine;

/// <summary>
/// BlessingBarrierZone.cs
/// Vùng kết giới/phân thân được tạo ra bởi BlessingRuntimeController khi:
///   - Blessing "Tường Thành" (DashBarrier): làm chậm + ngăn địch vào vùng.
///   - Blessing "Bóng Chiến Trường" (DashDecoy): tạo phân thân thu hút địch.
/// Zone tự hủy sau khi hết `duration`, tick đẩy và stun địch ở gần.
/// </summary>
public sealed class BlessingBarrierZone : MonoBehaviour
{
    // ── Collision buffer (tránh alloc) ──────────────────────────────
    private readonly Collider[] hits = new Collider[32];

    // ── Config (set qua Configure()) ────────────────────────────────
    private float  radius       = 2.5f;   // Bán kính ảnh hưởng
    private float  duration     = 2f;     // Thời gian tồn tại (giây)
    private float  stunDuration = 0.25f;  // Thời gian stun địch mỗi tick
    private float  elapsed;               // Thời gian đã trôi qua
    private float  nextTick;              // Lần tick tiếp theo (tránh tick mỗi frame)
    private Color  color        = Color.cyan;
    private Vector3 baseScale   = Vector3.one;

    // ──────────────────────────────────────────────────────────────────
    //  Public Config
    // ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// Cấu hình zone ngay sau khi AddComponent.
    /// </summary>
    /// <param name="zoneRadius">Bán kính vùng ảnh hưởng.</param>
    /// <param name="zoneDuration">Thời gian tồn tại của zone (giây).</param>
    /// <param name="enemyStunDuration">Thời gian stun địch mỗi lần tick (giây).</param>
    /// <param name="zoneColor">Màu viền Gizmos (debug + visual hint).</param>
    public void Configure(float zoneRadius, float zoneDuration, float enemyStunDuration, Color zoneColor)
    {
        radius       = Mathf.Max(0.5f,  zoneRadius);
        duration     = Mathf.Max(0.1f,  zoneDuration);
        stunDuration = Mathf.Max(0.05f, enemyStunDuration);
        color        = zoneColor;
        baseScale    = transform.localScale;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────────────
    private void Update()
    {
        elapsed += Time.deltaTime;

        // Tự hủy khi hết thời gian
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        // Hiệu ứng pulsing: scale dao động nhẹ để tạo cảm giác sống động
        float pulse          = 1f + Mathf.Sin(Time.time * 12f) * 0.06f;
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);

        // Throttle tick để tránh gọi mỗi frame
        if (Time.time < nextTick) return;
        nextTick = Time.time + 0.22f;

        AffectEnemies();
    }

    // ──────────────────────────────────────────────────────────────────
    //  Private – Enemy Interaction
    // ──────────────────────────────────────────────────────────────────
    private void AffectEnemies()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position, radius, hits,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hits[i];
            hits[i] = null;
            if (hit == null) continue;

            MinionChase3D minion = hit.GetComponentInParent<MinionChase3D>();
            if (minion == null) continue;

            // Đẩy địch ra xa tâm zone
            Vector3 away = minion.transform.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = minion.transform.forward;

            minion.ApplyKnockback(away.normalized, 1.15f, stunDuration);
            minion.SuppressAttacks(0.3f);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Gizmos (Editor debug)
    // ──────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
