using UnityEngine;

public class EnemyChase3D : MonoBehaviour
{
    public Transform target;

    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float chaseRange = 20f;
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackCooldown = 1f;
    public float knockbackDamping = 10f;
    public float groundSnapHeight = 6f;
    public float groundSnapOffset = 0.02f;
    public LayerMask groundMask = ~0;

    private float lastAttackTime;
    private float stunnedUntil;
    private Vector3 knockbackVelocity;
    private Animator[] visualAnimators;

    private void Awake()
    {
        visualAnimators = GetComponentsInChildren<Animator>(true);
        SnapToGround();
    }

    private void OnEnable()
    {
        SnapToGround();
        ForceVisualAnimation();
    }

    private void Update()
    {
        SnapToGround();
        ApplyKnockbackMotion();

        if (target == null)
            return;

        if (Time.time < stunnedUntil)
            return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= chaseRange && distance > attackRange)
        {
            ChaseTarget();
        }

        if (distance <= attackRange)
        {
            AttackTarget();
        }
    }

    private void ChaseTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        direction.Normalize();
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void AttackTarget()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        PlayerHealth3D playerHealth = target.GetComponent<PlayerHealth3D>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        Debug.Log("Hắc Tinh tấn công Player.");
    }

    public void ApplyKnockback(Vector3 direction, float force, float stunDuration)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = -transform.forward;

        knockbackVelocity = direction.normalized * force;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunDuration);
    }

    private void ApplyKnockbackMotion()
    {
        if (knockbackVelocity.sqrMagnitude < 0.01f)
            return;

        transform.position += knockbackVelocity * Time.deltaTime;
        knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDamping * Time.deltaTime);
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundSnapHeight;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundSnapHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider.transform.IsChildOf(transform))
            return;

        transform.position = new Vector3(transform.position.x, hit.point.y + groundSnapOffset, transform.position.z);
    }

    private void SetVisualAnimationSpeed(float speed)
    {
        if (visualAnimators == null || visualAnimators.Length == 0)
            return;

        foreach (Animator animator in visualAnimators)
        {
            if (animator != null)
                animator.speed = speed;
        }
    }

    public void ForceVisualAnimation(string stateName = "Walking")
    {
        visualAnimators = GetComponentsInChildren<Animator>(true);

        if (visualAnimators == null || visualAnimators.Length == 0)
        {
            Debug.LogWarning("EnemyChase3D: spawned enemy has no child Animator: " + name, this);
            return;
        }

        foreach (Animator animator in visualAnimators)
        {
            if (animator == null)
                continue;

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            if (animator.runtimeAnimatorController != null && !string.IsNullOrWhiteSpace(stateName))
            {
                animator.Rebind();
                animator.Update(0f);
                animator.Play(stateName, 0, Random.value);
            }
            else if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("EnemyChase3D: child Animator is missing RuntimeAnimatorController on " + animator.gameObject.name, animator);
            }
        }
    }
}
