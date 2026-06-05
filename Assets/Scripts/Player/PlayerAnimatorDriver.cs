using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimatorDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float speedDampTime = 0.12f;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string groundedParameter = "Grounded";

    private CharacterController characterController;
    private PlayerController3D playerController;
    private RuntimeAnimatorController cachedController;
    private Vector3 previousPosition;
    private int speedHash;
    private int groundedHash;
    private bool hasSpeedParameter;
    private bool hasGroundedParameter;

    private void Awake()
    {
        characterController = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController3D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        previousPosition = GetPlayerRoot().position;
        RefreshParameters();
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

        float runSpeed = playerController != null ? Mathf.Max(0.01f, playerController.runSpeed) : 8f;
        float normalizedSpeed = Time.deltaTime > 0.0001f
            ? Mathf.Clamp01(movement.magnitude / Time.deltaTime / runSpeed)
            : 0f;

        if (hasSpeedParameter)
            animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);

        if (hasGroundedParameter && characterController != null)
            animator.SetBool(groundedHash, characterController.isGrounded);
    }

    private void RefreshParameters()
    {
        cachedController = animator != null ? animator.runtimeAnimatorController : null;
        hasSpeedParameter = false;
        hasGroundedParameter = false;
        speedHash = Animator.StringToHash(speedParameter);
        groundedHash = Animator.StringToHash(groundedParameter);

        if (animator == null || cachedController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
                hasSpeedParameter = true;

            if (parameter.nameHash == groundedHash && parameter.type == AnimatorControllerParameterType.Bool)
                hasGroundedParameter = true;
        }

        animator.applyRootMotion = false;
    }

    private Transform GetPlayerRoot()
    {
        return characterController != null ? characterController.transform : transform;
    }
}
