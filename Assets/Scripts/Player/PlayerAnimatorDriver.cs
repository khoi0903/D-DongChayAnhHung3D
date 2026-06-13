using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float speedDampTime = 0.12f;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string groundedParameter = "Grounded";
    [SerializeField] private string hitParameter = "Hit";
    [SerializeField] private string pushParameter = "Push";
    [SerializeField] private string deathParameter = "Die";
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string hitState = "Hit";
    [SerializeField] private float hitDuration = 0.5f;
    [SerializeField] private string pushState = "Push";
    [SerializeField] private float stopPushCrossFade = 0.08f;
    [SerializeField] private float walkAnimationSpeedValue = 0.5f;
    [SerializeField] private float runAnimationSpeedValue = 1f;
    [SerializeField] private bool alignVisualToControllerFeet = true;

    private CharacterController characterController;
    private PlayerController3D playerController;
    private RuntimeAnimatorController cachedController;
    private Vector3 previousPosition;
    private int speedHash;
    private int groundedHash;
    private int hitHash;
    private int pushHash;
    private int deathHash;
    private bool hasSpeedParameter;
    private bool hasGroundedParameter;
    private bool hasHitParameter;
    private bool hasPushParameter;
    private bool hasDeathParameter;
    private Coroutine hitRoutine;

    private void Awake()
    {
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController3D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        previousPosition = GetPlayerRoot().position;
        RefreshParameters();
        AlignVisualToControllerFeet();
    }

    private void Update()
    {
        if (animator == null)
            return;

        if (cachedController != animator.runtimeAnimatorController)
            RefreshParameters();

        Transform playerRoot = GetPlayerRoot();
        Vector3 movement = playerRoot.position - previousPosition;
        movement.y = 0f;
        previousPosition = playerRoot.position;

        float movedDistance = movement.magnitude;
        bool isMoving = movedDistance > 0.001f || HasMovementInput();
        bool isRunning = IsRunHeld();
        float normalizedSpeed = isMoving
            ? (isRunning ? runAnimationSpeedValue : walkAnimationSpeedValue)
            : 0f;

        if (hasSpeedParameter)
            animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);

        if (hasGroundedParameter && characterController != null)
            animator.SetBool(groundedHash, characterController.isGrounded);
    }

    private void LateUpdate()
    {
        AlignVisualToControllerFeet();
    }

    public void PlayHit()
    {
        if (animator == null)
            return;

        if (cachedController != animator.runtimeAnimatorController)
            RefreshParameters();

        if (hasHitParameter)
            animator.SetTrigger(hitHash);

        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(StopHitAfterDuration());
    }

    private IEnumerator StopHitAfterDuration()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, hitDuration));

        if (animator == null)
        {
            hitRoutine = null;
            yield break;
        }

        if (hasHitParameter)
            animator.ResetTrigger(hitHash);

        int idleHash = Animator.StringToHash(idleState);
        int hitStateHash = Animator.StringToHash(hitState);
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.HasState(0, idleHash) && stateInfo.shortNameHash == hitStateHash)
            animator.CrossFade(idleHash, 0.08f, 0);

        hitRoutine = null;
    }

    public void PlayDeath()
    {
        if (animator == null)
            return;

        if (cachedController != animator.runtimeAnimatorController)
            RefreshParameters();

        if (hasDeathParameter)
            animator.SetTrigger(deathHash);
    }

    public void PlayPush()
    {
        if (animator == null)
            return;

        if (cachedController != animator.runtimeAnimatorController)
            RefreshParameters();

        if (hasPushParameter)
            animator.SetTrigger(pushHash);
    }

    public void StopPush()
    {
        if (animator == null)
            return;

        if (cachedController != animator.runtimeAnimatorController)
            RefreshParameters();

        if (hasPushParameter)
            animator.ResetTrigger(pushHash);

        int idleHash = Animator.StringToHash(idleState);
        int pushStateHash = Animator.StringToHash(pushState);
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.HasState(0, idleHash) && stateInfo.shortNameHash == pushStateHash)
            animator.CrossFade(idleHash, stopPushCrossFade, 0);
    }

    private void RefreshParameters()
    {
        cachedController = animator != null ? animator.runtimeAnimatorController : null;
        hasSpeedParameter = false;
        hasGroundedParameter = false;
        hasHitParameter = false;
        hasPushParameter = false;
        hasDeathParameter = false;
        speedHash = Animator.StringToHash(speedParameter);
        groundedHash = Animator.StringToHash(groundedParameter);
        hitHash = Animator.StringToHash(hitParameter);
        pushHash = Animator.StringToHash(pushParameter);
        deathHash = Animator.StringToHash(deathParameter);

        if (animator == null || cachedController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
                hasSpeedParameter = true;

            if (parameter.nameHash == groundedHash && parameter.type == AnimatorControllerParameterType.Bool)
                hasGroundedParameter = true;

            if (parameter.nameHash == hitHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasHitParameter = true;

            if (parameter.nameHash == pushHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasPushParameter = true;

            if (parameter.nameHash == deathHash && parameter.type == AnimatorControllerParameterType.Trigger)
                hasDeathParameter = true;
        }

        animator.applyRootMotion = false;
    }

    private Transform GetPlayerRoot()
    {
        return characterController != null ? characterController.transform : transform;
    }

    private bool HasMovementInput()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f ||
               Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
    }

    private bool IsRunHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private void AlignVisualToControllerFeet()
    {
        if (!alignVisualToControllerFeet || animator == null || characterController == null)
            return;

        Transform visual = animator.transform;
        if (visual == null || visual == characterController.transform || visual.parent == null)
            return;

        float controllerBottom = characterController.center.y - characterController.height * 0.5f;
        Vector3 localPosition = visual.localPosition;
        localPosition.y = controllerBottom;
        visual.localPosition = localPosition;
    }
}
