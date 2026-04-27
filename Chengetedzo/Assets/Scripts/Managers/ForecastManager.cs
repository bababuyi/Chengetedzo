using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static ForecastLines;
using static GameManager;

public class ForecastManager : MonoBehaviour
{
    [System.Serializable]
    public class ForecastArticle
    {
        public ForecastCategory category;
        public GameManager.Season season;

        public ForecastSignal signal;
        public ForecastLines.ForecastIntensity intensity;

        public string headline;
        public string body;
    }

    public enum ForecastSignal
    {
        Neutral,
        Dry,
        Wet,
        Heat,
        Disease,
        EconomicStress,
        CrimeWave
    }

    private Color GetColorForCategory(ForecastCategory category) => category switch
    {
        ForecastCategory.Health => new Color(0.85f, 0.33f, 0.33f),
        ForecastCategory.Livestock => new Color(0.70f, 0.50f, 0.20f),
        ForecastCategory.Crops => new Color(0.40f, 0.70f, 0.30f),
        ForecastCategory.Economic => new Color(0.20f, 0.55f, 0.80f),
        ForecastCategory.Crime => new Color(0.40f, 0.40f, 0.40f),
        ForecastCategory.Weather => new Color(0.30f, 0.60f, 0.90f),
        _ => Color.white
    };

    ForecastSignal GetSignal(EventData ev)
    {
        string name = ev.eventName.ToLower();

        if (name.Contains("drought")) return ForecastSignal.Dry;
        if (name.Contains("flood")) return ForecastSignal.Wet;
        if (name.Contains("storm")) return ForecastSignal.Wet;
        if (name.Contains("heat")) return ForecastSignal.Heat;

        return ForecastSignal.Neutral;
    }

    public class ForecastState
    {
        public Dictionary<ForecastCategory, float> categoryRiskMultiplier
            = new Dictionary<ForecastCategory, float>();

        public Dictionary<ForecastSignal, float> signalRiskMultiplier
            = new Dictionary<ForecastSignal, float>();

        public float globalIncomeModifier = 0f;
    }

    [System.Serializable]
    public class ForecastCategoryIcon
    {
        public ForecastCategory category;
        public Sprite icon;
    }

    [Header("Category Icons")]
    public List<ForecastCategoryIcon> categoryIcons;

    [Header("UI References")]
    public GameObject forecastPanel;
    public Transform forecastListParent;
    public GameObject forecastItemPrefab;
    public TMP_Text forecastHeaderText;

    [SerializeField] private int articlesPerForecast = 3;
    private HashSet<string> usedHeadlines = new HashSet<string>();
    public ForecastState CurrentForecast { get; private set; }
    public enum ForecastCategory
    {
        Health,
        Livestock,
        Crops,
        Economic,
        Crime,
        Weather
    }

    private List<ForecastArticle> forecastLibrary;
    private List<ForecastArticle> selectedForecasts = new List<ForecastArticle>();

    private void Start()
    {
    }

    private void Awake()
    {
        forecastGeneratedThisMonth = false;
        forecastLibrary = new List<ForecastArticle>();

        AddCategoryArticles(ForecastCategory.Health, ForecastLines.Health);
        AddCategoryArticles(ForecastCategory.Livestock, ForecastLines.Livestock);
        AddCategoryArticles(ForecastCategory.Crops, ForecastLines.Crops);
        AddCategoryArticles(ForecastCategory.Economic, ForecastLines.Economic);
        AddCategoryArticles(ForecastCategory.Crime, ForecastLines.Crime);
        AddCategoryArticles(ForecastCategory.Weather, ForecastLines.Weather);
    }

    public bool forecastGeneratedThisMonth = false;

    public void GenerateForecast()
    {
        Debug.Log($"forecastGeneratedThisMonth = {forecastGeneratedThisMonth}");
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Forecast)
        {
            Debug.LogWarning("Forecast generation attempted outside Forecast phase.");
            return;
        }

        if (forecastGeneratedThisMonth)
            return;

        forecastGeneratedThisMonth = true;

        usedHeadlines.Clear();
        selectedForecasts.Clear();
        Season upcomingSeason =
        GameManager.Instance.GetSeasonForMonth(GameManager.Instance.currentMonth);
        var database = GameManager.Instance.eventManager.EventDatabase;

        if (database == null || database.events == null)
        {
            Debug.LogWarning("[Forecast] No EventDatabase assigned.");
            return;
        }

        var possibleEvents = database.events;

        List<EventData> seasonalEvents =
        possibleEvents.FindAll(e =>
        e.season == Season.Any || e.season == upcomingSeason
        );
        Dictionary<ForecastCategory, float> categoryRisk =
        new Dictionary<ForecastCategory, float>();

        foreach (var ev in seasonalEvents)
        {
            if (!categoryRisk.ContainsKey(ev.category))
                categoryRisk[ev.category] = 0f;

            float avgLoss = (ev.minLossPercent + ev.maxLossPercent) * 0.5f;

            // Convert probability to 0-1 scale
            float probabilityFactor = ev.probability / 100f;

            // Risk score
            float eventRisk = probabilityFactor * avgLoss;

            categoryRisk[ev.category] += eventRisk;
        }

        var playerAssets = GameManager.Instance.financeManager.assets;
        bool hasFarmAssets = playerAssets.hasCrops || playerAssets.hasLivestock;
        bool hasHouse = playerAssets.hasHouse;
        bool hasMotor = playerAssets.hasMotor;

        // Asset based forecasting
        List<ForecastCategory> riskyCategories = new List<ForecastCategory>();
        foreach (var key in categoryRisk.Keys)
        {
            if (key == ForecastCategory.Crops && !hasFarmAssets) continue;
            if (key == ForecastCategory.Livestock && !playerAssets.hasLivestock) continue;
            riskyCategories.Add(key);
        }

        while (selectedForecasts.Count < articlesPerForecast && riskyCategories.Count > 0)
        {
            riskyCategories.Sort((a, b) => categoryRisk[b].CompareTo(categoryRisk[a]));
            ForecastCategory category = riskyCategories[0];

            AddArticlesForCategory(category, 1, upcomingSeason);

            riskyCategories.RemoveAt(0);
        }

        CurrentForecast = new ForecastState();

        // Default signal multipliers
        CurrentForecast.signalRiskMultiplier[ForecastSignal.Neutral] = 1f;
        CurrentForecast.signalRiskMultiplier[ForecastSignal.Dry] = 1f;
        CurrentForecast.signalRiskMultiplier[ForecastSignal.Wet] = 1f;
        CurrentForecast.signalRiskMultiplier[ForecastSignal.Heat] = 1f;
        CurrentForecast.signalRiskMultiplier[ForecastSignal.Disease] = 1f;
        CurrentForecast.signalRiskMultiplier[ForecastSignal.EconomicStress] = 1f;
        CurrentForecast.signalRiskMultiplier[ForecastSignal.CrimeWave] = 1f;

        foreach (var entry in categoryRisk)
        {
            float normalizedRisk = Mathf.Clamp01(entry.Value / 100f);

            float multiplier = Mathf.Lerp(0.8f, 1.6f, normalizedRisk);

            CurrentForecast.categoryRiskMultiplier[entry.Key] = multiplier;
        }

        int safetyCounter = 0;
        while (selectedForecasts.Count < articlesPerForecast && safetyCounter < 30)
        {
            safetyCounter++;
            ForecastCategory randomCategory =
                (ForecastCategory)Random.Range(0,
                    System.Enum.GetValues(typeof(ForecastCategory)).Length);

            if (selectedForecasts.Exists(a => a.category == randomCategory))
                continue;

            // Skip asset-specific categories if player doesn't have those assets
            if (randomCategory == ForecastCategory.Crops && !hasFarmAssets) continue;
            if (randomCategory == ForecastCategory.Livestock && !playerAssets.hasLivestock) continue;

            AddArticlesForCategory(randomCategory, 1, upcomingSeason);
        }

        foreach (var article in selectedForecasts)
        {
            float intensityMultiplier = 1f;

            switch (article.intensity)
            {
                case ForecastLines.ForecastIntensity.Mild:
                    intensityMultiplier = 1.2f;
                    break;

                case ForecastLines.ForecastIntensity.Warning:
                    intensityMultiplier = 1.4f;
                    break;

                case ForecastLines.ForecastIntensity.Severe:
                    intensityMultiplier = 1.7f;
                    break;
            }

            CurrentForecast.signalRiskMultiplier[article.signal] *= intensityMultiplier;
        }

        Debug.Log($"[Forecast] Selected Count: {selectedForecasts.Count}");

        if (selectedForecasts.Count > articlesPerForecast)
        {
            selectedForecasts = selectedForecasts.GetRange(0, articlesPerForecast);
        }

        ShowForecast();
    }

    private void ShowForecast()
    {
        Debug.Log("[Forecast] ShowForecast CALLED");
        Debug.Log($"Parent: {forecastListParent}");
        Debug.Log($"Prefab: {forecastItemPrefab}");
        if (forecastPanel == null) return;

        forecastHeaderText.text = "Monthly News";

        foreach (Transform child in forecastListParent)
            Destroy(child.gameObject);

        foreach (var forecast in selectedForecasts)
        {
            GameObject item =
                Instantiate(forecastItemPrefab, forecastListParent);

            TMP_Text text =
                item.GetComponentInChildren<TMP_Text>();

            Debug.Log($"Spawning article: {forecast.headline}");

            if (text != null)
            {
                text.text =
                    $"<b>{forecast.headline}</b>\n" +
                    $"<size=80%>{forecast.body}</size>";
            }

            UnityEngine.UI.Image img =
    item.GetComponentInChildren<UnityEngine.UI.Image>();

            if (img != null)
            {
                Sprite icon = GetIconForCategory(forecast.category);
                if (icon != null)
                {
                    img.sprite = icon;
                    img.color = Color.white;
                }
                else
                {
                    img.sprite = null;
                    img.color = GetColorForCategory(forecast.category);
                }
            }
        }
    }

    private Sprite GetIconForCategory(ForecastCategory category)
    {
        foreach (var entry in categoryIcons)
        {
            if (entry.category == category)
                return entry.icon;
        }
        return null;
    }

    public void ContinueToInsuranceSelection()
    {
        if (forecastPanel != null)
            forecastPanel.SetActive(false);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowInsurancePanel();
    }

    private void AddCategoryArticles(
    ForecastCategory category,
    ForecastLine[] lines,
    GameManager.Season season = GameManager.Season.Any)
    {
        foreach (ForecastLine line in lines)
        {
            forecastLibrary.Add(new ForecastArticle
            {
                category = category,
                season = season,
                signal = line.signal,
                intensity = line.intensity,
                headline = line.headline,
                body = line.body
            });
        }
    }

    private void AddArticlesForCategory(ForecastCategory category,int count,Season currentSeason)
    {
        List<ForecastArticle> pool = forecastLibrary.FindAll(a =>
            a.category == category &&
            (a.season == Season.Any || a.season == currentSeason) &&
            !usedHeadlines.Contains(a.headline)
        );

        pool = pool.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < Mathf.Min(count, pool.Count); i++)
        {
            selectedForecasts.Add(pool[i]);
            usedHeadlines.Add(pool[i].headline);
        }
    }
}
