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
        UIManager.Instance.ShowInsurancePanel();
        gameObject.SetActive(false);
    }
}