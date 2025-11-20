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

    private void Start()
    {
        insuranceManager = FindFirstObjectByType<InsuranceManager>();

        if (insuranceManager == null)
        {
            Debug.LogError("InsuranceManager not found in scene!");
            return;
        }

        // Hook up toggle listeners
        funeralToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Funeral));
        educationToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Education));
        groceryToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Grocery));
        hospitalToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.Hospital));
        microMedicalToggle.onValueChanged.AddListener(isOn => OnPlanToggled(isOn, InsuranceManager.InsuranceType.MicroMedical));

        confirmButton.onClick.AddListener(ConfirmSelection);
        confirmButton.gameObject.SetActive(false);
        planInfoText.text = "Select one or more insurance plans to see their details.";
        summaryText.text = "";
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
        float totalPremium = 0f;

        foreach (var plan in insuranceManager.allPlans)
        {
            if (plan.isActive)
                totalPremium += plan.premium;
        }

        if (totalPremium <= 0f)
        {
            summaryText.text = "No insurance plans selected.";
            confirmButton.gameObject.SetActive(false);
        }
        else
        {
            summaryText.text = $"Total Monthly Premium: ${totalPremium:F2}";
            confirmButton.gameObject.SetActive(true);
        }
    }

    private void ConfirmSelection()
    {
        Debug.Log("[Insurance] Selection confirmed — proceeding to simulation...");
        gameObject.SetActive(false);
        GameManager.Instance.BeginSimulation();
    }
}
