using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D defaultCursor;
    public Texture2D hoverCursor;

    void Start()
    {
        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        Vector2 hotspot = new Vector2(
            defaultCursor.width * 0.5f,
            defaultCursor.height * 0f
        );
        Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
    }

    public void SetHoverCursor()
    {
        Vector2 hotspot = new Vector2(
            hoverCursor.width * 0.5f,
            hoverCursor.height * 0f
        );
        Cursor.SetCursor(hoverCursor, hotspot, CursorMode.Auto);
    }
}
