using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    private const string KEY_MUSIC = "Settings_MusicVol";
    private const string KEY_SFX = "Settings_SFXVol";
    private const string KEY_MENTOR = "Settings_MentorHints";

    public float MusicVolume { get; private set; } = 0.35f;
    public float SFXVolume { get; private set; } = 0.85f;
    public bool MentorHints { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        MusicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 0.35f);
        SFXVolume = PlayerPrefs.GetFloat(KEY_SFX, 0.85f);
        MentorHints = PlayerPrefs.GetInt(KEY_MENTOR, 1) == 1;
    }

    private void Start()
    {
        AudioManager.Instance?.SetMusicVolume(MusicVolume);
        AudioManager.Instance?.SetSFXVolume(SFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        AudioManager.Instance?.SetMusicVolume(MusicVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        AudioManager.Instance?.SetSFXVolume(SFXVolume);
        PlayerPrefs.SetFloat(KEY_SFX, SFXVolume);
        PlayerPrefs.Save();
    }

    public void SetMentorHints(bool value)
    {
        MentorHints = value;
        PlayerPrefs.SetInt(KEY_MENTOR, value ? 1 : 0);
        PlayerPrefs.Save();

        if (value)
            TutorialManager.Instance?.ResetAll();
    }
}