using System.Collections.Generic;
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

    private void Start()
    {
        RefreshUI();
    }

    private void Awake()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[InsurancePanel] GameManager not initialized.");
            enabled = false;
            return;
        }

        insuranceManager = GameManager.Instance.insuranceManager;

        if (insuranceManager == null)
        {
            Debug.LogError("[InsurancePanel] InsuranceManager not found.");
            enabled = false;
            return;
        }

        if (planInfoText != null)
            planInfoText.text = "Select one or more insurance plans to see their details.";

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmInsurance);
            confirmButton.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmInsurance);
    }

    public bool PlayerMeetsRequirement(InsuranceManager.InsurancePlan plan)
    {
        return insuranceManager != null &&
               plan != null &&
               insuranceManager.PlayerMeetsRequirement(plan);
    }

    public void OnPlanToggled(bool isOn, InsuranceType type)
    {
        if (insuranceManager == null)
            return;

        var plan = insuranceManager.allPlans.Find(p => p.type == type);
        if (plan == null)
            return;

        if (isOn)
        {
            bool success = insuranceManager.BuyInsurance(type);

            if (!success)
            {
                RefreshUI(); // revert toggle state
                planInfoText.text = "Not enough funds to purchase this plan.";
                return;
            }

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
        Debug.Log("Current Phase: " + GameManager.Instance.CurrentPhase);

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Insurance)
            return;

        GameManager.Instance.OnInsuranceConfirmed();
    }

    public void RefreshUI()
    {
        if (toggleContainer == null || togglePrefab == null || insuranceManager == null)
            return;

        foreach (Transform child in toggleContainer)
            Destroy(child.gameObject);

        foreach (var plan in insuranceManager.allPlans)
        {
            var item = Instantiate(togglePrefab, toggleContainer);

            bool meetsRequirement = PlayerMeetsRequirement(plan);

            item.Init(plan, this, insuranceManager);
            item.SetInteractable(meetsRequirement);
        }

        UpdateSummary();
    }

}
