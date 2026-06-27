using UnityEngine;

//==============================================================================
// GameRoot
//==============================================================================
// The only singleton entry point for project-wide controller access.
//==============================================================================
[DisallowMultipleComponent]
public sealed class GameRoot : MonoBehaviour
{
    // Project-wide singleton instance.
    private static GameRoot _instance;

    // Stock rhythm game flow controller owned by GameRoot.
    private StockRhythmController _stockRhythmController;

    // Project-wide singleton accessor.
    public static GameRoot Instance
    {
        get => _instance;
        private set => _instance = value;
    }

    // Stock rhythm controller accessor.
    public StockRhythmController StockRhythmController
    {
        get => _stockRhythmController;
        private set => _stockRhythmController = value;
    }

    // Initializes the singleton root and constructs controllers.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StockRhythmController = new StockRhythmController();
        StockRhythmController.Construct(this);
        StockRhythmController.Initialize();
    }

    // Ticks owned controllers from the Unity frame loop.
    private void Update()
    {
        StockRhythmController?.Tick(Time.deltaTime);
    }

    // Clears the singleton reference when this root is destroyed.
    private void OnDestroy()
    {
        if (Instance == this)
        {
            StockRhythmController?.Dispose();
            Instance = null;
        }
    }
}
