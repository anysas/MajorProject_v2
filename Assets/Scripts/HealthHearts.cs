using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class HealthHearts : MonoBehaviour
{
    public const int MaxLives = 3;

    public static int Lives { get; private set; } = MaxLives;

    const float HeartSize = 104f;
    const float HeartGap = 12f;
    const float MarginLeft = 40f;
    const float MarginTop = 36f;

    static HealthHearts instance;

    Image[] hearts;
    float[] shown;
    GameObject healthRoot;
    Canvas hudCanvas;
    Sprite heartSprite;
    Texture2D heartTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        Lives = MaxLives;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BindIfPresent()
    {
        EnsureExists();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetForNewRun();
        if (instance != null)
        {
            return;
        }

        EnsureExists();
    }

    public static void ResetForNewRun()
    {
        Lives = MaxLives;
        if (instance != null)
        {
            instance.ResetHearts();
        }
    }

    public static HealthHearts EnsureExists()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<HealthHearts>();
        if (instance != null)
        {
            return instance;
        }

        GameObject holder = new GameObject("HealthHearts");
        return holder.AddComponent<HealthHearts>();
    }

    public static void LoseLife()
    {
        if (!ExperienceRestart.IsActive || Lives <= 0)
        {
            return;
        }

        Lives--;
        HealthHearts hud = EnsureExists();
        hud.RefreshVisibility();
    }

    void Awake()
    {
        instance = this;
        Lives = MaxLives;
        if (hearts == null && !BindSceneHearts())
        {
            BuildHud();
        }

        ResetHearts();
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (heartSprite != null)
        {
            Destroy(heartSprite);
        }

        if (heartTexture != null)
        {
            Destroy(heartTexture);
        }
    }

    void Update()
    {
        RefreshVisibility();
        if (hearts == null || shown == null)
        {
            return;
        }

        float step = Time.deltaTime * 3.2f;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
            {
                continue;
            }

            // Scoop 1 is index 0, so the first hit clears 1, then 2, then 3.
            int lost = Mathf.Clamp(MaxLives - Lives, 0, hearts.Length);
            float target = i < lost ? 0f : 1f;
            shown[i] = Mathf.MoveTowards(shown[i], target, step);
            float fill = shown[i];
            Color color = hearts[i].color;
            color.a = fill;
            hearts[i].color = color;

            float scale = 1f;
            if (i < lost && fill > 0.02f && fill < 0.98f)
            {
                scale = 1f + Mathf.Sin((1f - fill) * Mathf.PI) * 0.3f;
            }

            hearts[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void ResetHearts()
    {
        if (shown == null)
        {
            return;
        }

        for (int i = 0; i < shown.Length; i++)
        {
            shown[i] = 1f;
        }

        RefreshVisibility();
    }

    void RefreshVisibility()
    {
        if (healthRoot != null)
        {
            bool show = ExperienceRestart.HasStarted;
            if (show)
            {
                Canvas canvas = healthRoot.GetComponentInParent<Canvas>(true);
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                }
            }

            healthRoot.SetActive(show);
            return;
        }

        if (hudCanvas != null)
        {
            hudCanvas.enabled = ExperienceRestart.HasStarted;
        }
    }

    bool BindSceneHearts()
    {
        GameObject health = FindNamed("health");
        Image[] images = health != null
            ? health.GetComponentsInChildren<Image>(true)
            : FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (images == null || images.Length == 0)
        {
            return false;
        }

        Image[] scoops = new Image[MaxLives];
        int count = 0;
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            int number = image != null ? ScoopNumber(image.gameObject.name) : 0;
            if (number < 1 || number > MaxLives || scoops[number - 1] != null)
            {
                continue;
            }

            scoops[number - 1] = image;
            count++;
        }

        if (count == 0)
        {
            return false;
        }

        hearts = new Image[count];
        shown = new float[count];
        int used = 0;
        for (int i = 0; i < scoops.Length; i++)
        {
            if (scoops[i] == null)
            {
                continue;
            }

            hearts[used] = scoops[i];
            hearts[used].raycastTarget = false;
            shown[used] = 1f;
            used++;
        }

        healthRoot = health != null ? health : hearts[0].transform.parent.gameObject;
        return true;
    }

    static int ScoopNumber(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return 0;
        }

        if (objectName.IndexOf("white", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 0;
        }

        string trimmed = objectName.Trim();
        bool namedScoop = trimmed.StartsWith("scoop", System.StringComparison.OrdinalIgnoreCase);
        bool namedHeart = trimmed.StartsWith("heart", System.StringComparison.OrdinalIgnoreCase);
        if (!namedScoop && !namedHeart)
        {
            return 0;
        }

        int number = 0;
        bool foundDigit = false;
        for (int i = 0; i < trimmed.Length; i++)
        {
            if (!char.IsDigit(trimmed[i]))
            {
                continue;
            }

            foundDigit = true;
            number = number * 10 + (trimmed[i] - '0');
        }

        return foundDigit ? number : 0;
    }

    static GameObject FindNamed(string exactName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && string.Equals(transforms[i].name, exactName, System.StringComparison.OrdinalIgnoreCase))
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    void BuildHud()
    {
        heartSprite = LoadHeartSprite();

        GameObject canvasObject = new GameObject("HeartsCanvas");
        canvasObject.transform.SetParent(transform, false);

        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rowObject = new GameObject("Hearts");
        rowObject.transform.SetParent(canvasObject.transform, false);
        RectTransform row = rowObject.AddComponent<RectTransform>();
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(0f, 1f);
        row.pivot = new Vector2(0f, 1f);
        float width = MaxLives * HeartSize + (MaxLives - 1) * HeartGap;
        row.anchoredPosition = new Vector2(MarginLeft, -MarginTop);
        row.sizeDelta = new Vector2(width, HeartSize);

        hearts = new Image[MaxLives];
        shown = new float[MaxLives];
        for (int i = 0; i < MaxLives; i++)
        {
            GameObject heartObject = new GameObject("Heart " + (i + 1));
            heartObject.transform.SetParent(row, false);

            RectTransform rect = heartObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(i * (HeartSize + HeartGap), 0f);
            rect.sizeDelta = new Vector2(HeartSize, HeartSize);

            Image image = heartObject.AddComponent<Image>();
            image.sprite = heartSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            hearts[i] = image;
            shown[i] = 1f;
        }
    }

    Sprite LoadHeartSprite()
    {
        string[] candidates =
        {
            Path.Combine(Application.dataPath, "UI", "heart.png"),
            Path.Combine(Application.streamingAssetsPath, "heart.png")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (string.IsNullOrEmpty(candidates[i]) || !File.Exists(candidates[i]))
            {
                continue;
            }

            byte[] bytes = File.ReadAllBytes(candidates[i]);
            heartTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!heartTexture.LoadImage(bytes))
            {
                Destroy(heartTexture);
                heartTexture = null;
                continue;
            }

            heartTexture.wrapMode = TextureWrapMode.Clamp;
            heartTexture.filterMode = FilterMode.Bilinear;
            Rect crop = TightSpriteRect(heartTexture);
            return Sprite.Create(heartTexture, crop, new Vector2(0.5f, 0.5f), 100f);
        }

        heartTexture = CreateFallbackHeart(128);
        return Sprite.Create(heartTexture, new Rect(0f, 0f, heartTexture.width, heartTexture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Rect TightSpriteRect(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        int minX = width;
        int minY = height;
        int maxX = 0;
        int maxY = 0;
        bool any = false;
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[row + x].a <= 16)
                {
                    continue;
                }

                any = true;
                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (!any)
        {
            return new Rect(0f, 0f, width, height);
        }

        int pad = 6;
        minX = Mathf.Max(0, minX - pad);
        minY = Mathf.Max(0, minY - pad);
        maxX = Mathf.Min(width - 1, maxX + pad);
        maxY = Mathf.Min(height - 1, maxY + pad);
        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    static Texture2D CreateFallbackHeart(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 red = new Color32(210, 15, 25, 255);
        Color32[] pixels = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) / half;
                float ny = (y - half) / half * -1f;
                ny -= 0.15f;
                float a = nx * nx + ny * ny - 1f;
                bool inside = a * a * a - nx * nx * ny * ny * ny <= 0f;
                pixels[y * size + x] = inside ? red : clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }
}
