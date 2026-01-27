using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsuranceManager;

public class InsurancePanel : MonoBehaviour
{
    [Header("UI References")]
    public Transform toggleContainer;
    public InsuranceToggleItem togglePrefab;

    [Header("Info Display")]
    public TMP_Text planInfoText;
    public TMP_Text summaryText;
    public Button confirmButton;

    private InsuranceManager insuranceManager;
    private void Awake()
    {
        insuranceManager = GameManager.Instance.insuranceManager;

        if (insuranceManager == null)
        {
            Debug.LogError("InsuranceManager not found in scene!");
            return;
        }

        planInfoText.text = "Select one or more insurance plans to see their details.";
        confirmButton.onClick.AddListener(ConfirmInsurance);
        confirmButton.gameObject.SetActive(true);
    }

    public bool PlayerMeetsRequirement(InsuranceManager.InsurancePlan plan)
    {
        return insuranceManager != null &&
               plan != null &&
               insuranceManager.PlayerMeetsRequirement(plan);
    }

    public void OnPlanToggled(bool isOn, InsuranceManager.InsuranceType type)
    {
        var plan = insuranceManager.allPlans.Find(p => p.type == type);
        if (plan == null) return;

        if (isOn)
        {
            insuranceManager.BuyInsurance(type);

            float premium = insuranceManager.GetTotalMonthlyPremium();

            planInfoText.text =
                $"<b>{plan.planName}</b>\n\n" +
                $"{plan.coverageDescription}\n\n" +
                $"Coverage: ${plan.coverageLimit:F0}\n" +
                $"Deductible: {plan.deductiblePercent}%";
        }
        else
        {
            insuranceManager.CancelInsurance(type);
            planInfoText.text = $"Removed {plan.planName} from your active plans.";
        }
        RefreshUI();
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

    public void RefreshUI()
    {
        // Clear existing toggle items
        foreach (Transform child in toggleContainer)
            Destroy(child.gameObject);

        // Rebuild from current insurance state
        foreach (var plan in insuranceManager.allPlans)
        {
            var item = Instantiate(togglePrefab, toggleContainer);
            item.Init(plan, this, insuranceManager);
        }

        UpdateSummary();
    }
}
