using UnityEngine;
using UnityEngine.UI;

public class ForecastPanelController : MonoBehaviour
{
    public Button continueButton;

    private void Start()
    {
        if (continueButton == null)
        {
            Debug.LogError("[ForecastPanelController] Continue button not assigned.");
            return;
        }

        continueButton.onClick.AddListener(ContinueToInsurance);
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToInsurance);
    }

    private void ContinueToInsurance()
    {
        if (GameManager.Instance == null || UIManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Forecast)
            return;

        // Prefer centralized phase control in GameManager
        GameManager.Instance.SetPhase(GameManager.GamePhase.Insurance);
    }
}