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

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(ContinueToInsurance);
    }

    private void ContinueToInsurance()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Forecast)
            return;

        GameManager.Instance.SetPhase(GameManager.GamePhase.Insurance);
        UIManager.Instance.ShowInsurancePanel();
    }

    private void BackToBudget()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Forecast)
            return;

        GameManager.Instance.SetPhase(GameManager.GamePhase.Idle);
        UIManager.Instance.ShowBudgetPanel();
    }
}