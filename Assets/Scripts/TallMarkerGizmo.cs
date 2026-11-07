using UnityEngine;
using UnityEditor;

public class TallMarkerGizmo : MonoBehaviour
{
    public float height = 50f;     // How tall the line is
    public float thickness = 3f;     // Line thickness
    public Color color = Color.yellow;


    private void OnDrawGizmos()
    {
        // Handles.color = color;

        // Vector3 start = transform.position;
        // Vector3 end = transform.position + Vector3.up * height;

        // Handles.DrawAAPolyLine(thickness, start, end);
    }

}
