using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InsurancePanel : MonoBehaviour
{
    [Header("UI References")]
    public Toggle funeralToggle;
    public Toggle educationToggle;
    public Toggle groceryToggle;
    public Toggle hospitalToggle;
    public Toggle microMedicalToggle;

    [Header("Info Display")]
    public TMP_Text planInfoText;
    public TMP_Text summaryText;
    public Button confirmButton;

    private InsuranceManager insuranceManager;

    private void OnEnable()
    {
        insuranceManager = FindFirstObjectByType<InsuranceManager>();

        if (insuranceManager == null)
        {
            Debug.LogError("InsuranceManager not found in scene!");
            return;
        }

        // Clear old listeners
        funeralToggle.onValueChanged.RemoveAllListeners();
        educationToggle.onValueChanged.RemoveAllListeners();
        groceryToggle.onValueChanged.RemoveAllListeners();
        hospitalToggle.onValueChanged.RemoveAllListeners();
        microMedicalToggle.onValueChanged.RemoveAllListeners();
        confirmButton.onClick.RemoveAllListeners();

        // Rebind listeners
        funeralToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Funeral));
        educationToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Education));
        groceryToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Grocery));
        hospitalToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Hospital));
        microMedicalToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.MicroMedical));

        confirmButton.onClick.AddListener(ConfirmInsurance);
        confirmButton.gameObject.SetActive(true);

        UpdateSummary();

        planInfoText.text = "Select one or more insurance plans to see their details.";
    }


    private void OnPlanToggled(bool isOn, InsuranceManager.InsuranceType type)
    {
        var plan = insuranceManager.allPlans.Find(p => p.type == type);
        if (plan == null) return;

        if (isOn)
        {
            insuranceManager.BuyInsurance(type);
            planInfoText.text = $"<b>{plan.planName}</b>\n\n" +
                                $"{plan.coverageDescription}\n\n" +
                                $"Premium: ${plan.premium:F2}\nCoverage: ${plan.coverageLimit:F0}\nDeductible: {plan.deductiblePercent}%";
        }
        else
        {
            insuranceManager.CancelInsurance(type);
            planInfoText.text = $"Removed {plan.planName} from your active plans.";
        }

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        float totalPremium = insuranceManager.GetTotalMonthlyPremium();

        summaryText.text = totalPremium > 0f
            ? $"Total Monthly Premium: ${totalPremium:F2}"
            : "No insurance selected. You proceed at your own risk.";
    }

    public void ConfirmInsurance()
    {
        Debug.Log($"[Insurance] Confirm clicked. Phase = {GameManager.Instance.CurrentPhase}");

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Insurance)
            return;

        GameManager.Instance.BeginMonthlySimulation();
    }
}
