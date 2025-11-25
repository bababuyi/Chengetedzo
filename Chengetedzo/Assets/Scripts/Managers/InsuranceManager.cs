using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Rebuilt InsuranceManager with:
/// - Waiting periods (per-plan)
/// - Grace-month lapse rule (1 missed payment => grace, 2 missed => lapse)
/// - Monthly premium processing
/// - Claim blocking when in waiting period or lapsed
/// - Basic "buy" and "cancel" behavior (first premium charged on buy)
/// - Per-dependent premium calculation (adult = full, child = 50%)
/// </summary>
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

        // Base monthly premium PER PERSON (we will multiply by dependents)
        public float premium = 1f;

        // Max payout for a claim
        public float coverageLimit = 100f;

        // percent deductible applied to estimated loss
        public float deductiblePercent = 0f;

        // description for UI
        public string coverageDescription;

        // Waiting period (months) before claims allowed
        public int waitingPeriodMonths = 0;

        // ===== Runtime tracking =====
        public bool isSubscribed = false;   // player has this policy active/subscribed
        public bool isLapsed = false;       // policy lapsed due to missed payments
        public int monthsPaid = 0;          // how many premiums successfully paid
        public int missedPayments = 0;      // consecutive missed payments
        public bool inGrace => missedPayments == 1; // first missed payment => grace

        // Helper: whether policy currently allows claims
        public bool CanClaim()
        {
            return isSubscribed && !isLapsed && monthsPaid >= waitingPeriodMonths;
        }

        // String for UI state
        public string GetStatusString()
        {
            if (isLapsed) return "Lapsed";
            if (!isSubscribed) return "Not Subscribed";
            if (!CanClaim()) return $"Active (Waiting: {monthsPaid}/{waitingPeriodMonths})";
            return "Active";
        }
    }

    [Header("Player Finances")]
    public float playerMoney = 2000f;

    [Header("Available Insurance Plans")]
    public List<InsurancePlan> allPlans = new List<InsurancePlan>();

    // Bookkeeping for analytics
    private float totalLoss;
    private float totalPayout;

    private void Awake()
    {
        // Default plans if none provided in inspector
        if (allPlans == null || allPlans.Count == 0)
            CreateDefaultPlans();
    }

    private void CreateDefaultPlans()
    {
        allPlans = new List<InsurancePlan>();

        // Serious plans: waiting 0
        allPlans.Add(new InsurancePlan
        {
            planName = "Funeral Cover",
            type = InsuranceType.Funeral,
            premium = 3f,
            coverageLimit = 1000f,
            deductiblePercent = 0f,
            coverageDescription = "Covers funeral expenses for insured family members.",
            waitingPeriodMonths = 0
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Education Rider",
            type = InsuranceType.Education,
            premium = 2f,
            coverageLimit = 300f,
            deductiblePercent = 0f,
            coverageDescription = "Pays school fees after qualifying events.",
            waitingPeriodMonths = 0
        });

        // Non-serious: waiting 1
        allPlans.Add(new InsurancePlan
        {
            planName = "Grocery Support Plan",
            type = InsuranceType.Grocery,
            premium = 2f,
            coverageLimit = 300f,
            deductiblePercent = 0f,
            coverageDescription = "Provides grocery support after emergencies.",
            waitingPeriodMonths = 1
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Hospital Cash Plan",
            type = InsuranceType.Hospital,
            premium = 5f,
            coverageLimit = 700f,
            deductiblePercent = 0f,
            coverageDescription = "Covers hospitalization costs and medical expenses.",
            waitingPeriodMonths = 1
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "MicroMedical Assist",
            type = InsuranceType.MicroMedical,
            premium = 5f,
            coverageLimit = 500f,
            deductiblePercent = 10f,
            coverageDescription = "Provides outpatient and medicine support for minor illnesses.",
            waitingPeriodMonths = 1
        });
    }

    // ------------------------------
    // Helper accessors
    // ------------------------------

    private InsurancePlan GetPlan(InsuranceType t)
    {
        return allPlans.Find(p => p.type == t);
    }

    /// <summary>
    /// Calculate monthly premium for a plan taking dependents into account:
    /// - adults pay full premium
    /// - children pay 50% premium
    /// Uses PlayerDataManager.Instance.adults / children.
    /// </summary>
    private float CalculateMonthlyPremiumForPlan(InsurancePlan plan)
    {
        if (plan == null) return 0f;

        int adults = 0;
        int children = 0;
        if (PlayerDataManager.Instance != null)
        {
            adults = PlayerDataManager.Instance.adults;
            children = PlayerDataManager.Instance.children;
        }

        float adultCost = adults * plan.premium;
        float childCost = children * plan.premium * 0.5f;
        return adultCost + childCost;
    }

    // ------------------------------
    // Buying / Cancelling policies
    // ------------------------------

    /// <summary>
    /// Attempt to subscribe to a plan. Charges the first premium immediately if possible.
    /// If the player cannot pay the initial premium, subscription fails.
    /// </summary>
    public void BuyInsurance(InsuranceType type)
    {
        var plan = GetPlan(type);
        if (plan == null)
        {
            Debug.LogWarning($"[Insurance] No plan found for {type}");
            return;
        }

        if (plan.isSubscribed && !plan.isLapsed)
        {
            Debug.Log($"[Insurance] {plan.planName} already subscribed.");
            return;
        }

        // If lapsed, require re-subscribe (player must re-buy)
        if (plan.isLapsed)
        {
            // Reset tracking for a new subscription
            plan.isLapsed = false;
            plan.missedPayments = 0;
            plan.monthsPaid = 0;
        }

        float firstPremium = CalculateMonthlyPremiumForPlan(plan);
        if (playerMoney >= firstPremium)
        {
            playerMoney -= firstPremium;
            plan.isSubscribed = true;
            plan.monthsPaid = 1;
            plan.missedPayments = 0;
            plan.isLapsed = false;

            Debug.Log($"[Insurance] Subscribed to {plan.planName}. Charged first premium ${firstPremium:F2}");
            UIManager.Instance?.UpdateMoneyText(playerMoney);
        }
        else
        {
            Debug.LogWarning($"[Insurance] Not enough money to subscribe to {plan.planName}. Need ${firstPremium:F2}");
        }
    }

    /// <summary>
    /// Cancel a subscribed policy. Refunds one premium back to the player for UX convenience.
    /// </summary>
    public void CancelInsurance(InsuranceType type)
    {
        var plan = GetPlan(type);
        if (plan == null) return;
        if (!plan.isSubscribed) return;

        float refund = CalculateMonthlyPremiumForPlan(plan);
        playerMoney += refund;

        // Reset tracking
        plan.isSubscribed = false;
        plan.isLapsed = false;
        plan.monthsPaid = 0;
        plan.missedPayments = 0;

        Debug.Log($"[Insurance] Canceled {plan.planName}. Refunded ${refund:F2}");
        UIManager.Instance?.UpdateMoneyText(playerMoney);
    }

    // ------------------------------
    // Monthly processing (call from GameManager)
    // ------------------------------

    /// <summary>
    /// Called once per month by GameManager to deduct premiums and update waiting / grace / lapse state.
    /// </summary>
    public void ProcessMonthlyPremiums()
    {
        float totalCharged = 0f;

        foreach (var plan in allPlans)
        {
            if (!plan.isSubscribed || plan.isLapsed)
                continue;

            float premium = CalculateMonthlyPremiumForPlan(plan);

            if (playerMoney >= premium)
            {
                // pay premium
                playerMoney -= premium;
                totalCharged += premium;

                // successful payment resets missedPayments and increments monthsPaid
                plan.missedPayments = 0;
                plan.monthsPaid++;
            }
            else
            {
                // failed to pay
                plan.missedPayments++;

                if (plan.missedPayments == 1)
                {
                    // enters grace
                    Debug.Log($"[Insurance] {plan.planName} missed payment — grace month.");
                }
                else if (plan.missedPayments >= 2)
                {
                    // lapse
                    plan.isLapsed = true;
                    plan.isSubscribed = false;
                    Debug.Log($"[Insurance] {plan.planName} has lapsed due to consecutive missed premiums.");
                }
            }
        }

        if (totalCharged > 0f)
            Debug.Log($"[Insurance] Monthly premiums charged: ${totalCharged:F2}");

        UIManager.Instance?.UpdateMoneyText(playerMoney);
    }

    // ------------------------------
    // Handle events / claims
    // ------------------------------

    /// <summary>
    /// Called when an in-game event causes a loss of a given InsuranceType.
    /// Returns the payout amount (0 if no payout).
    /// This checks waiting period and lapse rules.
    /// </summary>
    public float HandleEvent(InsuranceType type, float lossPercent)
    {
        // estimatedLoss is a simple conversion here (you can change to tie to player assets)
        float estimatedLoss = 1000f * (lossPercent / 100f);
        totalLoss += estimatedLoss;

        var plan = GetPlan(type);
        if (plan == null)
        {
            Debug.LogWarning($"[Insurance] No plan for {type}");
            return 0f;
        }

        // Eligibility checks
        if (!plan.isSubscribed)
        {
            Debug.Log($"[Insurance] {plan.planName} - claim denied: not subscribed.");
            return 0f;
        }

        if (plan.isLapsed)
        {
            Debug.Log($"[Insurance] {plan.planName} - claim denied: policy lapsed.");
            return 0f;
        }

        if (plan.monthsPaid < plan.waitingPeriodMonths)
        {
            Debug.Log($"[Insurance] {plan.planName} - claim denied: waiting period ({plan.monthsPaid}/{plan.waitingPeriodMonths}).");
            return 0f;
        }

        // Passed checks -> calculate payout
        float deductible = estimatedLoss * (plan.deductiblePercent / 100f);
        float payout = Mathf.Min(estimatedLoss - deductible, plan.coverageLimit);
        payout = Mathf.Max(0f, payout);

        playerMoney += payout;
        totalPayout += payout;

        Debug.Log($"[Insurance] {plan.planName} covered loss of ${estimatedLoss:F2}, paid out ${payout:F2}.");
        UIManager.Instance?.UpdateMoneyText(playerMoney);

        return payout;
    }

    // ------------------------------
    // Misc / reporting
    // ------------------------------

    public void ProcessClaims()
    {
        // Placeholder for any monthly claim processing you want to run
        Debug.Log("[Insurance] ProcessClaims called (placeholder).");
    }

    public void CalculateSeasonResults()
    {
        float resilienceScore = (playerMoney + totalPayout) - totalLoss;
        Debug.Log($"[Insurance] Resilience Score: {resilienceScore}");
    }

    // Public helper for UI to show total monthly premium
    public float GetTotalMonthlyPremium()
    {
        float total = 0f;
        foreach (var plan in allPlans)
            if (plan.isSubscribed && !plan.isLapsed)
                total += CalculateMonthlyPremiumForPlan(plan);
        return total;
    }

    // Public helper to get a readable summary for UI
    public string GetPlansSummary()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var plan in allPlans)
        {
            sb.AppendLine($"{plan.planName}: {plan.GetStatusString()}");
        }
        return sb.ToString();
    }
}
