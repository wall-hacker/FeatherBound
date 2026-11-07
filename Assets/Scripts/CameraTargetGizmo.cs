using UnityEngine;

[ExecuteAlways]
public class CameraTargetGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.cyan;
    public float arrowSize = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 pos = transform.position;
        Vector3 forward = transform.forward * arrowSize;

        // Draw line
        Gizmos.DrawLine(pos, pos + forward);

        // Draw arrowhead
        Vector3 right = Quaternion.LookRotation(forward) * Quaternion.Euler(0, 150, 0) * Vector3.forward * 0.3f * arrowSize;
        Vector3 left = Quaternion.LookRotation(forward) * Quaternion.Euler(0, -150, 0) * Vector3.forward * 0.3f * arrowSize;
        Gizmos.DrawLine(pos + forward, pos + forward + right);
        Gizmos.DrawLine(pos + forward, pos + forward + left);
    }
}
