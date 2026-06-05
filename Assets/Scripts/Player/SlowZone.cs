using UnityEngine;
using System.Collections.Generic;

public class SlowZone : MonoBehaviour
{
    public float slowMoveSpeed = 1.5f;
    public float slowRunSpeed = 2.2f;

    private readonly Dictionary<PlayerController3D, SpeedSnapshot> originalSpeeds = new Dictionary<PlayerController3D, SpeedSnapshot>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerController3D player = GetPlayerController(other);

        if (player == null || originalSpeeds.ContainsKey(player))
            return;

        originalSpeeds.Add(player, new SpeedSnapshot(player.moveSpeed, player.runSpeed));

        player.moveSpeed = slowMoveSpeed;
        player.runSpeed = slowRunSpeed;

        Debug.Log("Entered SlowZone");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController3D player = GetPlayerController(other);

        if (player == null || !originalSpeeds.TryGetValue(player, out SpeedSnapshot snapshot))
            return;

        player.moveSpeed = snapshot.moveSpeed;
        player.runSpeed = snapshot.runSpeed;
        originalSpeeds.Remove(player);

        Debug.Log("Exited SlowZone");
    }

    private PlayerController3D GetPlayerController(Collider other)
    {
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
            return null;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player == null)
            player = other.GetComponentInParent<PlayerController3D>();

        return player;
    }

    private struct SpeedSnapshot
    {
        public readonly float moveSpeed;
        public readonly float runSpeed;

        public SpeedSnapshot(float moveSpeed, float runSpeed)
        {
            this.moveSpeed = moveSpeed;
            this.runSpeed = runSpeed;
        }
    }
}
