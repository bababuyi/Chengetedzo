using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ForecastManager : MonoBehaviour
{
    [System.Serializable]
    public class ForecastEvent
    {
        public string eventName;
        public string description;
        [Range(0, 100)] public int probability;
        public Sprite icon;
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
        forecastPanel.SetActive(false);
    }

    public void GenerateForecast()
    {
        selectedForecasts.Clear();

        // Randomly pick 3–5 likely events for this season
        int numForecasts = Random.Range(3, 6);
        List<ForecastEvent> shuffled = new List<ForecastEvent>(allPossibleEvents);
        shuffled.Sort((a, b) => Random.Range(-1, 2));

        for (int i = 0; i < numForecasts && i < shuffled.Count; i++)
            selectedForecasts.Add(shuffled[i]);

        ShowForecast();
    }

    private void ShowForecast()
    {
        forecastPanel.SetActive(true);
        forecastHeaderText.text = " Seasonal Forecast";

        // Clear old UI items
        foreach (Transform child in forecastListParent)
            Destroy(child.gameObject);

        // Spawn forecast entries
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
        forecastPanel.SetActive(false);
        var insuranceUI = FindFirstObjectByType<InsuranceSelectionUI>();
        if (insuranceUI != null)
        {
            insuranceUI.gameObject.SetActive(true);
            Debug.Log("Forecast complete — moving to insurance selection.");
        }
        else
        {
            Debug.LogWarning("InsuranceSelectionUI not found! Starting simulation instead.");
            GameManager.Instance.BeginSimulation();
        }
    }
}
