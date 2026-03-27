using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Ambience")]
    public AudioClip marketAmbience;
    public AudioClip birdsAmbience;
    public AudioClip sirenAmbience;

    [Header("Music")]
    public AudioClip calmMusic;     // marimba base
    public AudioClip tensionMusic;  // darker variation (optional)

    [Header("Time Notifications")]
    public AudioClip monthEnd;
    public AudioClip yearTransition;
    public AudioClip gameEnd;

    [Header("Event Popups")]
    [Tooltip("Small financial hit — clinic fee, transport delay. Soft notification tone.")]
    public AudioClip eventMinor;

    [Tooltip("Mid-weight hit — theft, livestock issue. Slightly heavier chime.")]
    public AudioClip eventModerate;

    [Tooltip("Serious event — house fire, breadwinner death. Low, sombre tone. NOT a dramatic sting.")]
    public AudioClip eventMajor;

    [Tooltip("Reward or opportunity event — bonus, grant. Warm, brief positive tone.")]
    public AudioClip eventPositive;

    [Header("Mentor Chat")]
    [Tooltip("Mentor message arrives — matches chat/messaging UI aesthetic. Like a WhatsApp chime.")]
    public AudioClip mentorMessage;

    [Header("Loan Chat")]
    [Tooltip("Loan message arrives — same messaging aesthetic as mentor but slightly more neutral.")]
    public AudioClip loanMessage;

    [Tooltip("Loan taken or confirmed.")]
    public AudioClip loanConfirm;

    [Header("Insurance Panel")]
    [Tooltip("Toggle a policy on — subtle, like ticking a form checkbox.")]
    public AudioClip insuranceToggleOn;

    [Tooltip("Toggle a policy off — slightly softer version of the same.")]
    public AudioClip insuranceToggleOff;

    [Tooltip("Insurance panel confirmed. Brief, reassuring.")]
    public AudioClip insuranceConfirm;

    [Header("Navigation")]
    [Tooltip("Panel opens — very soft, like turning a page.")]
    public AudioClip panelOpen;

    [Tooltip("General button click — subtle, not a game-UI click.")]
    public AudioClip buttonClick;

    [Header("Report Panel")]
    [Tooltip("Monthly report appears — paper/ledger aesthetic. Subtle rustle or soft thud.")]
    public AudioClip reportAppear;

    [Header("Forecast")]
    [Tooltip("Each forecast card slides in — very quiet, like setting down a card.")]
    public AudioClip forecastCard;

    [Header("Financial Feedback")]
    [Tooltip("Money gained — warm, brief tone.")]
    public AudioClip moneyGain;

    [Tooltip("Money lost — low, brief tone.")]
    public AudioClip moneyLoss;

    [Tooltip("Savings deposited.")]
    public AudioClip savingsDeposit;

    [Header("End of Year")]
    [Tooltip("Year complete — calm, reflective. Not a fanfare.")]
    public AudioClip yearComplete;

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume = 0.85f;
    [Range(0f, 1f)] public float musicVolume = 0.35f;

    private AudioSource _sfxSource;
    private AudioSource _musicSource;

    private AudioSource _ambienceSourceA;
    private AudioSource _ambienceSourceB;
    private AudioSource _ambienceSourceC;
    private AudioLowPassFilter _musicLowPass;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        var sources = GetComponents<AudioSource>();

        _sfxSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        _musicSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        _ambienceSourceA = sources.Length > 2 ? sources[2] : gameObject.AddComponent<AudioSource>();
        _ambienceSourceB = sources.Length > 3 ? sources[3] : gameObject.AddComponent<AudioSource>();
        _ambienceSourceC = sources.Length > 4 ? sources[4] : gameObject.AddComponent<AudioSource>();

        _musicSource.loop = true;
        _ambienceSourceA.loop = true;
        _ambienceSourceB.loop = true;
        _ambienceSourceC.loop = true;

        _sfxSource.playOnAwake = false;
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicLowPass = _musicSource.gameObject.GetComponent<AudioLowPassFilter>();

        if (_musicLowPass == null)
            _musicLowPass = _musicSource.gameObject.AddComponent<AudioLowPassFilter>();
    }

    public void PlayAmbience()
    {
        PlayLoop(_ambienceSourceA, marketAmbience, 0.5f);
        PlayLoop(_ambienceSourceB, birdsAmbience, 0.3f);
        PlayLoop(_ambienceSourceC, sirenAmbience, 0.1f);
    }

    private void PlayLoop(AudioSource source, AudioClip clip, float volume)
    {
        if (clip == null) return;

        source.clip = clip;
        source.volume = volume;
        source.Play();
    }
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || _musicSource.clip == clip) return;

        _musicSource.clip = clip;
        _musicSource.volume = musicVolume;
        _musicSource.pitch = 1f;
        _musicSource.Play();
    }

    public void StopMusic() => _musicSource.Stop();
    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); }
    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); _musicSource.volume = musicVolume; }

    public void OnEventPopup(ResolvedEvent ev)
    {
        if (ev == null) return;

        if (ev.moneyChange > 0f)
        {
            PlaySFX(eventPositive);
            return;
        }

        float loss = Mathf.Abs(ev.moneyChange);

        if (loss < 200f) PlaySFX(eventMinor);
        else if (loss < 600f) PlaySFX(eventModerate);
        else PlaySFX(eventMajor);
    }

    public void OnEventBySeverity(EventSeverity severity, bool isPositive)
    {
        if (isPositive) { PlaySFX(eventPositive); return; }

        switch (severity)
        {
            case EventSeverity.Minor: PlaySFX(eventMinor); break;
            case EventSeverity.Moderate: PlaySFX(eventModerate); break;
            case EventSeverity.Major: PlaySFX(eventMajor); break;
        }
    }

    public void UpdateStressAudio(float moneyRatio)
    {
        float stress = 1f - moneyRatio;

        _ambienceSourceB.volume = Mathf.Lerp(0.3f, 0.05f, stress);
        _ambienceSourceC.volume = Mathf.Lerp(0.05f, 0.25f, stress);
        _ambienceSourceA.volume = Mathf.Lerp(0.4f, 0.3f, stress);

        _musicSource.volume = Mathf.Lerp(0.3f, 0.45f, stress);
        _musicLowPass.cutoffFrequency = Mathf.Lerp(22000f, 1200f, stress);
        _musicSource.pitch = Mathf.Lerp(1f, 0.85f, stress);
    }

    public void OnMentorMessage() => PlaySFX(mentorMessage);
    public void OnLoanMessage() => PlaySFX(loanMessage);
    public void OnLoanConfirm() => PlaySFX(loanConfirm);

    public void OnInsuranceToggle(bool isOn)
        => PlaySFX(isOn ? insuranceToggleOn : insuranceToggleOff);

    public void OnInsuranceConfirm() => PlaySFX(insuranceConfirm);
    public void OnPanelOpen() => PlaySFX(panelOpen);
    public void OnButtonClick() => PlaySFX(buttonClick);
    public void OnReportAppear() => PlaySFX(reportAppear);
    public void OnForecastCard() => PlaySFX(forecastCard, 0.6f);
    public void OnMoneyGain() => PlaySFX(moneyGain);
    public void OnMoneyLoss() => PlaySFX(moneyLoss);
    public void OnSavingsDeposit() => PlaySFX(savingsDeposit);
    public void OnYearComplete() => PlaySFX(yearComplete);
    public void OnMonthEnd() => PlaySFX(monthEnd, 0.7f);
    public void OnYearTransition() => PlaySFX(yearTransition, 0.8f);
    public void OnGameEnd() => PlaySFX(gameEnd, 1f);
}