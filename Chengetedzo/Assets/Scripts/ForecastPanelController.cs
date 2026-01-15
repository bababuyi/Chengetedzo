using UnityEngine;
using UnityEngine.UI;

public class ForecastPanelController : MonoBehaviour
{
    public Button continueButton;

    private void Start()
    {
        continueButton.onClick.AddListener(ContinueToInsurance);
    }

    private void ContinueToInsurance()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Forecast)
            return;

        UIManager.Instance.HideAllPanels();
        GameManager.Instance.SetPhase(GameManager.GamePhase.Insurance);
        UIManager.Instance.ShowInsurancePanel();
    }
}