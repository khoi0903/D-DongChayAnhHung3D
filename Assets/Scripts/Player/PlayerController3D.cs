using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 12f;
    public float jumpHeight = 1.4f;
    public float gravity = -20f;
    public float groundedStickForce = -2f;
    public float stepOffset = 0.55f;
    public float slopeLimit = 55f;

    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private float originalStepOffset;
    private float temporarySpeedMultiplier = 1f;
    private float temporarySpeedUntil;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        originalStepOffset = controller.stepOffset;
        ConfigureControllerForPrototypeTraversal();
    }

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;
        float currentSpeed = (IsRunHeld() ? runSpeed : moveSpeed) * GetTemporarySpeedMultiplier();

        if (input.magnitude >= 0.1f)
        {
            Vector3 moveDirection;

            if (cameraTransform != null)
            {
                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;

                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = cameraForward * input.z + cameraRight * input.x;
            }
            else
            {
                moveDirection = input;
            }

            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        bool grounded = controller.isGrounded;

        if (grounded && velocity.y < 0f)
            velocity.y = groundedStickForce;

        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void ConfigureControllerForPrototypeTraversal()
    {
        controller.stepOffset = Mathf.Max(originalStepOffset, stepOffset);
        controller.slopeLimit = Mathf.Max(controller.slopeLimit, slopeLimit);
    }

    private bool IsRunHeld()
    {
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    public void ApplyTemporarySlow(float multiplier, float duration)
    {
        temporarySpeedMultiplier = Mathf.Clamp(multiplier, 0.05f, 1f);
        temporarySpeedUntil = Time.time + Mathf.Max(0f, duration);
    }

    private float GetTemporarySpeedMultiplier()
    {
        if (Time.time <= temporarySpeedUntil)
            return temporarySpeedMultiplier;

        temporarySpeedMultiplier = 1f;
        return 1f;
    }
}
