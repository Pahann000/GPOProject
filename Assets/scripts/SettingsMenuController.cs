
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuController : MonoBehaviour
{
	[Header("Audio")]
	[SerializeField] private AudioMixer audioMixer;
	[SerializeField] private Slider generalSlider;
	[SerializeField] private Slider musicSlider;
	[SerializeField] private Slider effectsSlider;

	[Header("Graphics")]
	[SerializeField] private TMP_Dropdown resolutionDropdown;
	[SerializeField] private Button fullscreenButton;
	[SerializeField] private TMP_Text fullscreenText;

	[Header("Apply")]
	[SerializeField] private Button applyButton;

	private readonly List<Resolution> availableResolutions =
		new List<Resolution>();

	private bool pendingFullscreen;
	private int pendingResolutionIndex;

	private const string MasterKey = "MasterVolume";
	private const string MusicKey = "MusicVolume";
	private const string EffectsKey = "EffectsVolume";
	private const string FullscreenKey = "Fullscreen";
	private const string WidthKey = "ScreenWidth";
	private const string HeightKey = "ScreenHeight";

	private void Start()
	{
		SetupResolutions();
		LoadSettingsIntoUI();
		ConnectEvents();
	}


	private void SetupResolutions()
	{
		resolutionDropdown.ClearOptions();
		availableResolutions.Clear();

		List<string> options = new List<string>();
		HashSet<string> addedOptions = new HashSet<string>();

		foreach (Resolution resolution in Screen.resolutions)
		{
			string key = $"{resolution.width}x{resolution.height}";

			if (addedOptions.Add(key))
			{
				availableResolutions.Add(resolution);
				options.Add(
					$"{resolution.width} x {resolution.height}");
			}
		}

		string currentKey = $"{Screen.width}x{Screen.height}";

		if (addedOptions.Add(currentKey))
		{
			availableResolutions.Add(new Resolution
			{
				width = Screen.width,
				height = Screen.height
			});

			options.Add($"{Screen.width} x {Screen.height}");
		}

		resolutionDropdown.AddOptions(options);
	}


	private void LoadSettingsIntoUI()
	{
		generalSlider.SetValueWithoutNotify(
			PlayerPrefs.GetFloat(MasterKey, 1f));

		musicSlider.SetValueWithoutNotify(
			PlayerPrefs.GetFloat(MusicKey, 1f));

		effectsSlider.SetValueWithoutNotify(
			PlayerPrefs.GetFloat(EffectsKey, 1f));

		pendingFullscreen = PlayerPrefs.GetInt(
			FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

		int savedWidth = PlayerPrefs.GetInt(
			WidthKey, Screen.width);

		int savedHeight = PlayerPrefs.GetInt(
			HeightKey, Screen.height);

		pendingResolutionIndex = 0;

		for (int i = 0; i < availableResolutions.Count; i++)
		{
			Resolution resolution = availableResolutions[i];

			if (resolution.width == savedWidth &&
				resolution.height == savedHeight)
			{
				pendingResolutionIndex = i;
				break;
			}
		}

		resolutionDropdown.SetValueWithoutNotify(
			pendingResolutionIndex);

		resolutionDropdown.RefreshShownValue();
		UpdateFullscreenLabel();
	}


	private void ConnectEvents()
	{
		generalSlider.onValueChanged.AddListener(
			OnGeneralVolumeChanged);

		musicSlider.onValueChanged.AddListener(
			OnMusicVolumeChanged);

		effectsSlider.onValueChanged.AddListener(
			OnEffectsVolumeChanged);

		resolutionDropdown.onValueChanged.AddListener(
			OnResolutionChanged);

		fullscreenButton.onClick.AddListener(
			ToggleFullscreen);

		applyButton.onClick.AddListener(
			ApplySettings);
	}

	private void OnGeneralVolumeChanged(float value)
	{
		// Intentionally do not apply until Apply is pressed.
	}

	private void OnMusicVolumeChanged(float value)
	{
		// Intentionally do not apply until Apply is pressed.
	}

	private void OnEffectsVolumeChanged(float value)
	{
		// Intentionally do not apply until Apply is pressed.
	}

	private void OnResolutionChanged(int index)
	{
		pendingResolutionIndex = index;
	}

	private void ToggleFullscreen()
	{
		pendingFullscreen = !pendingFullscreen;
		UpdateFullscreenLabel();
	}

	private void UpdateFullscreenLabel()
	{
		if (fullscreenText != null)
		{
			fullscreenText.text =
				pendingFullscreen ? "YES" : "NO";
		}
	}


	private void ApplySettings()
	{
		ApplyAudioSettings();
		ApplyGraphicsSettings();
		SaveSettings();

		Debug.Log("Redsoil: settings applied and saved.");
	}

	private void ApplyAudioSettings()
	{
		if (audioMixer == null)
		{
			Debug.LogWarning(
				"Settings: AudioMixer is not assigned.");
			return;
		}

		SetMixerVolume("MasterVolume", generalSlider.value);
		SetMixerVolume("MusicVolume", musicSlider.value);
		SetMixerVolume("EffectsVolume", effectsSlider.value);
	}

	private void SetMixerVolume(string parameter, float value)
	{
		float decibels = value <= 0.0001f
			? -80f
			: Mathf.Log10(value) * 20f;

		if (!audioMixer.SetFloat(parameter, decibels))
		{
			Debug.LogWarning(
				$"Settings: AudioMixer parameter '{parameter}' " +
				"is missing or not exposed.");
		}
	}

	private void ApplyGraphicsSettings()
	{
		if (availableResolutions.Count == 0)
			return;

		pendingResolutionIndex = Mathf.Clamp(
			pendingResolutionIndex,
			0,
			availableResolutions.Count - 1);

		Resolution resolution =
			availableResolutions[pendingResolutionIndex];

		FullScreenMode mode = pendingFullscreen
			? FullScreenMode.FullScreenWindow
			: FullScreenMode.Windowed;

		Screen.SetResolution(
			resolution.width,
			resolution.height,
			mode);
	}

	private void SaveSettings()
	{
		PlayerPrefs.SetFloat(
			MasterKey, generalSlider.value);

		PlayerPrefs.SetFloat(
			MusicKey, musicSlider.value);

		PlayerPrefs.SetFloat(
			EffectsKey, effectsSlider.value);

		PlayerPrefs.SetInt(
			FullscreenKey, pendingFullscreen ? 1 : 0);

		if (availableResolutions.Count > 0)
		{
			Resolution resolution =
				availableResolutions[pendingResolutionIndex];

			PlayerPrefs.SetInt(WidthKey, resolution.width);
			PlayerPrefs.SetInt(HeightKey, resolution.height);
		}

		PlayerPrefs.Save();
	}

	private void OnDestroy()
	{
		generalSlider.onValueChanged.RemoveListener(
			OnGeneralVolumeChanged);

		musicSlider.onValueChanged.RemoveListener(
			OnMusicVolumeChanged);

		effectsSlider.onValueChanged.RemoveListener(
			OnEffectsVolumeChanged);

		resolutionDropdown.onValueChanged.RemoveListener(
			OnResolutionChanged);

		fullscreenButton.onClick.RemoveListener(
			ToggleFullscreen);

		applyButton.onClick.RemoveListener(
			ApplySettings);
	}
}
