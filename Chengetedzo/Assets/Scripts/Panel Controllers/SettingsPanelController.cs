using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Audio")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;

    [Header("Mentor")]
    public Toggle mentorHintsToggle;

    [Header("Navigation")]
    public Button closeButton;

    private void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        musicSlider.SetValueWithoutNotify(SettingsManager.Instance.MusicVolume);
        sfxSlider.SetValueWithoutNotify(SettingsManager.Instance.SFXVolume);
        mentorHintsToggle.SetIsOnWithoutNotify(SettingsManager.Instance.MentorHints);

        UpdateMusicLabel(SettingsManager.Instance.MusicVolume);
        UpdateSFXLabel(SettingsManager.Instance.SFXVolume);
    }

    private void Start()
    {
        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;
        sfxSlider.maxValue = 1f;

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        mentorHintsToggle.onValueChanged.AddListener(OnMentorToggled);
        closeButton.onClick.AddListener(UIManager.Instance.HideSettings);
    }

    private void OnDestroy()
    {
        musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
        mentorHintsToggle.onValueChanged.RemoveListener(OnMentorToggled);
    }

    private void OnMusicChanged(float value)
    {
        SettingsManager.Instance?.SetMusicVolume(value);
        UpdateMusicLabel(value);
    }

    private void OnSFXChanged(float value)
    {
        SettingsManager.Instance?.SetSFXVolume(value);
        AudioManager.Instance?.OnButtonClick();
        UpdateSFXLabel(value);
    }

    private void OnMentorToggled(bool value)
    {
        SettingsManager.Instance?.SetMentorHints(value);
    }

    private void UpdateMusicLabel(float value)
    {
        if (musicValueText != null)
            musicValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void UpdateSFXLabel(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}