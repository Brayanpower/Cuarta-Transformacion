using UnityEngine;

/// <summary>
/// Anima elementos coloniales-medievales nocturnos en los 3 niveles:
/// antorchas parpadeantes, faroles con pulso cálido y estandartes de fiesta
/// meciéndose con la brisa nocturna de Dolores Hidalgo y Guanajuato.
/// </summary>
public class MedievalPropAnimator : MonoBehaviour
{
    public enum PropStyle
    {
        TorchFlicker,
        LanternGlowPulse,
        BannerSway
    }

    public PropStyle propStyle = PropStyle.TorchFlicker;
    public Sprite[] frames;
    public float animationFps = 7f;
    public float swayAngle = 4.5f;
    public float swaySpeed = 2.0f;

    private SpriteRenderer sr;
    private float timer = 0f;
    private int frameIndex = 0;
    private float phaseOffset = 0f;
    private Color baseColor = Color.white;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;
        phaseOffset = Random.Range(0f, 6.28f);
    }

    private void Update()
    {
        switch (propStyle)
        {
            case PropStyle.TorchFlicker:
                if (frames != null && frames.Length > 1 && sr != null)
                {
                    timer += Time.deltaTime;
                    if (timer >= 1f / Mathf.Max(1f, animationFps))
                    {
                        timer = 0f;
                        frameIndex = (frameIndex + 1) % frames.Length;
                        if (frames[frameIndex] != null)
                            sr.sprite = frames[frameIndex];
                    }
                }
                if (sr != null)
                {
                    float flicker = 0.92f + Mathf.Sin(Time.time * 16f + phaseOffset) * 0.08f;
                    sr.color = new Color(baseColor.r * flicker, baseColor.g * flicker, baseColor.b * flicker, baseColor.a);
                }
                break;

            case PropStyle.LanternGlowPulse:
                if (sr != null)
                {
                    float pulse = 0.90f + Mathf.Sin(Time.time * 3.2f + phaseOffset) * 0.10f;
                    sr.color = new Color(baseColor.r * pulse, baseColor.g * pulse, baseColor.b * pulse, baseColor.a);
                }
                break;

            case PropStyle.BannerSway:
                float angle = Mathf.Sin(Time.time * swaySpeed + phaseOffset) * swayAngle;
                transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                break;
        }
    }
}
