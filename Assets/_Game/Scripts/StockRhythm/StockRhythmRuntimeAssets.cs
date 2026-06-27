using UnityEngine;

//==============================================================================
// StockRhythmRuntimeAssets
//==============================================================================
// Builds and owns generated fonts, sprites, materials, and short audio clips.
//==============================================================================
internal sealed class StockRhythmRuntimeAssets
{
    // Root dependency injected by StockRhythmController.
    private GameRoot _gameRoot;

    // Runtime audio source for generated click and hit sounds.
    private AudioSource _audioSource;

    // Dynamic UI font for Korean and English labels.
    public Font UiFont { get; private set; }

    // Shared line material for generated LineRenderers.
    public Material LineMaterial { get; private set; }

    // Solid rectangle sprite used by generated UI and world objects.
    public Sprite RectSprite { get; private set; }

    // Filled circle sprite used by the timing marker.
    public Sprite CircleSprite { get; private set; }

    // Ring sprite used by the timing marker.
    public Sprite RingSprite { get; private set; }

    // Soft circle sprite used for glows.
    public Sprite SoftCircleSprite { get; private set; }

    // Up arrow sprite used for BUY notes and buttons.
    public Sprite UpTriangleSprite { get; private set; }

    // Down arrow sprite used for SELL notes and buttons.
    public Sprite DownTriangleSprite { get; private set; }

    // Generated beat click sound.
    public AudioClip BeatClip { get; private set; }

    // Generated successful hit sound.
    public AudioClip HitClip { get; private set; }

    // Generated miss sound.
    public AudioClip MissClip { get; private set; }

    // Injects GameRoot dependency.
    public void Construct(GameRoot gameRoot)
    {
        _gameRoot = gameRoot;
    }

    // Builds reusable generated sprites, materials, font, and audio clips.
    public void Initialize()
    {
        UiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Segoe UI", "Arial" }, 32);
        RectSprite = SpriteFromTexture(CreateSolidTexture(Color.white), 1f);
        CircleSprite = SpriteFromTexture(CreateCircleTexture(96, false, Color.white), 96f);
        RingSprite = SpriteFromTexture(CreateRingTexture(128, Color.white), 128f);
        SoftCircleSprite = SpriteFromTexture(CreateCircleTexture(128, true, Color.white), 128f);
        UpTriangleSprite = SpriteFromTexture(CreateTriangleTexture(96, true, Color.white), 96f);
        DownTriangleSprite = SpriteFromTexture(CreateTriangleTexture(96, false, Color.white), 96f);
        LineMaterial = new Material(Shader.Find("Sprites/Default"));
        LineMaterial.hideFlags = HideFlags.HideAndDontSave;

        _audioSource = _gameRoot.gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.volume = 0.55f;
        BeatClip = CreateToneClip(110f, 0.055f, 0.18f);
        HitClip = CreateToneClip(880f, 0.08f, 0.25f);
        MissClip = CreateToneClip(92f, 0.12f, 0.35f);
    }

    // Releases transient runtime resources.
    public void Dispose()
    {
        if (LineMaterial != null)
        {
            Object.Destroy(LineMaterial);
        }
    }

    // Plays a one-shot generated audio clip.
    public void PlayClip(AudioClip clip, float volumeScale)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip, volumeScale);
        }
    }

    // Creates a short generated sine-wave tone.
    private static AudioClip CreateToneClip(float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            float envelope = 1f - (float)i / sampleCount;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create("Tone " + frequency, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // Creates a one-pixel solid texture.
    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Creates a filled or soft circle texture.
    private static Texture2D CreateCircleTexture(int size, bool soft, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = soft ? Mathf.Clamp01(1f - distance) : (distance <= 1f ? 1f : 0f);
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Creates a ring texture for the timing marker.
    private static Texture2D CreateRingTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = distance > 0.62f && distance < 0.96f ? 1f : 0f;
                texture.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Creates an up or down triangle texture for note icons.
    private static Texture2D CreateTriangleTexture(int size, bool up, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedX = Mathf.Abs((x + 0.5f) / size - 0.5f) * 2f;
                float normalizedY = (y + 0.5f) / size;
                float edge = up ? normalizedY : 1f - normalizedY;
                bool inside = edge > 0.16f && normalizedX < edge * 0.95f;
                texture.SetPixel(x, y, inside ? color : Color.clear);
            }
        }

        texture.filterMode = FilterMode.Bilinear;
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    // Wraps a generated texture in a sprite.
    private static Sprite SpriteFromTexture(Texture2D texture, float pixelsPerUnit)
    {
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
