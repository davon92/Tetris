using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The first scene the game loads. It owns everything that outlives a single
/// scene — saved settings, input profiles, the audio host — and then hands off
/// to the menu scene, which is left holding only what a match actually needs.
/// </summary>
/// <remarks>
/// <see cref="EnsureInitialized"/> is idempotent and is also called by
/// <see cref="GameFlowController"/>, so entering play mode straight into the
/// game scene still works. That matters more than purity: a bootstrap you have
/// to route through to test anything is a bootstrap people route around.
/// </remarks>
public sealed class GameBootstrap : MonoBehaviour
{
    /// <summary>Where <c>Bootstrap.unity</c> lives. The editor tooling writes it here.</summary>
    public const string BootstrapScenePath = "Assets/Tetris/Scenes/Bootstrap.unity";

    public const string DefaultNextScene = "SampleScene";

    private static bool initialized;

    [Tooltip("Scene to load once global systems are up. Must be in Build Settings.")]
    [SerializeField] private string nextScene = DefaultNextScene;

    /// <summary>
    /// Brings up every global system exactly once per play session. Safe to
    /// call from anywhere; later calls are no-ops.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        // Preferences first: they set the audio levels the very first clip is
        // mixed at, and the bindings the first frame of input is read against.
        GameSettings.Load();
        PlayerInputProfiles.LoadAll();

        // Touching Instance builds the persistent audio host if it is missing.
        GameAudio audio = GameAudio.Instance;
        if (audio != null)
            audio.MarkPersistent();
    }

    /// <summary>
    /// Play mode does not reset statics when domain reloading is disabled, so a
    /// second run would otherwise skip initialization entirely.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        initialized = false;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        // Loading from Start rather than Awake gives every other object in the
        // bootstrap scene its own Awake first.
        string target = string.IsNullOrWhiteSpace(nextScene) ? DefaultNextScene : nextScene;
        if (SceneManager.GetActiveScene().name == target)
            return;

        SceneManager.LoadScene(target);
    }
}
