using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 4.8f;
    public float height = 4f;
    public float mouseSensitivity = 3f;
    public float smoothSpeed = 8f;
    public bool fixedAngle = true;
    public float fixedYaw = 45f;
    public float fixedPitch = 58f;
    public bool lockCursor = false;

    [Header("Death View")]
    public float deathYaw = 45f;
    public float deathPitch = 76f;
    public float deathDistance = 6.4f;
    public float deathHeight = 5.9f;
    public float deathLookAtHeight = 0.45f;
    public float deathSmoothSpeed = 4.5f;

    [Header("Vertical Clamp")]
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch = 20f;
    private bool deathViewActive;

    private void Start()
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;

        Vector3 angles = transform.eulerAngles;
        yaw = fixedAngle ? fixedYaw : angles.y;
        pitch = fixedAngle ? fixedPitch : angles.x;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float targetYaw;
        float targetPitch;
        float targetDistance;
        float targetHeight;
        float targetLookAtHeight;
        float targetSmoothSpeed;

        if (deathViewActive)
        {
            targetYaw = deathYaw;
            targetPitch = deathPitch;
            targetDistance = deathDistance;
            targetHeight = deathHeight;
            targetLookAtHeight = deathLookAtHeight;
            targetSmoothSpeed = deathSmoothSpeed;
            yaw = targetYaw;
            pitch = targetPitch;
        }
        else if (fixedAngle)
        {
            yaw = fixedYaw;
            pitch = fixedPitch;
            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
            targetHeight = height;
            targetLookAtHeight = 1.5f;
            targetSmoothSpeed = smoothSpeed;
        }
        else
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            targetYaw = yaw;
            targetPitch = pitch;
            targetDistance = distance;
            targetHeight = height;
            targetLookAtHeight = 1.5f;
            targetSmoothSpeed = smoothSpeed;
        }

        Quaternion rotation = Quaternion.Euler(targetPitch, targetYaw, 0f);

        Vector3 desiredPosition = target.position - rotation * Vector3.forward * targetDistance;
        desiredPosition.y += targetHeight;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            targetSmoothSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up * targetLookAtHeight);
    }

    public void BeginDeathView(Transform deathTarget)
    {
        if (deathTarget != null)
            target = deathTarget;

        deathViewActive = true;
    }
}
