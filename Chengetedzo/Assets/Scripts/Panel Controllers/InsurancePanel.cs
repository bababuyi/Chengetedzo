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

    [Header("Navigation")]
    public Button backButton;

    private InsuranceManager insuranceManager;
    private bool onAssetPage = false;

    private void OnEnable()
    {
        onAssetPage = false;
        ShowPage();
    }

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(BackToForecast);

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
            confirmButton.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();
    }

    private void ShowPage()
    {
        if (toggleContainer == null || togglePrefab == null || insuranceManager == null)
            return;

        foreach (Transform child in toggleContainer)
            Destroy(child.gameObject);

        var plansForPage = insuranceManager.allPlans.FindAll(p =>
            onAssetPage
                ? p.requiredAsset != GameManager.AssetRequirement.None
                : p.requiredAsset == GameManager.AssetRequirement.None
        );

        if (onAssetPage && plansForPage.TrueForAll(p => !PlayerMeetsRequirement(p)))
        {
            ConfirmInsurance();
            return;
        }

        foreach (var plan in plansForPage)
        {
            var item = Instantiate(togglePrefab, toggleContainer);
            item.Init(plan, this, insuranceManager);
            item.SetInteractable(PlayerMeetsRequirement(plan));
        }

        if (planInfoText != null)
            planInfoText.text = onAssetPage
                ? "These plans cover your assets — vehicle, home, and farm."
                : "Select one or more insurance plans to see their details.";

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();

            if (!onAssetPage)
            {
                confirmButton.GetComponentInChildren<TMP_Text>().text = "Next →";
                confirmButton.onClick.AddListener(AdvanceToAssetPage);
            }
            else
            {
                confirmButton.GetComponentInChildren<TMP_Text>().text = "Confirm";
                confirmButton.onClick.AddListener(ConfirmInsurance);
            }
        }

        UpdateSummary();
    }

    private void AdvanceToAssetPage()
    {
        onAssetPage = true;
        ShowPage();
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
                ShowPage();
                planInfoText.text = "Not enough funds to purchase this plan.";
                return;
            }

            TutorialManager.Instance?.TriggerTutorial("insurance_deductible_explainer");

            planInfoText.text =
                $"<b>{plan.planName}</b>\n\n" +
                $"{plan.coverageDescription}\n\n" +
                $"Coverage: {GameUtils.FormatMoney(plan.coverageLimit)}\n";

            if (plan.deductiblePercent > 0f)
                planInfoText.text += $"Deductible: {plan.deductiblePercent}%";
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
            ? $"Total Monthly Premium: {GameUtils.FormatMoney(totalPremium)}"
            : "No insurance selected. You proceed at your own risk.";
    }

    public void ConfirmInsurance()
    {
        if (GameManager.Instance == null) return;

        if (UIManager.Instance.IsPopupActive)
            UIManager.Instance.ForceCloseAllPopups();

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Insurance)
        {
            Debug.LogWarning($"[InsurancePanel] ConfirmInsurance called in wrong phase: {GameManager.Instance.CurrentPhase}");
            return;
        }

        GameManager.Instance.OnInsuranceConfirmed();
    }

    public void RefreshUI()
    {
        ShowPage();
    }

    private void BackToForecast()
    {
        if (onAssetPage)
        {
            onAssetPage = false;
            ShowPage();
            return;
        }

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Insurance)
            return;

        GameManager.Instance.OnInsuranceBack();
    }
}