using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameManager;

public class ForecastManager : MonoBehaviour
{
    [System.Serializable]
    public class ForecastEvent
    {
        public string eventName;
        public string description;
        [Range(0, 100)] public int probability;
        public Sprite icon;
        public Season season;
    }

    [Header("Forecast Data")]
    public List<ForecastEvent> allPossibleEvents = new List<ForecastEvent>();

    [Header("UI References")]
    public GameObject forecastPanel;
    public Transform forecastListParent;
    public GameObject forecastItemPrefab;
    public TMP_Text forecastHeaderText;

    private List<ForecastEvent> selectedForecasts = new List<ForecastEvent>();

    private void Start()
    {
        // Hide forecast screen until needed
        if (forecastPanel != null)
            forecastPanel.SetActive(false);
    }

    public void GenerateForecast()
    {
        Debug.Log("[Forecast] GenerateForecast called");

        selectedForecasts.Clear();

        // Forecast season = first month of simulation
        Season upcomingSeason = GameManager.Instance.GetSeasonForMonth(GameManager.Instance.currentMonth);

        // Filter to correct season
        List<ForecastEvent> seasonal = allPossibleEvents.FindAll(e => e.season == upcomingSeason);

        if (seasonal.Count == 0)
        {
            Debug.LogWarning($"[Forecast] No events defined for {upcomingSeason}.");
            return;
        }

        // Random 3–5 items
        int numForecasts = Random.Range(3, Mathf.Min(6, seasonal.Count + 1));
        seasonal.Sort((a, b) => Random.Range(-1, 2));

        for (int i = 0; i < numForecasts; i++)
            selectedForecasts.Add(seasonal[i]);

        ShowForecast();
    }

    private void ShowForecast()
    {
        forecastPanel.SetActive(true);
        forecastHeaderText.text = "Seasonal Forecast";

        // Clear old forecast UI items
        foreach (Transform child in forecastListParent)
            Destroy(child.gameObject);

        // Spawn forecast entries dynamically
        foreach (var forecast in selectedForecasts)
        {
            GameObject item = Instantiate(forecastItemPrefab, forecastListParent);
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            text.text = $"{forecast.eventName} ({forecast.probability}% chance)\n<size=80%>{forecast.description}</size>";

            UnityEngine.UI.Image img = item.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img && forecast.icon)
                img.sprite = forecast.icon;
        }
    }

    public void ContinueToInsuranceSelection()
    {
        if (forecastPanel != null)
            forecastPanel.SetActive(false);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInsurancePanel();
            Debug.Log("[Forecast] Showing Insurance Panel via UIManager");
        }
    }
}
