using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image fillImage;
    public bool  invertFill = false;   // true for P2 (right-anchored bar)

    void Awake()
    {
        if (fillImage == null) fillImage = GetComponentInChildren<Image>();
    }

    public void SetHealth(float percent)
    {
        if (fillImage == null) return;

        percent = Mathf.Clamp01(percent);

        // Shrink by moving the anchor edge inward
        var rt = fillImage.rectTransform;
        if (invertFill)
            rt.anchorMin = new Vector2(1f - percent, rt.anchorMin.y);
        else
            rt.anchorMax = new Vector2(percent, rt.anchorMax.y);

        // Green → Yellow → Red
        fillImage.color = percent > 0.5f
            ? Color.Lerp(Color.yellow, Color.green,  (percent - 0.5f) * 2f)
            : Color.Lerp(Color.red,    Color.yellow,  percent * 2f);
    }
}
