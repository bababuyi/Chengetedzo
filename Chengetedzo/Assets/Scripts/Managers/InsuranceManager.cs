using UnityEngine;
using System.Collections.Generic;

public class InsuranceManager : MonoBehaviour
{
    public enum InsuranceType
    {
        Funeral,
        Education,
        Grocery,
        Hospital,
        MicroMedical
    }

    [System.Serializable]
    public class InsurancePlan
    {
        public string planName;
        public InsuranceType type;
        public float premium;
        public float coverageLimit;
        public float deductiblePercent;
        public string coverageDescription;
        public bool isActive;
    }

    [Header("Player Finances")]
    public float playerMoney = 2000f;

    [Header("Available Insurance Plans")]
    public List<InsurancePlan> allPlans = new List<InsurancePlan>();

    private float totalLoss;
    private float totalPayout;

    private void Awake()
    {
        if (allPlans.Count == 0)
        {
            allPlans.Add(new InsurancePlan
            {
                planName = "Funeral Cover",
                type = InsuranceType.Funeral,
                premium = 3f,
                coverageLimit = 1000f,
                deductiblePercent = 0,
                coverageDescription = "Covers funeral expenses for insured family members."
            });

            allPlans.Add(new InsurancePlan
            {
                planName = "Education Rider",
                type = InsuranceType.Education,
                premium = 2f,
                coverageLimit = 300f,
                deductiblePercent = 0,
                coverageDescription = "Pays school fees after qualifying events."
            });

            allPlans.Add(new InsurancePlan
            {
                planName = "Grocery Support Plan",
                type = InsuranceType.Grocery,
                premium = 2f,
                coverageLimit = 300f,
                deductiblePercent = 0,
                coverageDescription = "Provides grocery support after emergencies."
            });

            allPlans.Add(new InsurancePlan
            {
                planName = "Hospital Cash Plan",
                type = InsuranceType.Hospital,
                premium = 5f,
                coverageLimit = 700f,
                deductiblePercent = 0,
                coverageDescription = "Covers hospitalization costs and medical expenses."
            });

            allPlans.Add(new InsurancePlan
            {
                planName = "MicroMedical Assist",
                type = InsuranceType.MicroMedical,
                premium = 5f,
                coverageLimit = 500f,
                deductiblePercent = 10f,
                coverageDescription = "Provides outpatient and medicine support for minor illnesses."
            });
        }
    }

    public void BuyInsurance(InsuranceType type)
    {
        InsurancePlan plan = allPlans.Find(p => p.type == type);

        if (plan == null)
        {
            Debug.LogWarning($"[Insurance] No plan found for {type}!");
            return;
        }

        if (plan.isActive)
        {
            Debug.Log($"[Insurance] {plan.planName} is already active!");
            return;
        }

        if (playerMoney >= plan.premium)
        {
            playerMoney -= plan.premium;
            plan.isActive = true;
            Debug.Log($"[Insurance] Bought: {plan.planName}");
            UIManager.Instance?.UpdateMoneyText(playerMoney);
        }
        else
        {
            Debug.LogWarning("[Insurance] Not enough funds to buy this plan!");
        }
    }

    public void CancelInsurance(InsuranceType type)
    {
        InsurancePlan plan = allPlans.Find(p => p.type == type);
        if (plan != null && plan.isActive)
        {
            plan.isActive = false;
            Debug.Log($"[Insurance] Canceled: {plan.planName}");
        }
    }
    public float HandleEvent(InsuranceType type, float lossPercent)
    {
        // Convert % loss into a dollar value
        float estimatedLoss = 1000f * (lossPercent / 100f);
        totalLoss += estimatedLoss;

        // Check if player has active coverage of this type
        InsurancePlan plan = allPlans.Find(p => p.type == type && p.isActive);

        if (plan != null)
        {
            float deductible = estimatedLoss * (plan.deductiblePercent / 100f);
            float payout = Mathf.Min(estimatedLoss - deductible, plan.coverageLimit);

            // Add payout to player’s money
            playerMoney += payout;
            totalPayout += payout;

            Debug.Log($"[Insurance] {plan.planName} covered loss of ${estimatedLoss:F0}, paid out ${payout:F0}.");
            UIManager.Instance?.UpdateMoneyText(playerMoney);
            return payout;
        }

        Debug.Log($"[Insurance] No active policy to cover {type} event.");
        return 0f; // no payout
    }

    public void ProcessPremiums()
    {
        float totalPremium = 0f;
        foreach (var plan in allPlans)
        {
            if (plan.isActive)
                totalPremium += plan.premium;
        }

        playerMoney -= totalPremium;
        if (totalPremium > 0)
            Debug.Log($"[Insurance] Monthly premiums deducted: ${totalPremium}");

        UIManager.Instance?.UpdateMoneyText(playerMoney);
    }

    public void ProcessClaims()
    {
        Debug.Log("[Insurance] Processing claims (placeholder).");
    }

    public void CalculateSeasonResults()
    {
        float resilienceScore = (playerMoney + totalPayout) - totalLoss;
        Debug.Log($"[Insurance] Resilience Score: {resilienceScore}");
    }
}
