using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ForecastManager : MonoBehaviour
{
    [System.Serializable]
    public class EventDefinition
    {
        public string eventName;
        public string description;

        public InsuranceManager.InsuranceType relatedInsurance;
        public InsuranceManager.AssetRequirement requiredAsset;

        [Range(0, 100)]
        public int probability;

        public int minLossPercent;
        public int maxLossPercent;

        public GameManager.Season season;
        public Sprite icon;
    }

    [Header("Forecast Data")]
    public List<EventDefinition> allPossibleEvents = new List<EventDefinition>();

    private List<EventDefinition> selectedForecasts = new List<EventDefinition>();

    [Header("UI References")]
    public GameObject forecastPanel;
    public Transform forecastListParent;
    public GameObject forecastItemPrefab;
    public TMP_Text forecastHeaderText;

    private void Start()
    {
        if (forecastPanel != null)
            forecastPanel.SetActive(false);
    }

    public void GenerateForecast()
    {
        Debug.Log("[Forecast] GenerateForecast called");

        selectedForecasts.Clear();

        GameManager.Season upcomingSeason =
            GameManager.Instance.GetSeasonForMonth(GameManager.Instance.currentMonth);

        // Filter events by season
        List<EventDefinition> seasonal =
            allPossibleEvents.FindAll(e => e.season == upcomingSeason);

        if (seasonal.Count == 0)
        {
            Debug.LogWarning($"[Forecast] No events defined for {upcomingSeason}.");
            return;
        }

        int numForecasts = Random.Range(3, Mathf.Min(6, seasonal.Count + 1));

        // Shuffle
        seasonal.Sort((a, b) => Random.Range(-1, 2));

        for (int i = 0; i < numForecasts; i++)
            selectedForecasts.Add(seasonal[i]);

        ShowForecast();
    }

    private void ShowForecast()
    {
        if (forecastPanel == null) return;

        forecastPanel.SetActive(true);
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
                    $"{forecast.eventName} ({forecast.probability}% chance)\n" +
                    $"<size=80%>{forecast.description}</size>";
            }

            UnityEngine.UI.Image img =
                item.GetComponentInChildren<UnityEngine.UI.Image>();

            if (img != null && forecast.icon != null)
                img.sprite = forecast.icon;
        }
    }

    public void ContinueToInsuranceSelection()
    {
        if (forecastPanel != null)
            forecastPanel.SetActive(false);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowInsurancePanel();
    }
}
