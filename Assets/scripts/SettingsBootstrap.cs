
using UnityEngine;
using UnityEngine.Audio;

public class SettingsBootstrap : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string EffectsKey = "EffectsVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string WidthKey = "ScreenWidth";
    private const string HeightKey = "ScreenHeight";

    private void Start()
    {
        LoadAudioSettings();
        LoadGraphicsSettings();
    }

    private void LoadAudioSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogWarning(
                "SettingsBootstrap: AudioMixer is not assigned.");
            return;
        }

        SetVolume(
            "MasterVolume",
            PlayerPrefs.GetFloat(MasterKey, 1f));

        SetVolume(
            "MusicVolume",
            PlayerPrefs.GetFloat(MusicKey, 1f));

        SetVolume(
            "EffectsVolume",
            PlayerPrefs.GetFloat(EffectsKey, 1f));
    }

    private void SetVolume(string parameter, float value)
    {
        float decibels = value <= 0.0001f
            ? -80f
            : Mathf.Log10(value) * 20f;

        if (!audioMixer.SetFloat(parameter, decibels))
        {
            Debug.LogWarning(
                $"SettingsBootstrap: parameter '{parameter}' " +
                "is missing or not exposed.");
        }
    }

    private void LoadGraphicsSettings()
    {
        bool hasResolution =
            PlayerPrefs.HasKey(WidthKey) &&
            PlayerPrefs.HasKey(HeightKey);

        bool hasFullscreen = PlayerPrefs.HasKey(FullscreenKey);

        // On first launch, leave the game's default display mode alone.
        if (!hasResolution && !hasFullscreen)
            return;

        int width = PlayerPrefs.GetInt(WidthKey, Screen.width);
        int height = PlayerPrefs.GetInt(HeightKey, Screen.height);

        bool fullscreen = PlayerPrefs.GetInt(
            FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        FullScreenMode mode = fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(width, height, mode);
    }
}
