using UnityEngine;
using static GameManager;

public class SeasonVisualManager : MonoBehaviour
{
    public Sprite summerBackground;
    public Sprite winterBackground;
    public UnityEngine.UI.Image seasonImage;

    private void Start()
    {
        UpdateSeasonVisual();
    }

    public void UpdateSeasonVisual()
    {
        Season season = GameManager.Instance.GetCurrentSeason();

        switch (season)
        {
            case Season.Summer:
                seasonImage.sprite = summerBackground;
                break;

            case Season.Winter:
                seasonImage.sprite = winterBackground;
                break;
        }
    }
}
