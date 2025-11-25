using UnityEngine;
using static GameManager;

public class VisualSimulationManager : MonoBehaviour
{
    [Header("Season Icons")]
    public GameObject sunIcon;
    public GameObject winterIcon;

    [Header("Environment Layers")]
    public GameObject heatHaze;
    public GameObject fogLayer;
    public GameObject dryGrass;

    [Header("Clouds")]
    public GameObject whiteClouds;
    public GameObject grayClouds;

    [Header("Wind Lines")]
    public GameObject windLines;

    private void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        Season currentSeason = GameManager.Instance.GetCurrentSeason();

        if (currentSeason == Season.Summer)
            ApplySummer();
        else
            ApplyWinter();
    }

    private void ApplySummer()
    {
        sunIcon.SetActive(true);
        winterIcon.SetActive(false);

        heatHaze.SetActive(true);
        fogLayer.SetActive(false);

        whiteClouds.SetActive(true);
        grayClouds.SetActive(false);

        dryGrass.SetActive(true);
        windLines.SetActive(true);

        Debug.Log("Visuals updated ? SUMMER");
    }

    private void ApplyWinter()
    {
        sunIcon.SetActive(false);
        winterIcon.SetActive(true);

        heatHaze.SetActive(false);
        fogLayer.SetActive(true);

        whiteClouds.SetActive(false);
        grayClouds.SetActive(true);

        dryGrass.SetActive(false);
        windLines.SetActive(false);

        Debug.Log("Visuals updated ? WINTER");
    }
}
