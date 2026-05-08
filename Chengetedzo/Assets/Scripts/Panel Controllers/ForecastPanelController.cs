using UnityEngine;
using UnityEngine.UI;

public class ForecastPanelController : MonoBehaviour
{
    [Header("Navigation")]
    public Button continueButton;
    public Button backButton;

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(BackToBudget);

        if (continueButton == null)
        {
            Debug.LogError("[ForecastPanelController] Continue button not assigned.");
            return;
        }

        continueButton.onClick.AddListener(ContinueToInsurance);
    }

    private void OnEnable()
    {
        if (backButton == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[ForecastPanelController] GameManager not ready yet.");
            return;
        }

        bool isFirstMonth = GameManager.Instance.currentMonth == 1;
        backButton.gameObject.SetActive(
            isFirstMonth && !GameManager.Instance.IsForecastBackLocked
        );
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToInsurance);
    }

    private void ContinueToInsurance()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Forecast)
            return;

        GameManager.Instance.OnForecastConfirmed();
    }

    private void BackToBudget()
    {
        if (GameManager.Instance.IsForecastBackLocked)
            return;

        if (GameManager.Instance.currentMonth != 1)
            return;

        GameManager.Instance.OnForecastBack();
    }
}