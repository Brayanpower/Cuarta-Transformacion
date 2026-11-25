using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Anima únicamente el fondo nocturno del Menú Principal (Parroquia de Dolores Hidalgo):
/// - Respiración y paneo suave del paisaje nocturno.
/// - Estrellas pixel-art parpadeantes en el cielo.
/// - Globos de cantoya luminosos subiendo lentamente con brisa suave.
/// - Fuegos artificiales silenciosos lejanos de Fiesta Patronal.
/// - Luciérnagas doradas flotando sobre la plaza.
/// - Halo cálido pulsante en el farol colonial izquierdo.
/// No modifica botones, títulos ni paneles de navegación.
/// </summary>
public class MainMenuBackgroundAnimator : MonoBehaviour
{
    private class StarItem
    {
        public Image img;
        public float phase;
        public float speed;
        public float baseAlpha;
        public Color tint;
    }

    private class LanternItem
    {
        public RectTransform root;
        public Image glowImg;
        public float anchorX;
        public float anchorY;
        public float speedY;
        public float swayAmp;
        public float swaySpeed;
        public float phase;
    }

    private class FireflyItem
    {
        public RectTransform rt;
        public Image img;
        public float baseX;
        public float baseY;
        public float rangeX;
        public float rangeY;
        public float speed;
        public float phase;
    }

    private class FireworkSpark
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float age;
        public float maxAge;
        public Color color;
    }

    private RectTransform bgRect;
    private Vector2 bgBasePos;
    private RectTransform animRoot;
    private Image lanternGlowImg;

    private Sprite pixelSprite;
    private Sprite softGlowSprite;

    private readonly List<StarItem> stars = new List<StarItem>();
    private readonly List<LanternItem> lanterns = new List<LanternItem>();
    private readonly List<FireflyItem> fireflies = new List<FireflyItem>();
    private readonly List<FireworkSpark> activeSparks = new List<FireworkSpark>();
    private readonly Queue<FireworkSpark> sparkPool = new Queue<FireworkSpark>();

    private float elapsed = 0f;
    private float nextFireworkTime = 1.6f;

    private static readonly Color[] FiestaPalette =
    {
        new Color(0.18f, 0.90f, 0.42f, 1f), // Verde fiesta
        new Color(1.00f, 0.97f, 0.86f, 1f), // Blanco cálido
        new Color(0.96f, 0.26f, 0.26f, 1f), // Rojo fiesta
        new Color(1.00f, 0.82f, 0.24f, 1f), // Dorado
        new Color(0.96f, 0.34f, 0.72f, 1f), // Rosa mexicano
        new Color(0.32f, 0.88f, 1.00f, 1f)  // Azul talavera
    };

    private void Awake()
    {
        pixelSprite = BuildPixelSprite();
        softGlowSprite = BuildSoftGlowSprite();
    }

    private void Start()
    {
        SetupBackgroundLayers();
    }

    private void SetupBackgroundLayers()
    {
        if (transform.Find("Layer_AnimatedMenuBackground") != null)
            return;

        Transform bgTf = transform.Find("Background_ParroquiaDoloresNoche");
        if (bgTf != null)
        {
            bgRect = bgTf.GetComponent<RectTransform>();
            if (bgRect != null)
                bgBasePos = bgRect.anchoredPosition;
        }

        GameObject layerObj = new GameObject("Layer_AnimatedMenuBackground");
        layerObj.transform.SetParent(transform, false);
        animRoot = layerObj.AddComponent<RectTransform>();
        animRoot.anchorMin = Vector2.zero;
        animRoot.anchorMax = Vector2.one;
        animRoot.offsetMin = Vector2.zero;
        animRoot.offsetMax = Vector2.zero;

        // Colocar justo detrás del suelo de cantera, props coloniales y paneles del menú
        Transform overlayTf = transform.Find("Overlay_AtmosferaNocturna");
        if (overlayTf != null)
            animRoot.SetSiblingIndex(overlayTf.GetSiblingIndex() + 1);
        else if (bgTf != null)
            animRoot.SetSiblingIndex(bgTf.GetSiblingIndex() + 1);
        else
            animRoot.SetAsFirstSibling();

        CreateTwinklingStars(32);
        CreateFloatingLanterns(11);
        CreateFireflies(18);
        CreateColonialLanternHalo();
    }

    private void CreateTwinklingStars(int count)
    {
        Color[] tints =
        {
            new Color(1.00f, 0.98f, 0.86f, 1f),
            new Color(0.84f, 0.92f, 1.00f, 1f),
            new Color(1.00f, 0.88f, 0.56f, 1f)
        };

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject($"Star_{i}");
            go.transform.SetParent(animRoot, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            float ax = Random.Range(0.03f, 0.97f);
            float ay = Random.Range(0.56f, 0.97f);
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            float sz = Random.Range(4f, 8f);
            rt.sizeDelta = new Vector2(sz, sz);

            Image img = go.AddComponent<Image>();
            img.sprite = pixelSprite;
            img.raycastTarget = false;

            stars.Add(new StarItem
            {
                img = img,
                phase = Random.Range(0f, 6.283f),
                speed = Random.Range(1.4f, 3.6f),
                baseAlpha = Random.Range(0.35f, 0.92f),
                tint = tints[i % tints.Length]
            });
        }
    }

    private void CreateFloatingLanterns(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject($"GloboCantoya_{i}");
            go.transform.SetParent(animRoot, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            float ax = Random.Range(0.05f, 0.95f);
            float ay = Random.Range(0.14f, 0.96f);
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            float size = Random.Range(14f, 24f);
            rt.sizeDelta = new Vector2(size, size * 1.25f);

            // Halo cálido naranja detrás del globo
            GameObject haloObj = new GameObject("Halo");
            haloObj.transform.SetParent(rt, false);
            RectTransform hRt = haloObj.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(size * 3.4f, size * 3.4f);
            Image haloImg = haloObj.AddComponent<Image>();
            haloImg.sprite = softGlowSprite;
            haloImg.color = new Color(1.0f, 0.56f, 0.14f, 0.34f);
            haloImg.raycastTarget = false;

            // Cuerpo de papel iluminado del globo
            Image bodyImg = go.AddComponent<Image>();
            bodyImg.sprite = pixelSprite;
            bodyImg.color = new Color(1.0f, 0.80f, 0.32f, 0.88f);
            bodyImg.raycastTarget = false;

            // Llama brillante en la base del globo
            GameObject coreObj = new GameObject("Flame");
            coreObj.transform.SetParent(rt, false);
            RectTransform cRt = coreObj.AddComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0f, -size * 0.28f);
            cRt.sizeDelta = new Vector2(size * 0.52f, size * 0.40f);
            Image coreImg = coreObj.AddComponent<Image>();
            coreImg.sprite = pixelSprite;
            coreImg.color = new Color(1.0f, 0.98f, 0.78f, 0.95f);
            coreImg.raycastTarget = false;

            lanterns.Add(new LanternItem
            {
                root = rt,
                glowImg = haloImg,
                anchorX = ax,
                anchorY = ay,
                speedY = Random.Range(0.022f, 0.044f),
                swayAmp = Random.Range(10f, 26f),
                swaySpeed = Random.Range(0.7f, 1.4f),
                phase = Random.Range(0f, 6.283f)
            });
        }
    }

    private void CreateFireflies(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject($"Firefly_{i}");
            go.transform.SetParent(animRoot, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            float ax = Random.Range(0.06f, 0.94f);
            float ay = Random.Range(0.16f, 0.52f);
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            float sz = Random.Range(5f, 9f);
            rt.sizeDelta = new Vector2(sz, sz);

            Image img = go.AddComponent<Image>();
            img.sprite = softGlowSprite;
            img.color = new Color(1.0f, 0.86f, 0.36f, 0.75f);
            img.raycastTarget = false;

            fireflies.Add(new FireflyItem
            {
                rt = rt,
                img = img,
                baseX = ax,
                baseY = ay,
                rangeX = Random.Range(0.015f, 0.038f),
                rangeY = Random.Range(0.015f, 0.034f),
                speed = Random.Range(0.55f, 1.25f),
                phase = Random.Range(0f, 6.283f)
            });
        }
    }

    private void CreateColonialLanternHalo()
    {
        Transform farolTf = transform.Find("Prop_FarolColonial");
        if (farolTf == null)
            return;

        RectTransform farolRt = farolTf.GetComponent<RectTransform>();
        if (farolRt == null)
            return;

        GameObject glowObj = new GameObject("Glow_FarolColonial");
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.SetSiblingIndex(farolTf.GetSiblingIndex());

        RectTransform gRt = glowObj.AddComponent<RectTransform>();
        gRt.anchorMin = farolRt.anchorMin;
        gRt.anchorMax = farolRt.anchorMax;
        gRt.pivot = new Vector2(0.5f, 0.5f);
        gRt.anchoredPosition = farolRt.anchoredPosition + new Vector2(0f, farolRt.sizeDelta.y * 0.78f);
        gRt.sizeDelta = new Vector2(210f, 210f);

        lanternGlowImg = glowObj.AddComponent<Image>();
        lanternGlowImg.sprite = softGlowSprite;
        lanternGlowImg.color = new Color(1.0f, 0.72f, 0.24f, 0.42f);
        lanternGlowImg.raycastTarget = false;
    }

    private void Update()
    {
        if (animRoot == null)
            return;

        float dt = Time.unscaledDeltaTime;
        elapsed += dt;

        // 1. Respiración y paneo suave del fondo nocturno de la Parroquia
        if (bgRect != null)
        {
            float scale = 1.07f + Mathf.Sin(elapsed * 0.28f) * 0.014f;
            bgRect.localScale = new Vector3(scale, scale, 1f);
            bgRect.anchoredPosition = bgBasePos + new Vector2(
                Mathf.Sin(elapsed * 0.22f) * 20f,
                Mathf.Cos(elapsed * 0.17f) * 7f
            );
        }

        // 2. Parpadeo suave de estrellas
        for (int i = 0; i < stars.Count; i++)
        {
            StarItem st = stars[i];
            float pulse = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(elapsed * st.speed + st.phase));
            st.img.color = new Color(st.tint.r, st.tint.g, st.tint.b, st.baseAlpha * pulse);
        }

        // 3. Subida continua de globos de cantoya
        for (int i = 0; i < lanterns.Count; i++)
        {
            LanternItem l = lanterns[i];
            l.anchorY += l.speedY * dt;
            if (l.anchorY > 1.08f)
            {
                l.anchorY = 0.11f;
                l.anchorX = Random.Range(0.05f, 0.95f);
            }

            l.root.anchorMin = new Vector2(l.anchorX, l.anchorY);
            l.root.anchorMax = new Vector2(l.anchorX, l.anchorY);
            l.root.anchoredPosition = new Vector2(Mathf.Sin(elapsed * l.swaySpeed + l.phase) * l.swayAmp, 0f);

            float flamePulse = 0.24f + 0.14f * Mathf.Sin(elapsed * 6.5f + l.phase);
            l.glowImg.color = new Color(1.0f, 0.58f, 0.16f, flamePulse);
        }

        // 4. Luciérnagas doradas flotando sobre la plaza de cantera
        for (int i = 0; i < fireflies.Count; i++)
        {
            FireflyItem f = fireflies[i];
            float x = f.baseX + Mathf.Sin(elapsed * f.speed + f.phase) * f.rangeX;
            float y = f.baseY + Mathf.Cos(elapsed * f.speed * 1.3f + f.phase) * f.rangeY;
            f.rt.anchorMin = new Vector2(x, y);
            f.rt.anchorMax = new Vector2(x, y);
            float alpha = 0.25f + 0.65f * (0.5f + 0.5f * Mathf.Sin(elapsed * 2.8f + f.phase * 2f));
            f.img.color = new Color(1.0f, 0.88f, 0.38f, alpha);
        }

        // 5. Halo cálido parpadeante en el farol colonial izquierdo
        if (lanternGlowImg != null)
        {
            float flicker = 0.34f + 0.10f * Mathf.Sin(elapsed * 5.2f) + 0.04f * Mathf.Sin(elapsed * 13.7f);
            lanternGlowImg.color = new Color(1.0f, 0.72f, 0.24f, flicker);
        }

        // 6. Fuegos artificiales silenciosos lejanos en el cielo nocturno
        if (elapsed >= nextFireworkTime)
        {
            SpawnDistantFirework();
            nextFireworkTime = elapsed + Random.Range(2.8f, 4.9f);
        }

        for (int i = activeSparks.Count - 1; i >= 0; i--)
        {
            FireworkSpark sp = activeSparks[i];
            sp.age += dt;
            if (sp.age >= sp.maxAge)
            {
                sp.rt.gameObject.SetActive(false);
                sparkPool.Enqueue(sp);
                activeSparks.RemoveAt(i);
                continue;
            }

            sp.velocity.y -= 72f * dt;
            sp.rt.anchoredPosition += sp.velocity * dt;
            float alpha = Mathf.Pow(1f - (sp.age / sp.maxAge), 1.4f) * 0.85f;
            sp.img.color = new Color(sp.color.r, sp.color.g, sp.color.b, alpha);
        }
    }

    private void SpawnDistantFirework()
    {
        float ax = Random.Range(0.12f, 0.88f);
        float ay = Random.Range(0.58f, 0.88f);
        Color colA = FiestaPalette[Random.Range(0, FiestaPalette.Length)];
        Color colB = FiestaPalette[Random.Range(0, FiestaPalette.Length)];

        const int sparkCount = 12;
        for (int i = 0; i < sparkCount; i++)
        {
            FireworkSpark sp = GetSpark();
            sp.rt.anchorMin = new Vector2(ax, ay);
            sp.rt.anchorMax = new Vector2(ax, ay);
            sp.rt.anchoredPosition = Vector2.zero;
            float sz = (i % 2 == 0) ? 7f : 5f;
            sp.rt.sizeDelta = new Vector2(sz, sz);

            float angle = (i / (float)sparkCount) * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            float speed = (i % 2 == 0) ? Random.Range(68f, 96f) : Random.Range(38f, 60f);
            sp.velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
            sp.age = 0f;
            sp.maxAge = Random.Range(0.95f, 1.35f);
            sp.color = (i % 2 == 0) ? colA : colB;
            sp.img.color = sp.color;
            sp.rt.gameObject.SetActive(true);
            activeSparks.Add(sp);
        }
    }

    private FireworkSpark GetSpark()
    {
        if (sparkPool.Count > 0)
            return sparkPool.Dequeue();

        GameObject go = new GameObject("MenuFireworkSpark");
        go.transform.SetParent(animRoot, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.sprite = pixelSprite;
        img.raycastTarget = false;

        return new FireworkSpark { rt = rt, img = img };
    }

    private static Sprite BuildPixelSprite()
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }

    private static Sprite BuildSoftGlowSprite()
    {
        const int sz = 32;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear
        };
        float half = (sz - 1) * 0.5f;
        Color[] cols = new Color[sz * sz];
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(Mathf.Clamp01(1f - dist), 2.1f);
                cols[y * sz + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }
}
