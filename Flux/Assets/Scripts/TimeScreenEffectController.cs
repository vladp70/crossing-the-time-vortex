using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeScreenEffectController : MonoBehaviour
{
    private Player player;
    private Canvas uiCanvas;
    private GameObject effectContainer;
    private Image borderImage1;
    private Image borderImage2;
    private Texture2D proceduralVignette;

    private bool isPulseActive = false;

    private Image healthFill;
    private Image healthGhostFill;
    private Image manaFill;
    private Image manaGhostFill;
    private Text healthText;
    private Text manaText;

    void Start()
    {
        player = GetComponent<Player>();
        if (player == null) player = FindObjectOfType<Player>();

        CreateUIElements();
    }

    private Sprite CreateRoundedGradientSprite(int width, int height, int radius, Color colorStart, Color colorEnd, Color borderColor, int borderWidth)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Min(x, width - 1 - x);
                float dy = Mathf.Min(y, height - 1 - y);

                bool isCorner = dx < radius && dy < radius;
                float alpha = 1f;
                bool inside = true;

                if (isCorner)
                {
                    float cx = radius - dx;
                    float cy = radius - dy;
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);
                    if (dist > radius)
                    {
                        float diff = dist - radius;
                        if (diff < 1f)
                        {
                            alpha = 1f - diff;
                        }
                        else
                        {
                            inside = false;
                        }
                    }
                }

                if (!inside)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = (float)x / (width - 1);
                    Color fillColor = Color.Lerp(colorStart, colorEnd, t);

                    // Border check
                    bool isBorder = false;
                    if (borderWidth > 0)
                    {
                        if (isCorner)
                        {
                            float cx = radius - dx;
                            float cy = radius - dy;
                            float dist = Mathf.Sqrt(cx * cx + cy * cy);
                            if (dist > radius - borderWidth)
                            {
                                isBorder = true;
                            }
                        }
                        else
                        {
                            if (x < borderWidth || x >= width - borderWidth || y < borderWidth || y >= height - borderWidth)
                            {
                                isBorder = true;
                            }
                        }
                    }

                    Color finalColor = isBorder ? borderColor : fillColor;
                    finalColor.a *= alpha;
                    tex.SetPixel(x, y, finalColor);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateSymbolBadgeSprite(int size, string symbolType, Color fillColor, Color symbolColor, Color borderColor, int borderWidth)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float radius = size / 2f;

        Vector2[] boltPoly = null;
        if (symbolType == "lightning")
        {
            // Set up a beautifully shaped lightning bolt polygon inside the circle
            boltPoly = new Vector2[] {
                new Vector2(0.55f * size, 0.80f * size),
                new Vector2(0.25f * size, 0.45f * size),
                new Vector2(0.48f * size, 0.45f * size),
                new Vector2(0.35f * size, 0.15f * size),
                new Vector2(0.70f * size, 0.55f * size),
                new Vector2(0.48f * size, 0.55f * size)
            };
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 1f;
                bool insideCircle = true;

                if (dist > radius)
                {
                    float diff = dist - radius;
                    if (diff < 1f)
                    {
                        alpha = 1f - diff;
                    }
                    else
                    {
                        insideCircle = false;
                    }
                }

                if (!insideCircle)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    bool insideSymbol = false;
                    if (symbolType == "heart")
                    {
                        float cx = radius;
                        float cy = radius - size * 0.05f; // offset down slightly to center it
                        float scaleX = size * 0.40f; // wider horizontal scale
                        float scaleY = size * 0.32f; // shorter vertical scale
                        float u = (x - cx) / scaleX;
                        float v = (y - cy) / scaleY;
                        float term = v - Mathf.Sqrt(Mathf.Abs(u)) * 0.60f; // rounder lobes
                        insideSymbol = (u * u + term * term) < 0.5f;
                    }
                    else if (symbolType == "lightning")
                    {
                        Vector2 p = new Vector2(x, y);
                        insideSymbol = IsPointInPolygon(p, boltPoly);
                    }

                    // Border check
                    bool isBorder = false;
                    if (borderWidth > 0 && dist > radius - borderWidth)
                    {
                        isBorder = true;
                    }

                    Color finalColor;
                    if (isBorder)
                    {
                        finalColor = borderColor;
                    }
                    else if (insideSymbol)
                    {
                        finalColor = symbolColor;
                    }
                    else
                    {
                        finalColor = fillColor;
                    }

                    finalColor.a *= alpha;
                    tex.SetPixel(x, y, finalColor);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private bool IsPointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if ((poly[i].y < p.y && poly[j].y >= p.y || poly[j].y < p.y && poly[i].y >= p.y)
                && (poly[i].x + (p.y - poly[i].y) / (poly[j].y - poly[i].y) * (poly[j].x - poly[i].x) < p.x))
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    private void CreateUIElements()
    {
        // 1. Find or create a Canvas
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            uiCanvas = existingCanvas;
        }
        else
        {
            GameObject canvasObj = new GameObject("TimeEffectCanvas");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Create a procedural soft radial vignette texture
        proceduralVignette = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = (x - 64f) / 64f;
                float dy = (y - 64f) / 64f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Soft edges transition from 0.45 to 0.95
                float alpha = Mathf.Clamp01((dist - 0.45f) / 0.5f);
                
                // Base purple color with transparency
                Color c = new Color(0.6f, 0.15f, 0.95f, alpha * 0.75f);
                proceduralVignette.SetPixel(x, y, c);
            }
        }
        proceduralVignette.Apply();

        // 3. Create the container panel
        effectContainer = new GameObject("TimeEffectPanel");
        effectContainer.transform.SetParent(uiCanvas.transform, false);
        
        RectTransform containerRect = effectContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;

        CanvasGroup cg = effectContainer.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // 4. Create two large square layers that rotate to simulate energy flow
        float size = Mathf.Max(Screen.width, Screen.height) * 1.6f;

        GameObject imgObj1 = new GameObject("EnergyLayer1");
        imgObj1.transform.SetParent(effectContainer.transform, false);
        borderImage1 = imgObj1.AddComponent<Image>();
        borderImage1.sprite = Sprite.Create(proceduralVignette, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        borderImage1.color = new Color(1f, 1f, 1f, 0.65f);
        RectTransform rt1 = imgObj1.GetComponent<RectTransform>();
        rt1.sizeDelta = new Vector2(size, size);

        GameObject imgObj2 = new GameObject("EnergyLayer2");
        imgObj2.transform.SetParent(effectContainer.transform, false);
        borderImage2 = imgObj2.AddComponent<Image>();
        borderImage2.sprite = Sprite.Create(proceduralVignette, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        borderImage2.color = new Color(0.85f, 0.6f, 1f, 0.5f);
        RectTransform rt2 = imgObj2.GetComponent<RectTransform>();
        rt2.sizeDelta = new Vector2(size, size);

        // 5. Create Stats Panel Container (Health & Mana UI)
        GameObject statsPanel = new GameObject("StatsPanel");
        statsPanel.transform.SetParent(uiCanvas.transform, false);
        
        RectTransform panelRt = statsPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f); // top-left anchor
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = new Vector2(20f, -20f);
        panelRt.sizeDelta = new Vector2(275f, 90f);

        Image panelBg = statsPanel.AddComponent<Image>();
        // Dark semi-transparent background with border and rounded corners
        panelBg.sprite = CreateRoundedGradientSprite(275, 90, 12, 
            new Color(0.08f, 0.08f, 0.12f, 0.85f), 
            new Color(0.08f, 0.08f, 0.12f, 0.85f), 
            new Color(0.45f, 0.45f, 0.55f, 0.6f), 2);

        // Shadow for the panel
        Shadow panelShadow = statsPanel.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        panelShadow.effectDistance = new Vector2(2f, -2f);

        // Colors
        Color slotFill = new Color(0.04f, 0.04f, 0.05f, 0.75f);
        Color slotBorder = new Color(0.2f, 0.2f, 0.22f, 0.5f);

        // --- HEALTH ROW ---
        // A. Health Badge
        GameObject hpBadge = new GameObject("HealthBadge");
        hpBadge.transform.SetParent(statsPanel.transform, false);
        RectTransform hpBadgeRt = hpBadge.AddComponent<RectTransform>();
        hpBadgeRt.anchorMin = new Vector2(0f, 1f);
        hpBadgeRt.anchorMax = new Vector2(0f, 1f);
        hpBadgeRt.pivot = new Vector2(0f, 1f);
        hpBadgeRt.anchoredPosition = new Vector2(15f, -12f);
        hpBadgeRt.sizeDelta = new Vector2(24f, 24f);

        Image hpBadgeImg = hpBadge.AddComponent<Image>();
        hpBadgeImg.sprite = CreateSymbolBadgeSprite(24, "heart", 
            new Color(0.18f, 0.05f, 0.06f, 0.9f), 
            new Color(1.0f, 0.3f, 0.35f, 1.0f), 
            new Color(0.6f, 0.1f, 0.15f, 0.7f), 1);

        // B. Health Bar Background
        GameObject hpBarBg = new GameObject("HealthBar_Bg");
        hpBarBg.transform.SetParent(statsPanel.transform, false);
        RectTransform hpBgRt = hpBarBg.AddComponent<RectTransform>();
        hpBgRt.anchorMin = new Vector2(0f, 1f);
        hpBgRt.anchorMax = new Vector2(0f, 1f);
        hpBgRt.pivot = new Vector2(0f, 1f);
        hpBgRt.anchoredPosition = new Vector2(45f, -12f);
        hpBgRt.sizeDelta = new Vector2(215f, 24f);
        
        Image hpBgImg = hpBarBg.AddComponent<Image>();
        hpBgImg.sprite = CreateRoundedGradientSprite(215, 24, 6, slotFill, slotFill, slotBorder, 1);

        // C. Health Ghost Fill (Behind)
        GameObject hpBarGhost = new GameObject("HealthBar_Ghost");
        hpBarGhost.transform.SetParent(hpBarBg.transform, false);
        RectTransform hpGhostRt = hpBarGhost.AddComponent<RectTransform>();
        hpGhostRt.anchorMin = Vector2.zero;
        hpGhostRt.anchorMax = Vector2.one;
        hpGhostRt.sizeDelta = new Vector2(-4f, -4f); // slight inset
        hpGhostRt.anchoredPosition = Vector2.zero;
        
        healthGhostFill = hpBarGhost.AddComponent<Image>();
        healthGhostFill.sprite = CreateRoundedGradientSprite(211, 20, 5, 
            new Color(0.95f, 0.65f, 0.4f, 0.8f), 
            new Color(0.95f, 0.65f, 0.4f, 0.8f), 
            Color.clear, 0);
        healthGhostFill.type = Image.Type.Filled;
        healthGhostFill.fillMethod = Image.FillMethod.Horizontal;
        healthGhostFill.fillOrigin = 0;

        // D. Health Main Fill (On Top)
        GameObject hpBarFill = new GameObject("HealthBar_Fill");
        hpBarFill.transform.SetParent(hpBarBg.transform, false);
        RectTransform hpFillRt = hpBarFill.AddComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.sizeDelta = new Vector2(-4f, -4f); // slight inset
        hpFillRt.anchoredPosition = Vector2.zero;
        
        healthFill = hpBarFill.AddComponent<Image>();
        healthFill.sprite = CreateRoundedGradientSprite(211, 20, 5, 
            new Color(0.85f, 0.12f, 0.18f, 1.0f), 
            new Color(1.0f, 0.45f, 0.15f, 1.0f), 
            Color.clear, 0);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = 0;

        // E. Health Text
        GameObject hpTextObj = new GameObject("HealthText");
        hpTextObj.transform.SetParent(hpBarBg.transform, false);
        RectTransform hpTextRt = hpTextObj.AddComponent<RectTransform>();
        hpTextRt.anchorMin = Vector2.zero;
        hpTextRt.anchorMax = Vector2.one;
        hpTextRt.sizeDelta = Vector2.zero;
        
        healthText = hpTextObj.AddComponent<Text>();
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 11;
        healthText.fontStyle = FontStyle.Bold;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
        healthText.text = "HP: 100 / 100";

        Outline hpTextOutline = hpTextObj.AddComponent<Outline>();
        hpTextOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        hpTextOutline.effectDistance = new Vector2(1f, -1f);

        // --- MANA ROW ---
        // A. Mana Badge
        GameObject mpBadge = new GameObject("ManaBadge");
        mpBadge.transform.SetParent(statsPanel.transform, false);
        RectTransform mpBadgeRt = mpBadge.AddComponent<RectTransform>();
        mpBadgeRt.anchorMin = new Vector2(0f, 1f);
        mpBadgeRt.anchorMax = new Vector2(0f, 1f);
        mpBadgeRt.pivot = new Vector2(0f, 1f);
        mpBadgeRt.anchoredPosition = new Vector2(15f, -50f);
        mpBadgeRt.sizeDelta = new Vector2(24f, 24f);

        Image mpBadgeImg = mpBadge.AddComponent<Image>();
        mpBadgeImg.sprite = CreateSymbolBadgeSprite(24, "lightning", 
            new Color(0.05f, 0.1f, 0.18f, 0.9f), 
            new Color(0.2f, 0.8f, 1.0f, 1.0f), 
            new Color(0.1f, 0.5f, 0.7f, 0.7f), 1);

        // B. Mana Bar Background
        GameObject mpBarBg = new GameObject("ManaBar_Bg");
        mpBarBg.transform.SetParent(statsPanel.transform, false);
        RectTransform mpBgRt = mpBarBg.AddComponent<RectTransform>();
        mpBgRt.anchorMin = new Vector2(0f, 1f);
        mpBgRt.anchorMax = new Vector2(0f, 1f);
        mpBgRt.pivot = new Vector2(0f, 1f);
        mpBgRt.anchoredPosition = new Vector2(45f, -50f);
        mpBgRt.sizeDelta = new Vector2(215f, 24f);
        
        Image mpBgImg = mpBarBg.AddComponent<Image>();
        mpBgImg.sprite = CreateRoundedGradientSprite(215, 24, 6, slotFill, slotFill, slotBorder, 1);

        // C. Mana Ghost Fill (Behind)
        GameObject mpBarGhost = new GameObject("ManaBar_Ghost");
        mpBarGhost.transform.SetParent(mpBarBg.transform, false);
        RectTransform mpGhostRt = mpBarGhost.AddComponent<RectTransform>();
        mpGhostRt.anchorMin = Vector2.zero;
        mpGhostRt.anchorMax = Vector2.one;
        mpGhostRt.sizeDelta = new Vector2(-4f, -4f);
        mpGhostRt.anchoredPosition = Vector2.zero;
        
        manaGhostFill = mpBarGhost.AddComponent<Image>();
        manaGhostFill.sprite = CreateRoundedGradientSprite(211, 20, 5, 
            new Color(0.4f, 0.85f, 0.95f, 0.8f), 
            new Color(0.4f, 0.85f, 0.95f, 0.8f), 
            Color.clear, 0);
        manaGhostFill.type = Image.Type.Filled;
        manaGhostFill.fillMethod = Image.FillMethod.Horizontal;
        manaGhostFill.fillOrigin = 0;

        // D. Mana Main Fill (On Top)
        GameObject mpBarFill = new GameObject("ManaBar_Fill");
        mpBarFill.transform.SetParent(mpBarBg.transform, false);
        RectTransform mpFillRt = mpBarFill.AddComponent<RectTransform>();
        mpFillRt.anchorMin = Vector2.zero;
        mpFillRt.anchorMax = Vector2.one;
        mpFillRt.sizeDelta = new Vector2(-4f, -4f);
        mpFillRt.anchoredPosition = Vector2.zero;
        
        manaFill = mpBarFill.AddComponent<Image>();
        manaFill.sprite = CreateRoundedGradientSprite(211, 20, 5, 
            new Color(0.0f, 0.35f, 0.9f, 1.0f), 
            new Color(0.05f, 0.85f, 0.95f, 1.0f), 
            Color.clear, 0);
        manaFill.type = Image.Type.Filled;
        manaFill.fillMethod = Image.FillMethod.Horizontal;
        manaFill.fillOrigin = 0;

        // E. Mana Text
        GameObject mpTextObj = new GameObject("ManaText");
        mpTextObj.transform.SetParent(mpBarBg.transform, false);
        RectTransform mpTextRt = mpTextObj.AddComponent<RectTransform>();
        mpTextRt.anchorMin = Vector2.zero;
        mpTextRt.anchorMax = Vector2.one;
        mpTextRt.sizeDelta = Vector2.zero;
        
        manaText = mpTextObj.AddComponent<Text>();
        manaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        manaText.fontSize = 11;
        manaText.fontStyle = FontStyle.Bold;
        manaText.alignment = TextAnchor.MiddleCenter;
        manaText.color = Color.white;
        manaText.text = "Mana: 50 / 100";

        Outline mpTextOutline = mpTextObj.AddComponent<Outline>();
        mpTextOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        mpTextOutline.effectDistance = new Vector2(1f, -1f);
    }

    void Update()
    {
        // Update Health & Mana values
        if (player != null && healthFill != null && manaFill != null)
        {
            float currentHp = player.health;
            float maxHp = player.maxHealth;
            float hpPercent = Mathf.Clamp01(currentHp / maxHp);
            
            // Snap main fill instantly for immediate feedback
            healthFill.fillAmount = hpPercent;
            
            // Smoothed ghost bar trailing behind
            if (healthGhostFill != null)
            {
                if (healthGhostFill.fillAmount < hpPercent)
                {
                    healthGhostFill.fillAmount = hpPercent; // rise instantly
                }
                else
                {
                    healthGhostFill.fillAmount = Mathf.Lerp(healthGhostFill.fillAmount, hpPercent, Time.deltaTime * 2.5f);
                }
            }
            healthText.text = "HP: " + Mathf.RoundToInt(currentHp) + " / " + Mathf.RoundToInt(maxHp);

            float currentMp = player.mana;
            float maxMp = 100f; // Max mana cap is 100f
            float mpPercent = Mathf.Clamp01(currentMp / maxMp);
            
            // Snap main fill instantly for spelling feedback
            manaFill.fillAmount = mpPercent;
            
            // Smoothed ghost bar trailing behind
            if (manaGhostFill != null)
            {
                if (manaGhostFill.fillAmount < mpPercent)
                {
                    manaGhostFill.fillAmount = mpPercent; // rise instantly
                }
                else
                {
                    manaGhostFill.fillAmount = Mathf.Lerp(manaGhostFill.fillAmount, mpPercent, Time.deltaTime * 2.5f);
                }
            }
            manaText.text = "Mana: " + Mathf.RoundToInt(currentMp) + " / " + Mathf.RoundToInt(maxMp);
        }
        if (effectContainer == null || isPulseActive) return;

        bool isRewinding = player != null && player.IsReversing;
        bool isTimeFrozen = false;

        // Find if time is frozen
        TimeFreezableObject[] freezables = FindObjectsOfType<TimeFreezableObject>();
        foreach (var f in freezables)
        {
            if (f.IsFrozen)
            {
                isTimeFrozen = true;
                break;
            }
        }

        CanvasGroup cg = effectContainer.GetComponent<CanvasGroup>();

        if (isRewinding)
        {
            cg.alpha = Mathf.Lerp(cg.alpha, 0.85f, Time.deltaTime * 6f);
            
            // Rapid counter-rotation for flow
            borderImage1.transform.rotation = Quaternion.Euler(0, 0, Time.time * 50f);
            borderImage2.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 75f);

            // Shifting scale
            float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.04f;
            borderImage1.transform.localScale = new Vector3(pulse, pulse, 1f);
            borderImage2.transform.localScale = new Vector3(1f / pulse, 1f / pulse, 1f);
        }
        else if (isTimeFrozen)
        {
            cg.alpha = Mathf.Lerp(cg.alpha, 0.6f, Time.deltaTime * 4f);
            
            // Slow rotation for stasis
            borderImage1.transform.rotation = Quaternion.Euler(0, 0, Time.time * 8f);
            borderImage2.transform.rotation = Quaternion.Euler(0, 0, -Time.time * 12f);

            float pulse = 1.02f + Mathf.Sin(Time.time * 3f) * 0.015f;
            borderImage1.transform.localScale = new Vector3(pulse, pulse, 1f);
            borderImage2.transform.localScale = new Vector3(pulse, pulse, 1f);
        }
        else
        {
            cg.alpha = Mathf.Lerp(cg.alpha, 0f, Time.deltaTime * 3f);
        }
    }

    public void TriggerFreezePulse(float duration)
    {
        StartCoroutine(DoFreezePulse(duration));
    }

    private IEnumerator DoFreezePulse(float duration)
    {
        isPulseActive = true;
        CanvasGroup cg = effectContainer.GetComponent<CanvasGroup>();
        
        // Quick flash to 100% opacity
        cg.alpha = 1.0f;
        
        // Transition visually to neon-cyan energy
        borderImage1.color = new Color(0.1f, 0.75f, 1f, 0.9f);
        borderImage2.color = new Color(0.2f, 0.6f, 1f, 0.7f);

        // Quick rotate/scale flare
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.35f;
            
            borderImage1.transform.rotation = Quaternion.Euler(0, 0, t * 90f);
            borderImage2.transform.rotation = Quaternion.Euler(0, 0, -t * 120f);
            
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
            borderImage1.transform.localScale = new Vector3(scale, scale, 1f);
            borderImage2.transform.localScale = new Vector3(scale, scale, 1f);
            
            // Fade slightly
            cg.alpha = Mathf.Lerp(1.0f, 0.6f, t);
            
            yield return null;
        }

        // Restore normal purple colors
        borderImage1.color = new Color(1f, 1f, 1f, 0.65f);
        borderImage2.color = new Color(0.85f, 0.6f, 1f, 0.5f);
        
        isPulseActive = false;
    }
}
