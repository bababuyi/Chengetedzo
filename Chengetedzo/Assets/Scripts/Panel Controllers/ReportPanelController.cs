using UnityEngine;
using UnityEngine.UI;

public class ReportPanelController : MonoBehaviour
{
    public Button continueButton;

    private void OnEnable()
    {
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinue);
    }

    private void OnContinue()
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Report)
            return;

        gameObject.SetActive(false);
        GameManager.Instance.EndMonthAndAdvance();
    }
}
