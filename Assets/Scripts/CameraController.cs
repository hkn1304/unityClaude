using UnityEngine;

// Keeps both fighters in frame by tracking their midpoint and adjusting zoom.
public class CameraController : MonoBehaviour
{
    public Transform p1, p2;

    public float minSize    = 3.5f;
    public float maxSize    = 8.0f;
    public float padding    = 2.5f;   // units of extra space on each side
    public float smoothTime = 0.18f;

    Camera  cam;
    Vector3 velPos;
    float   velSize;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (p1 == null || p2 == null || cam == null) return;

        Vector2 mid = ((Vector2)p1.position + (Vector2)p2.position) * 0.5f;

        // Zoom to fit horizontal spread plus padding
        float halfSpread = Mathf.Abs(p1.position.x - p2.position.x) * 0.5f + padding;
        float targetSize = Mathf.Clamp(halfSpread / cam.aspect, minSize, maxSize);

        // Clamp Y so camera doesn't chase jumps too aggressively
        float targetY = Mathf.Clamp(mid.y, -1.5f, 1.0f);

        var targetPos = new Vector3(mid.x, targetY, transform.position.z);
        transform.position   = Vector3.SmoothDamp(transform.position, targetPos, ref velPos,  smoothTime);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize,  ref velSize, smoothTime);
    }
}
