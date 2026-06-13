using UnityEngine;

public class PlayerCombat3D : MonoBehaviour
{
    [Header("Resonance Counter")]
    public int damage = 24;
    public float attackRange = 5.2f;
    public float attackAngle = 95f;
    public float closeHitRadius = 1.35f;
    public float attackCooldown = 0.65f;
    public float knockbackForce = 6.5f;
    public float enemyStunDuration = 0.42f;

    [Header("Feedback")]
    public Camera aimCamera;
    public float feedbackDuration = 0.18f;
    public Color feedbackColor = new Color(0.15f, 0.9f, 1f, 0.45f);

    private float lastAttackTime;
    private Material feedbackMaterial;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        Vector3 attackDirection = GetAttackDirection();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int hitCount = 0;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null)
                continue;

            MinionHealth3D enemyHealth = enemy.GetComponent<MinionHealth3D>();
            if (enemyHealth == null || enemyHealth.IsDead)
                continue;

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;

            if (!IsInsideAttackArea(toEnemy, distance, attackDirection))
                continue;

            enemyHealth.TakeDamage(damage);

            if (enemyHealth.IsDead)
            {
                hitCount++;
                continue;
            }

            MinionChase3D chase = enemy.GetComponent<MinionChase3D>();
            if (chase != null)
            {
                Vector3 knockbackDirection = distance > 0.1f ? toEnemy.normalized : attackDirection;
                chase.ApplyKnockback(knockbackDirection, knockbackForce, enemyStunDuration);
            }

            hitCount++;
        }

        SpawnAttackFeedback(attackDirection, hitCount > 0);
        Debug.Log("Resonance counterattack hit enemies: " + hitCount);
    }

    private Vector3 GetAttackDirection()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        return direction.normalized;
    }

    private bool IsInsideAttackArea(Vector3 toEnemy, float distance, Vector3 attackDirection)
    {
        if (distance <= closeHitRadius)
            return true;

        if (distance > attackRange || toEnemy.sqrMagnitude < 0.001f)
            return false;

        float angle = Vector3.Angle(attackDirection, toEnemy.normalized);
        return angle <= attackAngle * 0.5f;
    }

    private void SpawnAttackFeedback(Vector3 attackDirection, bool hit)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pulse.name = hit ? "ResonanceCounter_HitPulse" : "ResonanceCounter_MissPulse";
        pulse.transform.position = transform.position + Vector3.up * 1.05f + attackDirection * 2.1f;
        pulse.transform.rotation = Quaternion.LookRotation(attackDirection);
        pulse.transform.localScale = new Vector3(2.4f, 0.14f, 3.6f);

        Collider collider = pulse.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = pulse.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = GetFeedbackMaterial(hit);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        Destroy(pulse, feedbackDuration);
    }

    private Material GetFeedbackMaterial(bool hit)
    {
        if (feedbackMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            feedbackMaterial = new Material(shader)
            {
                name = "Runtime_ResonanceCounter_Feedback"
            };

            feedbackMaterial.EnableKeyword("_EMISSION");
        }

        Color color = hit ? feedbackColor : new Color(feedbackColor.r, feedbackColor.g, feedbackColor.b, feedbackColor.a * 0.45f);
        feedbackMaterial.color = color;

        if (feedbackMaterial.HasProperty("_BaseColor"))
            feedbackMaterial.SetColor("_BaseColor", color);

        if (feedbackMaterial.HasProperty("_EmissionColor"))
            feedbackMaterial.SetColor("_EmissionColor", color * 1.8f);

        return feedbackMaterial;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
