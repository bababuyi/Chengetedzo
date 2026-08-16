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
        Crop,
        BurialSociety,
        MotorComprehensive // appended at the end - existing assets store this as an int, do not reorder
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

        // Set whenever a premium is actually charged (purchase or monthly billing).
        // ProcessMonthlyPremiums checks this before charging so a plan bought earlier
        // in the same month isn't billed a second time in the same ConfirmMonthAndResolve
        // call, which was also silently double-counting monthsPaid against the waiting
        // period. -1 means "never charged."
        public int lastChargedMonth = -1;

        // Cancelling and rebuying inside the same month is a UI correction, not a real
        // lapse: no month elapsed, the premium was refunded, no risk was carried. These
        // let BuyInsurance tell that apart from a genuine lapse and restore the waiting
        // period clock instead of wiping it. -1 means "not currently in a same-month
        // cancel window."
        public int cancelledMonth = -1;
        public int monthsPaidAtCancel = 0;

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

        // Plan context for surfacing the denial to the player (see EventManager's
        // insurance branches). isSubscribed is the load-bearing field here: a plan
        // that was never bought can still trip waitingPeriodBlocked (monthsPaid=0 <
        // waitingPeriodMonths), so callers must gate any player-facing waiting-period
        // message on isSubscribed==true, not on waitingPeriodBlocked alone.
        public bool isSubscribed;
        public string planName;
        public int monthsPaid;
        public int waitingPeriodMonths;
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
            planName = "Burial Society (Informal)",
            type = InsuranceType.BurialSociety,
            premium = 10f,
            coverageLimit = 450f,
            waitingPeriodMonths = 3,
            coverageDescription = "A community-run burial society. Monthly contributions are pooled to cover funeral costs for any member's family. Informal but trusted, and available to anyone."
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
            planName = "Third Party Motor",
            type = InsuranceType.Motor,
            premiumIsAssetBased = false,
            premium = 104f,             // charged every 4 months
            billingCycleMonths = 4,
            coverageLimit = 3000f,
            waitingPeriodMonths = 0,
            requiredAsset = GameManager.AssetRequirement.Motor,
            coverageDescription = "Legally required. Covers liability for death, bodily injury, and property damage to others. Does not pay out if your own vehicle is stolen, damaged, or destroyed."
        });

        allPlans.Add(new InsurancePlan
        {
            planName = "Comprehensive Motor",
            type = InsuranceType.MotorComprehensive,
            premiumIsAssetBased = false,
            premium = 360f,             // charged every 4 months - several times Third Party's rate
            billingCycleMonths = 4,
            coverageLimit = 6000f,
            waitingPeriodMonths = 0,
            requiredAsset = GameManager.AssetRequirement.Motor,
            coverageDescription = "Everything Third Party covers, plus theft, fire, and damage to your own vehicle."
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

    // True if either motor plan (Third Party or Comprehensive) currently has live coverage.
    public bool HasActiveMotorCover()
    {
        var motor = GetPlan(InsuranceType.Motor);
        var comprehensive = GetPlan(InsuranceType.MotorComprehensive);
        return (motor != null && motor.coverageMonthsRemaining > 0) ||
               (comprehensive != null && comprehensive.coverageMonthsRemaining > 0);
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

        // BurialSociety is a flat household contribution, not per-person
        if (plan.type == InsuranceType.BurialSociety)
            return plan.premium;

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

        if (type == InsuranceType.Motor || type == InsuranceType.MotorComprehensive)
        {
            if (plan.coverageMonthsRemaining > 0)
            {
                Debug.Log($"[Insurance] {plan.planName} already active.");
                return false;
            }

            // Third Party and Comprehensive cover the same vehicle - only one can be
            // active at a time. Comprehensive already includes third-party liability.
            InsuranceType otherType = type == InsuranceType.Motor
                ? InsuranceType.MotorComprehensive
                : InsuranceType.Motor;
            var otherPlan = GetPlan(otherType);

            if (otherPlan != null && otherPlan.coverageMonthsRemaining > 0)
            {
                Debug.LogWarning($"[Insurance] Cannot buy {plan.planName}: {otherPlan.planName} is already active. Cancel it first.");
                return false;
            }

            float cost = plan.premium;

            if (Finance.CashOnHand < cost)
            {
                Debug.LogWarning($"[Insurance] Not enough money for {plan.planName}.");
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

            Debug.Log($"[Insurance] {plan.planName} purchased for 4 months.");
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

            // Cancelling and rebuying within the same month is a UI correction, not a
            // lapse - no month elapsed, the premium was refunded via CancelInsurance,
            // no risk was carried. Restore the clock instead of restarting it. A genuine
            // lapse (handled above) or a cancel from an earlier month still resets it.
            if (plan.cancelledMonth == GameManager.Instance.currentMonth)
            {
                plan.monthsPaid = plan.monthsPaidAtCancel;
                plan.cancelledMonth = -1;
                plan.monthsPaidAtCancel = 0;
                Debug.Log($"[Insurance] {plan.planName} rebought same month as cancel - waiting period clock restored to {plan.monthsPaid}/{plan.waitingPeriodMonths}.");
            }
            else
            {
                plan.monthsPaid = 1;
            }

            plan.missedPayments = 0;
            plan.isLapsed = false;
            plan.monthsInCycle = 0;
            plan.lastChargedMonth = GameManager.Instance.currentMonth;

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

        if ((type == InsuranceType.Motor || type == InsuranceType.MotorComprehensive) && !plan.canCancelThisMonth)
        {
            Debug.Log($"[Insurance] {plan.planName} cannot be canceled after month 1.");
            return;
        }

        float refund = CalculateMonthlyPremiumForPlan(plan);

        GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.InsuranceRefund,
            $"Insurance Refund - {plan.planName}",
            refund,
            true
        );

        // Stamp the cancel month and the clock it had before wiping monthsPaid, so a
        // rebuy later in this same month can restore it instead of restarting the wait.
        plan.cancelledMonth = GameManager.Instance.currentMonth;
        plan.monthsPaidAtCancel = plan.monthsPaid;

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
            if (plan.type == InsuranceType.Motor || plan.type == InsuranceType.MotorComprehensive)
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

                        Debug.Log($"[Insurance] {plan.planName} expired.");
                    }
                }
                // Only roll the "no motor insurance" fine once per month, keyed off the
                // Motor entry specifically - otherwise Motor and MotorComprehensive would
                // each roll independently and could double-fine the same uninsured car.
                // HasActiveMotorCover checks both plans, so Comprehensive coverage still
                // protects the player from this fine when it's the one actually held.
                else if (plan.type == InsuranceType.Motor &&
                         Finance != null &&
                         GameManager.Instance.financeManager.assets.hasMotor &&
                         !HasActiveMotorCover() &&
                         Random.value < 0.15f)
                {
                    GameManager.Instance.ApplyMoneyChange(
                        FinancialEntry.EntryType.EventLoss,
                        "Traffic Fine: No Motor Insurance",
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
                        "Traffic Fine: No Motor Insurance",
                        fine,
                        false
                    );
                    Debug.Log($"[Insurance] Motor fine issued: ${fine:F0}");
                }
                continue;
            }

            // A plan bought earlier this same month was already charged its first
            // premium in BuyInsurance. Without this guard, ConfirmMonthAndResolve
            // running ProcessMonthlyPremiums right after would bill it again and bump
            // monthsPaid a second time in the same month, letting the waiting period
            // clock run roughly twice as fast as intended for the first cycle.
            if (plan.lastChargedMonth == GameManager.Instance.currentMonth)
            {
                continue;
            }

            // Quarterly billing cycle check
            plan.monthsInCycle++;
            bool isDueThisMonth = plan.monthsInCycle >= plan.billingCycleMonths;

            if (!isDueThisMonth)
            {
                // Policy remains active between billing months - no charge
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
                plan.lastChargedMonth = GameManager.Instance.currentMonth;

                AnyPremiumPaidThisMonth = true;
            }
            else
            {
                plan.missedPayments++;

                if (plan.missedPayments == 1)
                {
                    Debug.Log($"[Insurance] {plan.planName} missed payment - grace month.");
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

    public bool CanClaimForEvent(InsuranceType type)
    {
        var plan = GetPlan(type);
        return plan != null && plan.isSubscribed && !plan.isLapsed &&
               plan.monthsPaid >= plan.waitingPeriodMonths && plan.CanClaim();
    }

    public (float payout, float deductible) CalculateClaim(InsuranceType type, float rawLoss)
    {
        var plan = GetPlan(type);
        if (plan == null) return (0f, 0f);
        float deductible = rawLoss * (plan.deductiblePercent / 100f);
        float insurableLoss = Mathf.Max(0f, rawLoss - deductible);
        float coverageCap = plan.premiumIsAssetBased ? Finance.GetAssetValue(type) : plan.coverageLimit;
        float payout = Mathf.Min(insurableLoss, coverageCap);
        return (payout, deductible);
    }

    public void RecordClaimBookkeeping(InsuranceType type, float payout)
    {
        totalPayout += payout;
    }

    public InsuranceResult HandleEvent(InsuranceType type, float rawLoss, string eventName = "Unknown Event", List<InsuranceType> additionalCoveredBy = null)

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
            else if (!plan.isSubscribed)
            {
                // Never bought this cover. Confirmed via the diagnostic log on a real
                // playthrough: Month 1, Medication Costs (Health) - isSubscribed=False,
                // monthsPaid=0, waitingPeriodMonths=3 - fell into the check below purely
                // because 0 < 3 is always true for a plan that was never purchased, and
                // reported "Claim blocked: waiting period" for cover that didn't exist.
                // No cover, no claim, and no waiting-period/lapsed flag to report - this
                // is silently uninsured, not a denial.
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

        // Secondary policies (EventData.additionalCoveredBy). Each gets its own
        // coverage limit, deductible and waiting-period check via CanClaim(), but all of
        // them are measuring the SAME modeled loss the primary just evaluated - so each
        // secondary only pays the shortfall still open after the primary and any earlier
        // secondary, never the full rawLoss again. Without this, two policies covering
        // the same loss would sum past it into a windfall (this is exactly what the old
        // hardcoded Funeral->BurialSociety branch got wrong: it computed BurialSociety's
        // payout against the original rawLoss instead of what was left).
        // No waitingBlocked/lapsedBlocked reporting for secondaries - only the primary is
        // ever a player claim decision, secondaries resolve silently either way.
        if (additionalCoveredBy != null)
        {
            foreach (var secondaryType in additionalCoveredBy)
            {
                float remainingLoss = Mathf.Max(0f, rawLoss - payout);
                if (remainingLoss <= 0f) break;

                var secondaryPlan = GetPlan(secondaryType);
                if (secondaryPlan == null || !secondaryPlan.CanClaim()) continue;

                float secondaryDeductible = remainingLoss * (secondaryPlan.deductiblePercent / 100f);
                float secondaryInsurable = Mathf.Max(0f, remainingLoss - secondaryDeductible);
                float secondaryCap = secondaryPlan.premiumIsAssetBased
                    ? Finance.GetAssetValue(secondaryType)
                    : secondaryPlan.coverageLimit;
                float secondaryPayout = Mathf.Min(secondaryInsurable, secondaryCap);

                if (secondaryPayout > 0f)
                {
                    payout += secondaryPayout;
                    totalPayout += secondaryPayout;
                    Debug.Log($"[Insurance] {secondaryPlan.planName} secondary payout: ${secondaryPayout:F2}");
                }
            }
        }

        // 3 Apply remaining loss
        float netLoss = Mathf.Max(0f, rawLoss - payout);
        float cappedLoss = GameManager.Instance.ApplyMonthlyDamage(netLoss);

        if (cappedLoss > 0f)
        {
            GameManager.Instance.ApplyMoneyChange(FinancialEntry.EntryType.EventLoss,eventName,cappedLoss,false);
        }
        totalLoss += cappedLoss;

        // 4 Fill result struct
        result.rawLoss = rawLoss;
        result.payout = payout;
        result.deductibleAmount = deductibleAmount;
        result.finalLoss = cappedLoss;
        result.claimApproved = payout > 0f;
        result.waitingPeriodBlocked = waitingBlocked;
        result.lapsedBlocked = lapsedBlocked;
        result.isSubscribed = plan != null && plan.isSubscribed;
        result.planName = plan != null ? plan.planName : "";
        result.monthsPaid = plan != null ? plan.monthsPaid : 0;
        result.waitingPeriodMonths = plan != null ? plan.waitingPeriodMonths : 0;

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
            // These two were missing here, which was the actual cause of insurance
            // carrying into a new playthrough (e.g. "Third Party Motor already active"
            // on a fresh restart) - BuyInsurance's re-buy guard checks
            // coverageMonthsRemaining, not isSubscribed, so leaving it nonzero silently
            // blocked re-purchasing even though every other flag looked reset.
            plan.coverageMonthsRemaining = 0;
            plan.canCancelThisMonth = false;
            // Reset alongside the rest - a stale lastChargedMonth could coincidentally
            // match month 1 of a fresh run and wrongly skip that plan's first charge.
            plan.lastChargedMonth = -1;
            // Same reasoning as lastChargedMonth: a stale cancelledMonth could
            // coincidentally match month 1 of a fresh run and wrongly restore a
            // leftover monthsPaidAtCancel from the previous playthrough.
            plan.cancelledMonth = -1;
            plan.monthsPaidAtCancel = 0;
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
