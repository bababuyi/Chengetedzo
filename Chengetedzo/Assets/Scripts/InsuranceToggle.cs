using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static InsuranceManager;

public class InsuranceToggleItem : MonoBehaviour
{
    public Toggle toggle;
    public TMP_Text nameText;
    public TMP_Text premiumText;
    public TMP_Text statusText;
    public TMP_Text requirementText;

    private InsuranceManager.InsurancePlan plan;
    private InsurancePanel panel;
    private InsuranceManager insuranceManager;

    public void Init(
        InsuranceManager.InsurancePlan plan,
        InsurancePanel panel,
        InsuranceManager insuranceManager)
    {
        this.plan = plan;
        this.panel = panel;
        this.insuranceManager = insuranceManager;

        nameText.text = plan.planName;

        bool allowed = panel.PlayerMeetsRequirement(plan);
        toggle.interactable = allowed;

        requirementText.gameObject.SetActive(!allowed);
        requirementText.text = allowed
            ? ""
            : $"Requires {plan.requiredAsset}";

        toggle.isOn = plan.isSubscribed && !plan.isLapsed;

        UpdatePremium();
        UpdateStatus();

        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(isOn =>
        {
            panel.OnPlanToggled(isOn, plan.type);
            UpdatePremium();
            UpdateStatus();
        });
    }

    public void Refresh()
    {
        UpdatePremium();
        UpdateStatus();
        toggle.isOn = plan.isSubscribed && !plan.isLapsed;
    }

    private void UpdatePremium()
    {
        if (insuranceManager == null || plan == null)
        {
            premiumText.text = "$0.00 / month";
            return;
        }

        float planCost = insuranceManager.CalculateMonthlyPremiumForUI(plan);
        premiumText.text = $"${planCost:F2} / month";
    }

    private void UpdateStatus()
    {
        statusText.text = plan.GetStatusString();
    }
}