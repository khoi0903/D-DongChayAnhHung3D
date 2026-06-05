using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class S01ChaseThreat : MonoBehaviour
{
    public Transform player;
    public Transform[] waypoints;

    [HideInInspector] public float startDelay = 0f;
    public float directChaseSpeed = 6f;
    public float waypointSpeed = 5.2f;
    [HideInInspector] public float moveSpeed = 5.2f;
    [HideInInspector] public float catchUpSpeed = 7.5f;
    public float catchDistance = 1.6f;
    public float waypointReachDistance = 0.8f;
    [HideInInspector] public float farFromPlayerDistance = 22f;
    public float rotationSpeed = 10f;

    public float movementStartDistance = 1.2f;
    public bool hideUntilChaseStarts = true;
    public float nearPlayerDistance = 6f;
    public float farPlayerDistance = 22f;
    public float veryFarPlayerDistance = 35f;
    public float farSpeedMultiplier = 1.55f;
    public float veryFarSpeedMultiplier = 2f;
    public float speedMultiplierChangeRate = 1.8f;
    public bool debugLogs = true;

    private readonly RaycastHit[] visibilityHits = new RaycastHit[24];

    private float currentSpeedMultiplier = 1f;
    private float suppressCatchUpUntil;
    private int waypointIndex;
    private int highestReachedWaypointIndex;
    private int playerRouteWaypointIndex;
    private bool chaseStarted;
    private bool hasCaughtPlayer;
    private bool playerStartPositionSet;
    private Vector3 playerStartPosition;
    private S01WarningTextUI warningUI;
    private ChaseMode currentMode = ChaseMode.None;
    private PressureBand currentPressureBand = PressureBand.None;
    private Collider[] selfColliders;
    private Collider[] playerColliders;
    private Renderer[] threatRenderers;

    private enum ChaseMode
    {
        None,
        DirectChase,
        WaypointFollow
    }

    private enum PressureBand
    {
        None,
        Near,
        Normal,
        CatchUp,
        StrongCatchUp
    }

    private void Awake()
    {
        selfColliders = GetComponentsInChildren<Collider>();
        threatRenderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        FindWaypointsIfNeeded();
        SkipFirstWaypointIfAlreadyThere();
        warningUI = FindAnyObjectByType<S01WarningTextUI>();
        InitializePlayerStartPosition();
        SetThreatVisible(!hideUntilChaseStarts);

        if (warningUI != null)
            warningUI.ShowWarning("Dùng WASD để di chuyển.", 6f);

        Log(player != null
            ? "S01ChaseThreat: Player found: " + player.name
            : "S01ChaseThreat: Player missing.");
        Log("S01ChaseThreat: Waypoint count = " + (waypoints != null ? waypoints.Length : 0));
        Log("S01ChaseThreat: Waiting for player movement.");
        LogCurrentWaypoint();
    }

    private void Update()
    {
        if (hasCaughtPlayer)
            return;

        if (player == null)
            FindPlayerIfNeeded();

        if (player == null)
            return;

        if (!chaseStarted)
        {
            InitializePlayerStartPosition();

            if (!PlayerHasStartedMoving())
                return;

            StartChase();
        }

        TryCatchPlayer();

        if (hasCaughtPlayer || player == null)
            return;

        UpdateRubberBandSpeed();
        UpdateForwardRouteProgress();

        if (CanDirectlyReachPlayer())
            MoveDirectlyTowardPlayer();
        else
            MoveUsingWaypoints();

        TryCatchPlayer();
    }

    private void StartChase()
    {
        chaseStarted = true;
        SetThreatVisible(true);
        Log("S01 chase started after player movement.");

        if (warningUI == null)
            warningUI = FindAnyObjectByType<S01WarningTextUI>();

        if (warningUI != null)
            warningUI.ShowWarning("Chạy! Đừng để Hắc Tinh chạm vào bạn.", 5f);
    }

    private bool PlayerHasStartedMoving()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
            return true;

        return playerStartPositionSet &&
               HorizontalDistance(player.position, playerStartPosition) >= movementStartDistance;
    }

    private void InitializePlayerStartPosition()
    {
        if (player == null || playerStartPositionSet)
            return;

        playerStartPosition = player.position;
        playerStartPositionSet = true;
    }

    private void SetThreatVisible(bool visible)
    {
        if (threatRenderers == null)
            return;

        for (int i = 0; i < threatRenderers.Length; i++)
        {
            if (threatRenderers[i] != null)
                threatRenderers[i].enabled = visible;
        }
    }

    private void MoveDirectlyTowardPlayer()
    {
        SetMode(ChaseMode.DirectChase);
        AdvanceWaypointProgressNearThreat();

        Vector3 targetPosition = player.position;
        targetPosition.y = transform.position.y;

        MoveToward(targetPosition, GetPressureSpeed(directChaseSpeed));
    }

    private void MoveUsingWaypoints()
    {
        SetMode(ChaseMode.WaypointFollow);

        if (waypoints == null || waypoints.Length == 0 || waypointIndex >= waypoints.Length)
            return;

        SelectForwardFallbackWaypoint();
        Transform targetWaypoint = GetCurrentValidWaypoint();

        if (targetWaypoint == null)
            return;

        Vector3 targetPosition = targetWaypoint.position;
        targetPosition.y = transform.position.y;

        MoveToward(targetPosition, GetPressureSpeed(waypointSpeed));

        if (HorizontalDistance(transform.position, targetWaypoint.position) <= waypointReachDistance)
        {
            AdvanceWaypointIndex(waypointIndex + 1, false);
            LogCurrentWaypoint();
        }
    }

    private void MoveToward(Vector3 targetPosition, float speed)
    {
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed * Time.deltaTime);
        nextPosition.y = currentPosition.y;

        Vector3 moveDirection = nextPosition - currentPosition;
        transform.position = nextPosition;
        RotateToward(moveDirection);
    }

    private void RotateToward(Vector3 moveDirection)
    {
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private bool CanDirectlyReachPlayer()
    {
        if (player == null)
            return false;

        CachePlayerColliders();

        Vector3 start = GetVisibilityPoint(transform);
        Vector3 end = GetVisibilityPoint(player);
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(start, direction.normalized, visibilityHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(visibilityHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = visibilityHits[i].collider;

            if (hitCollider == null)
                continue;

            if (IsSelfCollider(hitCollider) || IsPlayerCollider(hitCollider))
                continue;

            return false;
        }

        return true;
    }

    private Vector3 GetVisibilityPoint(Transform target)
    {
        return target.position + Vector3.up * 0.9f;
    }

    private void TryCatchPlayer()
    {
        if (!chaseStarted || player == null)
            return;

        if (HorizontalDistance(transform.position, player.position) > catchDistance)
            return;

        CatchPlayer();
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer)
            return;

        hasCaughtPlayer = true;
        Debug.Log("Hắc Tinh caught the Player.");

        PlayerHealth3D playerHealth = player != null ? player.GetComponent<PlayerHealth3D>() : null;
        if (playerHealth == null && player != null)
            playerHealth = player.GetComponentInParent<PlayerHealth3D>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(9999);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCatchFromCollider(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCatchFromCollider(collision.collider);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        TryCatchFromCollider(hit.collider);
    }

    private void TryCatchFromCollider(Collider other)
    {
        if (!chaseStarted || hasCaughtPlayer || other == null)
            return;

        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return;

        if (player == null)
            player = other.transform.root;

        CatchPlayer();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
        {
            CachePlayerColliders();
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            CachePlayerColliders();
            InitializePlayerStartPosition();
            Log("S01ChaseThreat: Player found: " + player.name);
        }
    }

    private void FindWaypointsIfNeeded()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            SortWaypointsByName();
            return;
        }

        GameObject waypointRoot = GameObject.Find("S01_ChaseWaypoints");
        if (waypointRoot == null)
        {
            waypoints = new Transform[0];
            Log("S01ChaseThreat: Waypoint count = 0");
            return;
        }

        int childCount = waypointRoot.transform.childCount;
        waypoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
            waypoints[i] = waypointRoot.transform.GetChild(i);

        SortWaypointsByName();
        Log("S01ChaseThreat: Waypoint count = " + waypoints.Length);
    }

    private void SortWaypointsByName()
    {
        if (waypoints == null || waypoints.Length <= 1)
            return;

        Array.Sort(waypoints, CompareWaypointNames);
    }

    private int CompareWaypointNames(Transform left, Transform right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private void SkipFirstWaypointIfAlreadyThere()
    {
        waypointIndex = 0;
        highestReachedWaypointIndex = 0;
        playerRouteWaypointIndex = 0;

        if (waypoints == null || waypoints.Length <= 1 || waypoints[0] == null)
            return;

        if (HorizontalDistance(transform.position, waypoints[0].position) <= waypointReachDistance)
            AdvanceWaypointIndex(1, false);
    }

    private Transform GetCurrentValidWaypoint()
    {
        if (waypointIndex < highestReachedWaypointIndex)
        {
            Log("Ignoring backtrack waypoint.");
            waypointIndex = highestReachedWaypointIndex;
        }

        while (waypointIndex < waypoints.Length && waypoints[waypointIndex] == null)
            AdvanceWaypointIndex(waypointIndex + 1, false);

        if (waypointIndex >= waypoints.Length)
            return null;

        return waypoints[waypointIndex];
    }

    private void AdvanceWaypointProgressNearThreat()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        while (waypointIndex < waypoints.Length && waypoints[waypointIndex] != null &&
               HorizontalDistance(transform.position, waypoints[waypointIndex].position) <= waypointReachDistance)
        {
            AdvanceWaypointIndex(waypointIndex + 1, false);
        }
    }

    private void UpdateForwardRouteProgress()
    {
        if (waypoints == null || waypoints.Length == 0 || player == null)
            return;

        UpdatePlayerRouteWaypointIndex();
        SkipWaypointsBehindThreat();

        int nearestThreatIndex = FindClosestWaypointIndex(
            transform.position,
            highestReachedWaypointIndex,
            Mathf.Max(highestReachedWaypointIndex, playerRouteWaypointIndex));

        if (nearestThreatIndex > highestReachedWaypointIndex &&
            waypoints[nearestThreatIndex] != null &&
            HorizontalDistance(transform.position, waypoints[nearestThreatIndex].position) <= 5f)
        {
            AdvanceWaypointIndex(nearestThreatIndex, nearestThreatIndex > waypointIndex + 1);
        }
    }

    private void UpdatePlayerRouteWaypointIndex()
    {
        int closestIndex = FindClosestWaypointIndex(
            player.position,
            Mathf.Max(playerRouteWaypointIndex, highestReachedWaypointIndex),
            waypoints.Length - 1);

        if (closestIndex > playerRouteWaypointIndex)
            playerRouteWaypointIndex = closestIndex;
    }

    private void SelectForwardFallbackWaypoint()
    {
        if (waypointIndex < highestReachedWaypointIndex)
        {
            Log("Ignoring backtrack waypoint.");
            waypointIndex = highestReachedWaypointIndex;
        }

        int maximumCandidate = Mathf.Clamp(playerRouteWaypointIndex, highestReachedWaypointIndex, waypoints.Length - 1);
        int safeCandidate = -1;

        for (int i = maximumCandidate; i >= highestReachedWaypointIndex; i--)
        {
            if (waypoints[i] != null && CanDirectlyReachWaypoint(waypoints[i]))
            {
                safeCandidate = i;
                break;
            }
        }

        if (safeCandidate > waypointIndex)
        {
            bool skippedOldWaypoints = safeCandidate > waypointIndex + 1;
            AdvanceWaypointIndex(safeCandidate, skippedOldWaypoints);
            Log("Fallback waypoint advanced to match player route position.");
        }
    }

    private void SkipWaypointsBehindThreat()
    {
        while (waypointIndex < waypoints.Length - 1)
        {
            Transform currentWaypoint = waypoints[waypointIndex];
            Transform nextWaypoint = waypoints[waypointIndex + 1];

            if (currentWaypoint == null)
            {
                AdvanceWaypointIndex(waypointIndex + 1, false);
                continue;
            }

            if (nextWaypoint == null)
                break;

            Vector3 routeDirection = nextWaypoint.position - currentWaypoint.position;
            Vector3 threatOffset = transform.position - currentWaypoint.position;
            routeDirection.y = 0f;
            threatOffset.y = 0f;

            if (routeDirection.sqrMagnitude <= 0.001f ||
                Vector3.Dot(threatOffset, routeDirection.normalized) <= waypointReachDistance)
            {
                break;
            }

            AdvanceWaypointIndex(waypointIndex + 1, false);
            Log("Skipping old waypoint; player is ahead.");
        }
    }

    private void AdvanceWaypointIndex(int requestedIndex, bool skippedMultiple)
    {
        if (waypoints == null)
            return;

        if (requestedIndex < highestReachedWaypointIndex)
        {
            Log("Ignoring backtrack waypoint.");
            return;
        }

        int clampedIndex = Mathf.Clamp(requestedIndex, 0, waypoints.Length);
        if (clampedIndex <= waypointIndex)
            return;

        waypointIndex = clampedIndex;
        highestReachedWaypointIndex = Mathf.Max(highestReachedWaypointIndex, waypointIndex);

        if (skippedMultiple)
            Log("Skipping old waypoint; player is ahead.");
    }

    private int FindClosestWaypointIndex(Vector3 position, int minimumIndex, int maximumIndex)
    {
        if (waypoints == null || waypoints.Length == 0)
            return 0;

        int min = Mathf.Clamp(minimumIndex, 0, waypoints.Length - 1);
        int max = Mathf.Clamp(maximumIndex, min, waypoints.Length - 1);
        int closestIndex = min;
        float closestDistance = float.MaxValue;

        for (int i = min; i <= max; i++)
        {
            if (waypoints[i] == null)
                continue;

            float distance = HorizontalDistance(position, waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private bool CanDirectlyReachWaypoint(Transform waypoint)
    {
        Vector3 start = GetVisibilityPoint(transform);
        Vector3 end = GetVisibilityPoint(waypoint);
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        int hitCount = Physics.RaycastNonAlloc(
            start,
            direction.normalized,
            visibilityHits,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = visibilityHits[i].collider;
            if (hitCollider != null && !IsSelfCollider(hitCollider) && !IsPlayerCollider(hitCollider))
                return false;
        }

        return true;
    }

    private float GetPressureSpeed(float baseSpeed)
    {
        return baseSpeed * currentSpeedMultiplier;
    }

    private void UpdateRubberBandSpeed()
    {
        if (player == null)
            return;

        float distance = HorizontalDistance(transform.position, player.position);
        PressureBand nextBand;
        float desiredMultiplier;

        if (Time.time < suppressCatchUpUntil)
        {
            nextBand = PressureBand.Normal;
            desiredMultiplier = 1f;
        }
        else if (distance > veryFarPlayerDistance)
        {
            nextBand = PressureBand.StrongCatchUp;
            desiredMultiplier = veryFarSpeedMultiplier;
        }
        else if (distance > farPlayerDistance)
        {
            nextBand = PressureBand.CatchUp;
            desiredMultiplier = farSpeedMultiplier;
        }
        else if (distance < nearPlayerDistance)
        {
            nextBand = PressureBand.Near;
            desiredMultiplier = 1f;
        }
        else
        {
            nextBand = PressureBand.Normal;
            desiredMultiplier = 1f;
        }

        currentSpeedMultiplier = Mathf.MoveTowards(
            currentSpeedMultiplier,
            desiredMultiplier,
            speedMultiplierChangeRate * Time.deltaTime);

        SetPressureBand(nextBand);
    }

    public void SuppressCatchUp(float duration)
    {
        suppressCatchUpUntil = Mathf.Max(suppressCatchUpUntil, Time.time + Mathf.Max(0f, duration));
        currentSpeedMultiplier = Mathf.Min(currentSpeedMultiplier, 1f);
        SetPressureBand(PressureBand.Normal);
    }

    private void SetPressureBand(PressureBand nextBand)
    {
        if (currentPressureBand == nextBand)
            return;

        PressureBand previousBand = currentPressureBand;
        currentPressureBand = nextBand;

        if (nextBand == PressureBand.CatchUp || nextBand == PressureBand.StrongCatchUp)
        {
            Log("Hắc Tinh catch-up speed active.");
        }
        else if (nextBand == PressureBand.Near)
        {
            Log("Hắc Tinh near player; no extra speed.");
        }
        else if (nextBand == PressureBand.Normal &&
                 (previousBand == PressureBand.CatchUp || previousBand == PressureBand.StrongCatchUp))
        {
            Log("Hắc Tinh returned to normal chase speed.");
        }
    }

    private float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void CachePlayerColliders()
    {
        if (player == null)
        {
            playerColliders = null;
            return;
        }

        playerColliders = player.GetComponentsInChildren<Collider>();
    }

    private bool IsSelfCollider(Collider candidate)
    {
        if (selfColliders == null)
            return false;

        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (candidate == selfColliders[i])
                return true;
        }

        return false;
    }

    private bool IsPlayerCollider(Collider candidate)
    {
        if (playerColliders == null)
            return false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (candidate == playerColliders[i])
                return true;
        }

        return false;
    }

    private void SetMode(ChaseMode nextMode)
    {
        if (currentMode == nextMode)
            return;

        currentMode = nextMode;

        if (nextMode == ChaseMode.DirectChase)
            Log("S01 Hắc Tinh mode: DIRECT_CHASE");
        else if (nextMode == ChaseMode.WaypointFollow)
            Log("S01 Hắc Tinh mode: WAYPOINT_FALLBACK");
    }

    private void LogCurrentWaypoint()
    {
        if (!debugLogs)
            return;

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.Log("S01ChaseThreat: Current waypoint missing because waypoint list is empty.");
            return;
        }

        if (waypointIndex >= waypoints.Length)
        {
            Debug.Log("S01ChaseThreat: Reached final waypoint.");
            return;
        }

        Transform waypoint = waypoints[waypointIndex];
        Debug.Log("S01ChaseThreat: Current waypoint = " + (waypoint != null ? waypoint.name : "null"));
    }

    private void Log(string message)
    {
        if (debugLogs)
            Debug.Log(message);
    }

    private sealed class RaycastHitDistanceComparer : IComparer
    {
        public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

        public int Compare(object x, object y)
        {
            RaycastHit left = (RaycastHit)x;
            RaycastHit right = (RaycastHit)y;
            return left.distance.CompareTo(right.distance);
        }
    }
}
