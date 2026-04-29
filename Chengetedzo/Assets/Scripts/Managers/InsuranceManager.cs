using System.Collections.Generic;
using UnityEngine;
using static EventManager;

public class InsuranceManager : MonoBehaviour
{
    private PlayerAssets Assets =>
    GameManager.Instance.financeManager.assets;

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

        public float premium = 1f;

        public float coverageLimit = 100f;

        public float deductiblePercent = 0f;

        public string coverageDescription;

        public int waitingPeriodMonths = 0;
        public int billingCycleMonths = 1;
        public int monthsInCycle = 0;

        // Trackers
        public bool isSubscribed = false;
        public bool isLapsed = false;
        public int monthsPaid = 0;
        public int missedPayments = 0;

        public bool inGrace => missedPayments == 1;

        public float premiumRate = 0f;
        public bool premiumIsAssetBased = false;

        public bool CanClaim()
        {
            return isSubscribed && !isLapsed && monthsPaid >= waitingPeriodMonths;
        }

        public string GetStatusString()
        {
            if (isLapsed) return "Lapsed";
            if (!isSubscribed) return "Not Subscribed";
            if (!CanClaim()) return $"Active (Waiting: {monthsPaid}/{waitingPeriodMonths})";
            return "Active";
        }

        public int coverageMonthsRemaining = 0;
        public bool canCancelThisMonth = false;

        public GameManager.AssetRequirement requiredAsset;
    }

    [Header("Available Insurance Plans")]
    public List<InsurancePlan> allPlans = new List<InsurancePlan>();

    private float totalLoss;
    private float totalPayout;

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
        if (Finance == null)
            Debug.LogWarning("[InsuranceManager] FinanceManager not ready in Awake.");

        if (allPlans == null || allPlans.Count == 0)
            CreateDefaultPlans();
    }

    public struct InsuranceResult
    {
        public float rawLoss;
        public float payout;
        public float deductibleAmount;
        public float finalLoss;
        public bool claimApproved;
        public bool waitingPeriodBlocked;
        public bool lapsedBlocked;
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
            planName = "Motor Insurance (3rd Party)",
            type = InsuranceType.Motor,
            premiumIsAssetBased = false,
            premium = 104f,             // charged every 4 months
            billingCycleMonths = 4,
            coverageLimit = 3000f,
            waitingPeriodMonths = 0,
            requiredAsset = GameManager.AssetRequirement.Motor,
            coverageDescription = "Covers liability for death, bodily injury, and property damage to others"
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Home Insurance",
            type = InsuranceType.Home,
            premiumIsAssetBased = true,
            premiumRate = 0.00075f,
            deductiblePercent = 0f,
            waitingPeriodMonths = 0,
            requiredAsset = GameManager.AssetRequirement.House,
            coverageDescription = "In the event of an incident, covers damage up to the value of the house."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Agricultural Insurance",
            type = InsuranceType.Crop,
            premiumIsAssetBased = false,
            premium = 5f,
            coverageLimit = 2000f,
            deductiblePercent = 0.05f,
            waitingPeriodMonths = 0,
            requiredAsset = GameManager.AssetRequirement.CropsOrLivestock,
            coverageDescription = "Covers financial losses from crop failure or livestock disease. " +
                          "Protects farmers and smallholders against the unexpected costs " +
                          "of agricultural setbacks."
        });
    }

    // Helper accessors
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
            case GameManager.AssetRequirement.None:
                return true;
            case GameManager.AssetRequirement.Motor:
                return Assets.hasMotor;
            case GameManager.AssetRequirement.House:
                return Assets.hasHouse;
            case GameManager.AssetRequirement.Crops:
            case GameManager.AssetRequirement.Livestock:
            case GameManager.AssetRequirement.CropsOrLivestock:
                return Assets.hasCrops || Assets.hasLivestock;
        }
        return false;
    }

    public float CalculateMonthlyPremiumForPlan(InsurancePlan plan)
    {
        if (plan == null) return 0f;

        // Education is a flat per-policy premium (covers the policyholder only)
        if (plan.type == InsuranceType.Education)
            return plan.premium;

        // Plans with billing cycles longer than 1 month are flat-rate, not per-person
        // (e.g. Motor at $104 every 4 months)
        if (plan.billingCycleMonths > 1)
            return plan.premium;

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

        // Children count at full rate not half off
        float childCost = totalChildren * plan.premium;

        return mainAdultCost + otherAdultCost + childCost;
    }

    public float CalculateMonthlyPremiumForUI(InsurancePlan plan)
    {
        return CalculateMonthlyPremiumForPlan(plan);
    }

    public bool BuyInsurance(InsuranceType type)
    {
        if (Finance == null)
        {
            Debug.LogError("[Insurance] FinanceManager missing.");
            return false;
        }

        Debug.Log($"[Insurance] Cash: {Finance.CashOnHand}");

        var plan = GetPlan(type);

        if (type == InsuranceType.Motor)
        {
            if (plan.coverageMonthsRemaining > 0)
            {
                Debug.Log("[Insurance] Motor insurance already active.");
                return false;
            }

            float cost = plan.premium;

            if (Finance.CashOnHand < cost)
            {
                Debug.LogWarning("[Insurance] Not enough money for motor insurance.");
                return false;
            }

            GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.InsurancePremium,
                $"Insurance Premium - {plan.planName}",
                cost,
                false
            );

            plan.isSubscribed = true;
            plan.isLapsed = false;
            plan.coverageMonthsRemaining = 4;
            plan.canCancelThisMonth = true;
            plan.monthsPaid = 1;

            Debug.Log("[Insurance] Motor insurance purchased for 4 months.");
            return true;
        }

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
            plan.monthsInCycle = 0;

            Debug.Log($"[Insurance] Subscribed to {plan.planName}. Charged ${firstPremium:F2}");
            return true;
        }
        Debug.Log("Insurance sees Finance ID: " + Finance.GetInstanceID());
        Debug.Log("Insurance sees Cash: " + Finance.CashOnHand);

        Debug.LogWarning($"[Insurance] Not enough money for {plan.planName}. Need ${firstPremium:F2}");
        return false;
    }

    // Cancel a subscribed policy. Refunds only if canceled within the first paid month.
    public void CancelInsurance(InsuranceType type)
    {
        var plan = GetPlan(type);
        if (plan == null) return;
        if (!plan.isSubscribed) return;

        if (type == InsuranceType.Motor && !plan.canCancelThisMonth)
        {
            Debug.Log("[Insurance] Motor insurance cannot be canceled after month 1.");
            return;
        }

        float refund = CalculateMonthlyPremiumForPlan(plan);

        GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.InsuranceRefund,
            $"Insurance Refund - {plan.planName}",
            refund,
            true
        );

        plan.isSubscribed = false;
        plan.isLapsed = false;
        plan.monthsPaid = 0;
        plan.coverageMonthsRemaining = 0;
    }

    // Monthly processing

    public void ProcessMonthlyPremiums()
    {
        AnyPremiumPaidThisMonth = false;

        float totalCharged = 0f;

        foreach (var plan in allPlans)
        {
            if (plan.type == InsuranceType.Motor)
            {
                if (plan.coverageMonthsRemaining > 0)
                {
                    plan.coverageMonthsRemaining--;

                    // After first month passes, no cancellation allowed
                    if (plan.coverageMonthsRemaining < 4)
                        plan.canCancelThisMonth = false;

                    if (plan.coverageMonthsRemaining <= 0)
                    {
                        plan.isSubscribed = false;
                        plan.isLapsed = false;

                        Debug.Log("[Insurance] Motor insurance expired.");
                    }
                }
                else if (Finance != null &&
                         GameManager.Instance.financeManager.assets.hasMotor &&
                         Random.value < 0.15f)
                {
                    GameManager.Instance.ApplyMoneyChange(
                        FinancialEntry.EntryType.EventLoss,
                        "Traffic Fine — No Motor Insurance",
                        10f,
                        false
                    );

                    Debug.Log("[Insurance] $10 fine for no motor insurance.");
                }

                continue;
            }

            if (!plan.isSubscribed || plan.isLapsed)
            {
                // Fine check for lapsed/unsubscribed motor insurance
                if (plan.type == InsuranceType.Motor &&
                    Finance != null &&
                    GameManager.Instance.financeManager.assets.hasMotor &&
                    UnityEngine.Random.value < 0.15f) // 15% chance each month of fine
                {
                    float fine = UnityEngine.Random.Range(50f, 150f);
                    GameManager.Instance.ApplyMoneyChange(
                        FinancialEntry.EntryType.EventLoss,
                        "Traffic Fine — No Motor Insurance",
                        fine,
                        false
                    );
                    Debug.Log($"[Insurance] Motor fine issued: ${fine:F0}");
                }
                continue;
            }

            // Quarterly billing cycle check
            plan.monthsInCycle++;
            bool isDueThisMonth = plan.monthsInCycle >= plan.billingCycleMonths;

            if (!isDueThisMonth)
            {
                // Policy remains active between billing months — no charge
                continue;
            }

            plan.monthsInCycle = 0; // reset cycle

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
                plan.missedPayments++;

                if (plan.missedPayments == 1)
                {
                    Debug.Log($"[Insurance] {plan.planName} missed payment — grace month.");
                }
                else if (plan.missedPayments >= 2)
                {
                    plan.isLapsed = true;
                    plan.isSubscribed = false;
                    Debug.Log($"[Insurance] {plan.planName} has lapsed due to consecutive missed premiums.");
                }
            }
        }

        if (totalCharged > 0f)
            Debug.Log($"[Insurance] Monthly premiums charged: ${totalCharged:F2}");
    }

    // Handle events / claims
    
    public InsuranceResult HandleEvent(
    InsuranceType type,
    float rawLoss)

    {
        InsuranceResult result = new InsuranceResult();

        var plan = GetPlan(type);

        /*switch (lossType)
        {
            case LossCalculationType.AssetValue:
                float assetValue = Finance.GetAssetValue(type);
                rawLoss = assetValue * (lossPercent / 100f);
                break;

            case LossCalculationType.CashOnHand:
                rawLoss = Finance.CashOnHand * (lossPercent / 100f);
                break;

            case LossCalculationType.FixedAmount:
                rawLoss = fixedAmount;
                break;
        }*/

        float payout = 0f;
        float deductibleAmount = 0f;

        bool waitingBlocked = false;
        bool lapsedBlocked = false;

        // 2 Insurance evaluation
        if (plan != null)
        {
            if (plan.isLapsed)
            {
                lapsedBlocked = true;
            }
            else if (plan.monthsPaid < plan.waitingPeriodMonths)
            {
                waitingBlocked = true;
            }
            else if (plan.CanClaim())
            {
                deductibleAmount = rawLoss * (plan.deductiblePercent / 100f);
                float insurableLoss = Mathf.Max(0f, rawLoss - deductibleAmount);

                float coverageCap = plan.premiumIsAssetBased
                    ? Finance.GetAssetValue(type)
                    : plan.coverageLimit;

                payout = Mathf.Min(insurableLoss, coverageCap);
                totalPayout += payout;
            }
        }

        // 3?? Apply remaining loss
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

        // 4?? Fill result struct
        result.rawLoss = rawLoss;
        result.payout = payout;
        result.deductibleAmount = deductibleAmount;
        result.finalLoss = cappedLoss;
        result.claimApproved = payout > 0f;
        result.waitingPeriodBlocked = waitingBlocked;
        result.lapsedBlocked = lapsedBlocked;

        Debug.Log($"[Insurance] Event {type}: Raw ${rawLoss:F2}, Deductible ${deductibleAmount:F2}, Paid ${payout:F2}, Player ${cappedLoss:F2}");

        return result;
    }

    // Misc / reporting
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

    public void ResetAll()
    {
        totalLoss = 0f;
        totalPayout = 0f;

        foreach (var plan in allPlans)
        {
            plan.isSubscribed = false;
            plan.isLapsed = false;
            plan.monthsPaid = 0;
            plan.missedPayments = 0;
            plan.monthsInCycle = 0;
        }

        AnyPremiumPaidThisMonth = false;
    }

    public void EnableBasicPlan()
    {
        Debug.Log("[Insurance] Enabling all eligible base plans.");

        foreach (var plan in allPlans)
        {
            if (!plan.isSubscribed && PlayerMeetsRequirement(plan))
            {
                BuyInsurance(plan.type);
            }
        }
    }
}
