using UnityEngine;
using UnityEngine.UI;

public class ReportPanelController : MonoBehaviour
{
    public Button continueButton;

    private void OnEnable()
    {
        if (continueButton == null)
        {
            Debug.LogError("[ReportPanel] Continue button not assigned.");
            enabled = false;
            return;
        }

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinue);
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinue);
    }

    private void OnContinue()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Report)
            return;

        // Prevent double-click spam
        if (continueButton != null)
            continueButton.interactable = false;

        gameObject.SetActive(false);

        GameManager.Instance.EndMonthAndAdvance();
    }
}
