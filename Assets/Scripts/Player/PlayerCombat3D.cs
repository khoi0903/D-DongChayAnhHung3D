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
    public LayerMask aimGroundMask = ~0;
    public float feedbackDuration = 0.18f;
    public Color feedbackColor = new Color(0.15f, 0.9f, 1f, 0.45f);
    public bool faceAttackDirection = true;

    private float lastAttackTime;
    private Material feedbackMaterial;
    private PlayerController3D playerController;
    private InputSettingsManager inputManager;

    public bool IsHeavyAttackActive { get; set; }
    public bool IsAttacking => Time.time < lastAttackTime + attackCooldown;

    private void Awake()
    {
        playerController = GetComponent<PlayerController3D>();
        inputManager = GetComponent<InputSettingsManager>();
        if (inputManager == null)
        {
            inputManager = GetComponentInParent<InputSettingsManager>();
        }
    }

    private void Update()
    {
        if (playerController != null && (playerController.InputLocked || playerController.IsDashing))
            return;

        KeyCode attackKey = (inputManager != null && inputManager.Keyboard != null) ? inputManager.Keyboard.attack : KeyCode.Mouse0;
        
        // Handle Mouse0 or other keys through Input.GetKeyDown
        bool attackPressed = false;
        if (attackKey == KeyCode.Mouse0)
            attackPressed = Input.GetMouseButtonDown(0);
        else if (attackKey == KeyCode.Mouse1)
            attackPressed = Input.GetMouseButtonDown(1);
        else if (attackKey == KeyCode.Mouse2)
            attackPressed = Input.GetMouseButtonDown(2);
        else
            attackPressed = Input.GetKeyDown(attackKey);

        if (attackPressed)
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
        FaceAttackDirection(attackDirection);

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

    public bool CanCancelCurrentAttack()
    {
        // Light attacks can be canceled; Heavy attacks cannot.
        return IsAttacking && !IsHeavyAttackActive;
    }

    public void CancelAttack()
    {
        // Reset lastAttackTime to cancel the cooldown and attack state
        lastAttackTime = 0f;
        Debug.Log("PlayerCombat3D: Attack canceled by Dash.");
    }

    private Vector3 GetAttackDirection()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        if (aimCamera != null)
        {
            Ray ray = aimCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 120f, aimGroundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 cursorDirection = hit.point - transform.position;
                cursorDirection.y = 0f;

                if (cursorDirection.sqrMagnitude > 0.01f)
                    return cursorDirection.normalized;
            }
        }

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

    private void FaceAttackDirection(Vector3 attackDirection)
    {
        if (!faceAttackDirection || attackDirection.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(attackDirection.normalized);
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
