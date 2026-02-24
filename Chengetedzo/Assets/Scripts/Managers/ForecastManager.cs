using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameManager;

public class ForecastManager : MonoBehaviour
{
    [System.Serializable]
    public class ForecastArticle
    {
        public ForecastCategory category;
        public GameManager.Season season;
        public string headline;
        public string body;
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
        forecastLibrary = new List<ForecastArticle>();

        AddCategoryArticles(ForecastCategory.Health, ForecastLines.Health);
        AddCategoryArticles(ForecastCategory.Livestock, ForecastLines.Livestock);
        AddCategoryArticles(ForecastCategory.Crops, ForecastLines.Crops);
        AddCategoryArticles(ForecastCategory.Economic, ForecastLines.Economic);
        AddCategoryArticles(ForecastCategory.Crime, ForecastLines.Crime);
        AddCategoryArticles(ForecastCategory.Weather, ForecastLines.Weather);
    }


    public void GenerateForecast()
    {
        usedHeadlines.Clear();
        selectedForecasts.Clear();
        Season upcomingSeason =
        GameManager.Instance.GetSeasonForMonth(GameManager.Instance.currentMonth);
        var possibleEvents = GameManager.Instance.eventManager.allEvents;
        List<EventManager.MonthlyEvent> seasonalEvents =
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
            categoryRisk[ev.category] += ev.probability * avgLoss;
        }
        foreach (var entry in categoryRisk)
        {
            AddArticlesForCategory(entry.Key, articlesPerForecast, upcomingSeason);
        }

        while (selectedForecasts.Count < articlesPerForecast)
        {
            ForecastCategory randomCategory =
                (ForecastCategory)Random.Range(0,
                    System.Enum.GetValues(typeof(ForecastCategory)).Length);

            AddArticlesForCategory(randomCategory, 1, upcomingSeason);
        }

        ShowForecast();
    }

    private void ShowForecast()
    {
        if (forecastPanel == null) return;

        forecastHeaderText.text = "Seasonal Forecast";

        foreach (Transform child in forecastListParent)
            Destroy(child.gameObject);

        foreach (var forecast in selectedForecasts)
        {
            GameObject item =
                Instantiate(forecastItemPrefab, forecastListParent);

            TMP_Text text =
                item.GetComponentInChildren<TMP_Text>();

            if (text != null)
            {
                text.text =
                    $"<b>{forecast.headline}</b>\n" +
                    $"<size=80%>{forecast.body}</size>";
            }

            UnityEngine.UI.Image img =
                item.GetComponentInChildren<UnityEngine.UI.Image>();

            if (img != null)
                img.sprite = GetIconForCategory(forecast.category);
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

        if (UIManager.Instance != null)
            UIManager.Instance.ShowInsurancePanel();
    }

    private void AddCategoryArticles(
    ForecastCategory category,
    string[] lines,
    GameManager.Season season = GameManager.Season.Any)
    {
        foreach (string line in lines)
        {
            string[] parts = line.Split('\n');

            if (parts.Length < 2)
                continue;

            forecastLibrary.Add(new ForecastArticle
            {
                category = category,
                season = season,
                headline = parts[0],
                body = parts[1]
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

        pool.Sort((a, b) => Random.Range(-1, 2));

        for (int i = 0; i < Mathf.Min(count, pool.Count); i++)
        {
            selectedForecasts.Add(pool[i]);
            usedHeadlines.Add(pool[i].headline);
        }
    }
    //private string[] GetLinesForCategory(ForecastCategory category)
    //{
    //  return category switch
    //{
    //  ForecastCategory.Health => ForecastLines.Health,
    //ForecastCategory.Livestock => ForecastLines.Livestock,
    //ForecastCategory.Crops => ForecastLines.Crops,
    //ForecastCategory.Economic => ForecastLines.Economic,
    //ForecastCategory.Crime => ForecastLines.Crime,
    //ForecastCategory.Weather => ForecastLines.Weather,
    //_ => ForecastLines.Economic
    //};
    //}
}
