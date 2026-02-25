using System.Collections.Generic;
using UnityEngine;
using static EventManager;

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
        None,
        Funeral,
        Health,
        Education,
        HospitalCash,
        PersonalAccident,
        Motor,
        Home,
        Crop
    }

    public bool AnyPremiumPaidThisMonth { get; private set; }

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

        public GameManager.AssetRequirement requiredAsset;
    }

    [Header("Available Insurance Plans")]
    public List<InsurancePlan> allPlans = new List<InsurancePlan>();

    // Bookkeeping for analytics
    private float totalLoss;
    private float totalPayout;

    public static InsuranceManager Instance;

    private FinanceManager Finance
    {
        get
        {
            if (GameManager.Instance == null)
                return null;

            return GameManager.Instance.financeManager;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (Finance == null)
            Debug.LogWarning("[InsuranceManager] FinanceManager not ready in Awake.");

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
            requiredAsset = GameManager.AssetRequirement.Motor,
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
            requiredAsset = GameManager.AssetRequirement.House,
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
            requiredAsset = GameManager.AssetRequirement.Crops,
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
            case GameManager.AssetRequirement.None: return true;
            case GameManager.AssetRequirement.Motor: return PlayerOwnsCar();
            case GameManager.AssetRequirement.House: return PlayerOwnsHouse();
            case GameManager.AssetRequirement.Crops: return PlayerOwnsFarm();
        }
        return false;
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
            if (Finance == null) return 0f;
            float assetValue = Finance.GetAssetValue(plan.type);

            return assetValue * plan.premiumRate;
        }

        // Per-person premium
        int totalAdults = Mathf.Max(1, PlayerDataManager.Instance?.Adults ?? 1);
        int totalChildren = Mathf.Max(0, PlayerDataManager.Instance?.Children ?? 0);

        // Main adult (always 1)
        float mainAdultCost = plan.premium;

        // Other adults
        int otherAdults = totalAdults - 1;
        float otherAdultCost = otherAdults * plan.premium;

        // Children pay 50%
        float childCost = totalChildren * plan.premium * 0.5f;

        return mainAdultCost + otherAdultCost + childCost;
    }

    public float CalculateMonthlyPremiumForUI(InsurancePlan plan)
    {
        return CalculateMonthlyPremiumForPlan(plan);
    }

    public bool BuyInsurance(InsuranceType type)
    {
        Debug.Log($"[Insurance] Cash: {Finance.CashOnHand}");
        if (Finance == null)
        {
            Debug.LogError("[Insurance] FinanceManager missing.");
            return false;
        }

        var plan = GetPlan(type);
        if (plan == null)
        {
            Debug.LogWarning($"[Insurance] No plan found for {type}");
            return false;
        }

        if (!PlayerMeetsRequirement(plan))
        {
            Debug.LogWarning($"[Insurance] Cannot buy {plan.planName}: asset requirement not met.");
            return false;
        }

        if (plan.isSubscribed && !plan.isLapsed)
        {
            Debug.Log($"[Insurance] {plan.planName} already subscribed.");
            return false;
        }

        if (plan.isLapsed)
        {
            plan.isLapsed = false;
            plan.missedPayments = 0;
            plan.monthsPaid = 0;
        }

        float firstPremium = CalculateMonthlyPremiumForPlan(plan);

        if (Finance.CashOnHand >= firstPremium)
        {
            GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.InsurancePremium,
            $"Insurance Premium - {plan.planName}",
            firstPremium,
            false
            );

            plan.isSubscribed = true;
            plan.monthsPaid = 1;
            plan.missedPayments = 0;
            plan.isLapsed = false;

            Debug.Log($"[Insurance] Subscribed to {plan.planName}. Charged ${firstPremium:F2}");
            return true;
        }
        Debug.Log("Insurance sees Finance ID: " + Finance.GetInstanceID());
        Debug.Log("Insurance sees Cash: " + Finance.CashOnHand);

        Debug.LogWarning($"[Insurance] Not enough money for {plan.planName}. Need ${firstPremium:F2}");
        return false;
    }

    /// Cancel a subscribed policy. Refunds only if canceled within the first paid month.
    public void CancelInsurance(InsuranceType type)
    {
        var plan = GetPlan(type);
        if (plan == null) return;
        if (!plan.isSubscribed) return;

        float refund = (plan.monthsPaid <= 1)
        ? CalculateMonthlyPremiumForPlan(plan)
        : 0f;

        GameManager.Instance.ApplyMoneyChange(
        FinancialEntry.EntryType.InsuranceRefund,
        $"Insurance Refund - {plan.planName}",
        refund,
        true
        );

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
        AnyPremiumPaidThisMonth = false;

        float totalCharged = 0f;

        foreach (var plan in allPlans)
        {
            if (!plan.isSubscribed || plan.isLapsed)
                continue;

            float premium = CalculateMonthlyPremiumForPlan(plan);

            if (Finance.CashOnHand >= premium)
            {
                GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.InsurancePremium,
                $"Insurance Premium - {plan.planName}",
                premium,
                false
                );
                totalCharged += premium;

                plan.missedPayments = 0;
                plan.monthsPaid++;

                AnyPremiumPaidThisMonth = true;
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
    public float HandleEvent(InsuranceType type, float lossPercent, MonthlyEvent.LossCalculationType lossType, float fixedAmount = 0f)
    {
        var plan = GetPlan(type);
        float rawLoss = 0f;

        switch (lossType)
        {
            case MonthlyEvent.LossCalculationType.AssetValue:
                float assetValue = Finance.GetAssetValue(type);
                rawLoss = assetValue * (lossPercent / 100f);
                break;

            case MonthlyEvent.LossCalculationType.CashOnHand:
                rawLoss = Finance.CashOnHand * (lossPercent / 100f);
                break;

            case MonthlyEvent.LossCalculationType.FixedAmount:
                rawLoss = fixedAmount;
                break;
        }

        float payout = 0f;

        // 1? Insurance reduction FIRST
        if (plan != null && plan.CanClaim())
        {
            float deductible = rawLoss * (plan.deductiblePercent / 100f);
            float insurableLoss = Mathf.Max(0f, rawLoss - deductible);

            float coverageCap;

            if (plan.premiumIsAssetBased)
            {
                float assetValue = Finance.GetAssetValue(type);
                coverageCap = assetValue;
            }
            else
            {
                coverageCap = plan.coverageLimit;
            }

            payout = Mathf.Min(insurableLoss, coverageCap);
            totalPayout += payout;
        }

        // 2? Apply remaining loss to player (ApplyMonthlyDamage handles cash impact)
        float netLoss = Mathf.Max(0f, rawLoss - payout);
        float cappedLoss = GameManager.Instance.ApplyMonthlyDamage(netLoss);

        if (cappedLoss > 0f)
        {
            GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.EventLoss,
                $"Event Loss - {type}",
                cappedLoss,
                false
            );
        }

        totalLoss += cappedLoss;

        Debug.Log($"[Insurance] Event {type}: Loss ${rawLoss:F2}, Covered ${payout:F2}, Player Paid ${cappedLoss:F2}");

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
        float resilienceScore = (Finance.CashOnHand + totalPayout) - totalLoss;
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
