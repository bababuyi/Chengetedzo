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
    private FinanceManager finance;

    private PlayerAssets Assets =>
    GameManager.Instance.financeManager.assets;

    private bool PlayerOwnsCar()
    {
        return Assets.hasMotor;
    }

    private bool PlayerOwnsHouse()
    {
        return Assets.hasHouse;
    }

    private bool PlayerOwnsFarm()
    {
        return Assets.hasCrops;
    }

    public enum InsuranceType
    {
        Funeral,
        Health,
        Education,
        HospitalCash,
        PersonalAccident,
        Motor,
        Home,
        Crop
    }

    public enum AssetRequirement
    {
        None,
        Car,
        House,
        Farm
    }



    public bool PaidPremiumsThisMonth { get; private set; }

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

        public float premiumRate = 0f; // % of asset value per month
        public bool premiumIsAssetBased = false;

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

        public AssetRequirement requiredAsset = AssetRequirement.None;

    }

    [Header("Available Insurance Plans")]
    public List<InsurancePlan> allPlans = new List<InsurancePlan>();

    // Bookkeeping for analytics
    private float totalLoss;
    private float totalPayout;

    public static InsuranceManager Instance;

    private void Start()
    {
        if (finance == null)
            finance = GameManager.Instance.financeManager;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //finance = GameManager.Instance?.financeManager;

        //if (finance == null)
        {
        //    Debug.LogWarning("[InsuranceManager] FinanceManager not ready yet.");
        }

        if (allPlans == null || allPlans.Count == 0)
            CreateDefaultPlans();
    }

    private void CreateDefaultPlans()
    {
        allPlans = new List<InsurancePlan>();

        allPlans.Add(new InsurancePlan
        {
            planName = "Funeral Cover",
            type = InsuranceType.Funeral,
            premium = 1f,
            coverageLimit = 1000f,
            waitingPeriodMonths = 3,
            coverageDescription = "Provides funeral expense cover for the family in the event of death."
        });


        allPlans.Add(new InsurancePlan
        {
            planName = "Health Insurance",
            type = InsuranceType.Health,
            premium = 7f,
            coverageLimit = 10000f, // per year (logic later)
            waitingPeriodMonths = 3,
            coverageDescription = "Covers medical expenses due to illness or hospitalization up to an annual limit."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Education Rider",
            type = InsuranceType.Education,
            premium = 1f, // PER CHILD (logic later)
            coverageLimit = 1000f, // per year
            waitingPeriodMonths = 3,
            coverageDescription = "Pays for children's education in the event of death of a parent, up to tertiary level."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Hospital Cash Back",
            type = InsuranceType.HospitalCash,
            premium = 1f,
            coverageLimit = 3000f, // 100 × 30 days
            waitingPeriodMonths = 3,
            coverageDescription = "Provides daily cash support during hospitalization, up to 30 days per month."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Personal Accident Cover",
            type = InsuranceType.PersonalAccident,
            premium = 1f,
            coverageLimit = 10000f,
            deductiblePercent = 0f,
            waitingPeriodMonths = 3,
            coverageDescription = "Pays a lump sum in the event of accidental death of the breadwinner."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Motor Insurance",
            type = InsuranceType.Motor,
            premium = 26.25f,
            coverageLimit = 3000f,
            waitingPeriodMonths = 0,
            requiredAsset = AssetRequirement.Car,
            coverageDescription = "Covers damage caused to third-party vehicles in a motor accident."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Home Insurance",
            type = InsuranceType.Home,
            premiumIsAssetBased = true,
            premiumRate = 0.015f, // 1.5%
            deductiblePercent = 0f,
            waitingPeriodMonths = 0,
            requiredAsset = AssetRequirement.House,
            coverageDescription = "In the event of an incident, covers damage up to the value of the house."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Crop Insurance",
            type = InsuranceType.Crop,
            premiumIsAssetBased = true,
            premiumRate = 0.005f, // 0.5%
            deductiblePercent = 0f,
            waitingPeriodMonths = 0,
            requiredAsset = AssetRequirement.Farm,
            coverageDescription = "In the event of crop loss, covers the cost of inputs."
        });
    }

    // ------------------------------
    // Helper accessors
    // ------------------------------

    public InsurancePlan GetPlan(InsuranceType t)
    {
        return allPlans.Find(p => p.type == t);
    }
    public bool PlayerMeetsRequirement(InsurancePlan plan)
    {
        if (plan == null)
            return false;

        switch (plan.requiredAsset)
        {
            case AssetRequirement.None:
                return true;

            case AssetRequirement.Car:
                return PlayerOwnsCar();

            case AssetRequirement.House:
                return PlayerOwnsHouse();

            case AssetRequirement.Farm:
                return PlayerOwnsFarm();

            default:
                return true;
        }
    }

    /// <summary>
    /// Calculate monthly premium for a plan taking dependents into account:
    /// - adults pay full premium
    /// - children pay 50% premium
    /// Uses PlayerDataManager.Instance.adults / children.
    /// </summary>
    public float CalculateMonthlyPremiumForPlan(InsurancePlan plan)
    {
        if (plan == null) return 0f;

        // Asset-based premium (Home / Crop)
        if (plan.premiumIsAssetBased)
        {
            float assetValue = finance.GetAssetValue(plan.type);
            return assetValue * plan.premiumRate;
        }

        // Per-person premium (existing logic)
        int adults = PlayerDataManager.Instance?.adults ?? 0;
        int children = PlayerDataManager.Instance?.children ?? 0;

        float adultCost = adults * plan.premium;
        float childCost = children * plan.premium * 0.5f;

        return adultCost + childCost;
    }

    public float CalculateMonthlyPremiumForUI(InsurancePlan plan)
    {
        return CalculateMonthlyPremiumForPlan(plan);
    }

    public void BuyInsurance(InsuranceType type)
    {
        var plan = GetPlan(type);
        if (plan == null)
        {
            Debug.LogWarning($"[Insurance] No plan found for {type}");
            return;
        }

        if (!PlayerMeetsRequirement(plan))
        {
            Debug.LogWarning($"[Insurance] Cannot buy {plan.planName}: asset requirement not met.");
            return;
        }

        if (plan.isSubscribed && !plan.isLapsed)
        {
            Debug.Log($"[Insurance] {plan.planName} already subscribed.");
            return;
        }

        // If lapsed, require re-subscribe
        if (plan.isLapsed)
        {
            plan.isLapsed = false;
            plan.missedPayments = 0;
            plan.monthsPaid = 0;
        }

        float firstPremium = CalculateMonthlyPremiumForPlan(plan);
        if (finance.cashOnHand >= firstPremium)
        {
            finance.cashOnHand -= firstPremium;
            plan.isSubscribed = true;
            plan.monthsPaid = 1;
            plan.missedPayments = 0;
            plan.isLapsed = false;

            Debug.Log($"[Insurance] Subscribed to {plan.planName}. Charged first premium ${firstPremium:F2}");
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
        finance.cashOnHand += refund;

        // Reset tracking
        plan.isSubscribed = false;
        plan.isLapsed = false;
        plan.monthsPaid = 0;
        plan.missedPayments = 0;

        Debug.Log($"[Insurance] Canceled {plan.planName}. Refunded ${refund:F2}");
    }

    // ------------------------------
    // Monthly processing (call from GameManager)
    // ------------------------------

    /// <summary>
    /// Called once per month by GameManager to deduct premiums and update waiting / grace / lapse state.
    /// </summary>
    public void ProcessMonthlyPremiums()
    {

        PaidPremiumsThisMonth = false;

        float totalCharged = 0f;

        foreach (var plan in allPlans)
        {
            if (!plan.isSubscribed || plan.isLapsed)
                continue;

            float premium = CalculateMonthlyPremiumForPlan(plan);

            if (finance.cashOnHand >= premium)
            {
                finance.cashOnHand -= premium;
                totalCharged += premium;

                plan.missedPayments = 0;
                plan.monthsPaid++;

                PaidPremiumsThisMonth = true;
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
        float baseValue = finance.GetAssetValue(type);
        float rawLoss = baseValue * (lossPercent / 100f);

        float cappedLoss = GameManager.Instance.ApplyMonthlyDamage(rawLoss);

        if (cappedLoss <= 0f)
        {
            Debug.Log("[Insurance] Monthly damage cap reached. No further loss applied.");
            return 0f;
        }

        finance.cashOnHand -= cappedLoss;
        totalLoss += cappedLoss;

        var plan = GetPlan(type);
        if (plan == null || !plan.CanClaim())
        {
            Debug.Log($"[Insurance] No valid coverage for {type}.");
            return 0f;
        }

        float deductible = cappedLoss * (plan.deductiblePercent / 100f);

        float coverageLimit =
            plan.premiumIsAssetBased
                ? finance.GetAssetValue(type)
                : plan.coverageLimit;

        float payout = Mathf.Min(cappedLoss - deductible, coverageLimit);
        payout = Mathf.Max(0f, payout);

        finance.cashOnHand += payout;
        totalPayout += payout;

        Debug.Log($"[Insurance] {plan.planName} reduced loss. Paid ${payout:F2}.");

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
        float resilienceScore = (finance.cashOnHand + totalPayout) - totalLoss;
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

    public void RefreshEligibility()
    {
        foreach (var plan in allPlans)
        {
            bool eligible = PlayerMeetsRequirement(plan);

            // If player no longer meets requirements
            if (!eligible)
            {
                // If they had this insurance, force cancel it
                if (plan.isSubscribed)
                {
                    plan.isSubscribed = false;
                    plan.isLapsed = false;
                    plan.monthsPaid = 0;
                    plan.missedPayments = 0;

                    Debug.Log($"[Insurance] {plan.planName} canceled due to unmet asset requirement.");
                }
            }

            // NOTE:
            // If eligible again later, player must manually re-buy.
        }
        Debug.Log("[Insurance] Eligibility refreshed.");
    }
}
