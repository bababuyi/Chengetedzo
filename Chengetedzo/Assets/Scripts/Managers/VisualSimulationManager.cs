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
        if (GameManager.Instance == null) return;

        Season currentSeason = GameManager.Instance.GetCurrentSeason();

        if (currentSeason == Season.Summer)
            ApplySummer();
        else
            ApplyWinter();
    }

    private void SafeSet(GameObject obj, bool state)
    {
        if (obj != null)
            obj.SetActive(state);
    }

    private void ApplySummer()
    {
        SafeSet(sunIcon, true);
        SafeSet(winterIcon, false);

        SafeSet(heatHaze, true);
        SafeSet(fogLayer, false);

        SafeSet(whiteClouds, true);
        SafeSet(grayClouds, false);

        SafeSet(dryGrass, true);
        SafeSet(windLines, true);

        Debug.Log("Visuals updated SUMMER");
    }

    private void ApplyWinter()
    {
        SafeSet(sunIcon, false);
        SafeSet(winterIcon, true);

        SafeSet(heatHaze, false);
        SafeSet(fogLayer, true);

        SafeSet(whiteClouds, false);
        SafeSet(grayClouds, true);

        SafeSet(dryGrass, false);
        SafeSet(windLines, false);

        Debug.Log("Visuals updated WINTER");
    }

    private void SetActiveSafe(GameObject obj, bool state)
    {
        if (obj != null)
            obj.SetActive(state);
    }
}
