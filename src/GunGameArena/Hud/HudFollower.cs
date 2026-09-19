using FistVR;
using UnityEngine;

namespace GunGameArena.Hud
{
    /// <summary>Keeps the canvas in front of and above the player's head, following yaw only,
    /// with exponential smoothing so it drifts instead of snapping.</summary>
    public class HudFollower : MonoBehaviour
    {
        private const float Smoothing = 6f;
        private const float TiltDegrees = 10f;

        private void LateUpdate()
        {
            var body = GM.CurrentPlayerBody;
            if (body == null || body.Head == null) return;
            Transform head = body.Head;

            float yaw = head.rotation.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 target = head.position + yawRot * Vector3.forward * ArenaConfig.Distance.Value
                             + Vector3.up * ArenaConfig.Height.Value;
            Quaternion targetRot = Quaternion.Euler(-TiltDegrees, yaw, 0f);

            float t = 1f - Mathf.Exp(-Smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
        }
    }
}
