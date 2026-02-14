using UnityEngine;
using static GameManager;

public class SeasonVisualManager : MonoBehaviour
{
    public Sprite summerBackground;
    public Sprite winterBackground;
    public UnityEngine.UI.Image seasonImage;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSeasonChanged += UpdateSeasonVisual;

        UpdateSeasonVisual();
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSeasonChanged -= UpdateSeasonVisual;
    }

    public void UpdateSeasonVisual()
    {
        if (GameManager.Instance == null || seasonImage == null)
            return;

        Season season = GameManager.Instance.GetCurrentSeason();

        switch (season)
        {
            case Season.Summer:
                seasonImage.sprite = summerBackground;
                break;

            case Season.Winter:
                seasonImage.sprite = winterBackground;
                break;

            default:
                seasonImage.sprite = summerBackground;
                break;
        }
    }
}
