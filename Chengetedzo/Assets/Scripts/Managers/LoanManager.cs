using UnityEngine;

// Two independent borrowing channels, per the design brief:
// - Moneylender ("Ndlovu"): always available, no waiting period, expensive (25% interest,
//   max 6-month term).
// - Mukando: cheap (5% interest, max 12-month term, extendable to 18 on missed payments),
//   but requires 3 months of contribution before any borrowing power exists.
// Term-based instalment model: borrowing at a chosen term fixes a flat monthly instalment
// that actually pays the loan off in that many months, instead of a percentage-of-balance
// repayment that asymptotes and never clears.
// Forced loans draw from Mukando first, then fall back to the Moneylender, at whichever
// account's maximum term (the player isn't present to choose).
[System.Serializable]
public class LoanAccount
{
    public string label;             // "Moneylender" / "Mukando"
    public float balance;
    public float borrowingPower;
    public int maxTermMonths;        // Mukando 12, Moneylender 6
    public float interestRate;       // Mukando 0.05, Moneylender 0.25
    public int termMonths;           // months remaining on the current schedule
    public float monthlyInstalment;  // fixed, recomputed whenever balance/term changes
    public int missedPayments;
    public int onTimePayments;
    public float totalContributed;   // Mukando only
    public int monthsContributed;    // Mukando only
}

public class LoanManager : MonoBehaviour
{
    [Header("Mukando Contribution")]
    public float contribution = 50f;
    public bool mukandoJoined = false;

    [Header("Loan Accounts")]
    public LoanAccount moneylender = new LoanAccount { label = "Moneylender" };
    public LoanAccount mukando = new LoanAccount { label = "Mukando" };

    [Header("Terms and Interest")]
    public int moneylenderMaxTermMonths = 6;
    public float moneylenderInterestRate = 0.25f;
    public int mukandoMaxTermMonths = 12;
    public float mukandoInterestRate = 0.05f;
    public int mukandoTermHardCap = 18;

    [Header("Moneylender Cap")]
    // 3x current monthly income, rounded to the nearest $50 - scales across profiles
    // instead of being generous to a high earner and out of reach for a low one.
    public float moneylenderCapIncomeMultiplier = 3f;
    public float moneylenderCapRounding = 50f;

    // Consecutive missed Mukando contributions and the standing-recovery mechanic.
    // "Missing mukando contributions while opted in damages standing, it does not raise
    // the rate. Real savings groups suspend you, they do not charge you more."
    private int mukandoConsecutiveMisses = 0;
    private int mukandoRecoveryMonthsNeeded = 0;
    public int MukandoConsecutiveMisses => mukandoConsecutiveMisses;
    public int MukandoRecoveryMonthsNeeded => mukandoRecoveryMonthsNeeded;

    private bool loanUnlocked = false;
    public bool IsLoanUnlocked => loanUnlocked;

    public bool CanForceLoan =>
        moneylender.borrowingPower > 0f || mukando.borrowingPower > 0f;

    public bool ContributedThisMonth { get; private set; }
    public bool RepaidThisMonth { get; private set; }
    public bool BorrowedThisMonth { get; private set; }

    private void Awake()
    {
        ApplyAccountConfig();
    }

    private void ApplyAccountConfig()
    {
        moneylender.label = "Moneylender";
        moneylender.maxTermMonths = moneylenderMaxTermMonths;
        moneylender.interestRate = moneylenderInterestRate;

        mukando.label = "Mukando";
        mukando.maxTermMonths = mukandoMaxTermMonths;
        mukando.interestRate = mukandoInterestRate;
    }

    // Called once from GameManager.ResetForNewGame after TutorialManager.ResetRunState,
    // so the "seen it before" flag TutorialManager checks is already cleared for a fresh run.
    // Ndlovu is available from month 1, so this just reveals the loan UI and shows the
    // one-time intro - it no longer gates any actual borrowing power.
    public void AnnounceLoanSystem()
    {
        loanUnlocked = true;
        UIManager.Instance.ShowLoanTopButton();
        TutorialManager.Instance?.OnLoanUnlocked();
    }

    public void JoinMukando()
    {
        mukandoJoined = true;
    }

    // Opting out stops the monthly contribution but does not clear existing Mukando
    // borrowing power or balance - opting back in resumes building on top of what's there.
    public void LeaveMukando()
    {
        mukandoJoined = false;
        mukandoConsecutiveMisses = 0;
    }

    public void ProcessContribution()
    {
        // Moneylender's cap tracks current income, which can move month to month
        // (income effects/benefits) - keep it current regardless of Mukando membership.
        UpdateMoneylenderBorrowingPower();

        if (!mukandoJoined)
        {
            ContributedThisMonth = false;
            return;
        }

        if (GameManager.Instance.financeManager.CashOnHand < contribution)
        {
            ContributedThisMonth = false;
            MissedMukandoContribution();
            return;
        }

        GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.LoanContribution,
            "Mukando Contribution",
            contribution,
            false
        );
        ContributedThisMonth = true;

        mukando.totalContributed += contribution;
        mukando.monthsContributed++;
        mukandoConsecutiveMisses = 0;

        if (mukandoRecoveryMonthsNeeded > 0)
        {
            mukandoRecoveryMonthsNeeded--;
            if (mukandoRecoveryMonthsNeeded > 0)
            {
                // Still rebuilding standing - borrowing power stays suppressed until
                // two consecutive contributions land.
                return;
            }
        }

        UpdateMukandoBorrowingPower();
    }

    private void MissedMukandoContribution()
    {
        mukandoConsecutiveMisses++;

        if (mukandoRecoveryMonthsNeeded > 0)
        {
            // Broke the recovery streak - the two-month rebuild has to start over.
            mukandoRecoveryMonthsNeeded = 2;
            Debug.Log("[Loan] Mukando recovery streak broken by another missed contribution. Restarting.");
            return;
        }

        if (mukandoConsecutiveMisses >= 3)
        {
            mukando.borrowingPower = 0f;
            mukandoRecoveryMonthsNeeded = 2;
            Debug.Log("[Loan] Mukando standing damaged - three consecutive missed contributions. Borrowing power suspended until two months of contributions resume.");
        }
    }

    // Shared by voluntary Borrow and ForceBorrow. Applies interest to the new amount,
    // adds it to whatever balance already exists, and recomputes a single instalment
    // across the chosen term rather than stacking two separate schedules.
    private void ApplyBorrowToAccount(LoanAccount account, float amount, int chosenTerm)
    {
        int term = Mathf.Clamp(chosenTerm, 1, account.maxTermMonths);
        float owed = amount * (1f + account.interestRate);

        account.balance += owed;
        account.termMonths = term;
        account.monthlyInstalment = account.balance / term;
        account.borrowingPower = Mathf.Max(0f, account.borrowingPower - amount);
    }

    // Voluntary borrow from a specific account, at a term the player chose (1..maxTermMonths).
    public bool Borrow(float amount, LoanAccount account, int chosenTerm)
    {
        var phase = GameManager.Instance.CurrentPhase;

        if (phase != GameManager.GamePhase.Simulation &&
            phase != GameManager.GamePhase.Loan)
            return false;

        if (BorrowedThisMonth)
        {
            Debug.Log("Already borrowed this month.");
            return false;
        }

        if (account == null || amount <= 0f || amount > account.borrowingPower)
        {
            Debug.Log($"Not enough borrowing power from {(account != null ? account.label : "that source")}!");
            return false;
        }

        ApplyBorrowToAccount(account, amount, chosenTerm);
        BorrowedThisMonth = true;

        GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.LoanBorrow,
            $"{account.label} Loan Borrowed",
            amount,
            true
        );

        Debug.Log($"Borrowed {GameUtils.FormatMoney(amount)} from {account.label} over {account.termMonths} months. Instalment: ${account.monthlyInstalment:F2}/month.");
        return true;
    }

    public void UpdateLoans()
    {
        bool moneylenderRepaid = ProcessRepayment(moneylender);
        bool mukandoRepaid = ProcessRepayment(mukando);
        RepaidThisMonth = moneylenderRepaid || mukandoRepaid;
    }

    private bool ProcessRepayment(LoanAccount account)
    {
        if (account.balance <= 0f)
            return false;

        float repayment = Mathf.Min(account.monthlyInstalment, account.balance);

        if (GameManager.Instance.financeManager.CashOnHand >= repayment)
        {
            GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.LoanRepayment,
                $"{account.label} Repayment",
                repayment,
                false
            );

            account.balance = Mathf.Max(0f, account.balance - repayment);
            account.termMonths = Mathf.Max(0, account.termMonths - 1);
            account.onTimePayments++;

            if (account.missedPayments > 0)
                account.missedPayments--;

            if (account.balance <= 0.01f)
            {
                account.balance = 0f;
                account.termMonths = 0;
                account.monthlyInstalment = 0f;
            }

            Debug.Log($"[Loan] {account.label} instalment paid: ${repayment:F0}. Remaining balance: ${account.balance:F0}");

            if (account.onTimePayments == 2 && !GameManager.Instance.HasMentorSpokenThisMonth())
            {
                UIManager.Instance.ShowMentorMessage(
                    MentorLines.LoanRecovery[Random.Range(0, MentorLines.LoanRecovery.Length)]
                );
                GameManager.Instance.SetMentorSpokeThisMonth(true);
            }

            if (account == mukando)
                UpdateMukandoBorrowingPower();
            else
                UpdateMoneylenderBorrowingPower();

            return true;
        }

        MissedRepayment(account);
        return false;
    }

    // Moneylender: a missed instalment adds a 10% penalty to the outstanding balance and
    // recomputes the instalment over the remaining term.
    // Mukando: no monetary penalty - missing a repayment extends the remaining term by one
    // month (spreading the same balance thinner) up to a hard cap of 18 months, after which
    // it starts behaving like the Moneylender (10% penalty) instead.
    private void MissedRepayment(LoanAccount account)
    {
        account.missedPayments++;
        PlayerDataManager.Instance.ModifyMomentum(-4f);

        if (account == mukando && mukando.termMonths < mukandoTermHardCap)
        {
            mukando.termMonths = Mathf.Min(mukandoTermHardCap, mukando.termMonths + 1);
            mukando.monthlyInstalment = mukando.balance / Mathf.Max(1, mukando.termMonths);
            Debug.Log($"[Loan] Missed Mukando instalment - term extended to {mukando.termMonths} months. New instalment: ${mukando.monthlyInstalment:F2}");
        }
        else
        {
            account.balance *= 1.10f;
            account.monthlyInstalment = account.balance / Mathf.Max(1, account.termMonths);
            string reason = account == mukando ? "at 18-month cap, now penalized like the Moneylender" : "10% penalty";
            Debug.Log($"[Loan] Missed {account.label} instalment - {reason}. New balance: ${account.balance:F0}, instalment: ${account.monthlyInstalment:F2}");
        }

        if (account.missedPayments == 3)
        {
            PlayerDataManager.Instance.ModifyMomentum(-6f);
            if (!GameManager.Instance.HasMentorSpokenThisMonth())
            {
                UIManager.Instance.ShowMentorMessage(
                    MentorLines.MissedLoan[Random.Range(0, MentorLines.MissedLoan.Length)]
                );
                GameManager.Instance.SetMentorSpokeThisMonth(true);
            }
        }
    }

    private void UpdateMukandoBorrowingPower()
    {
        if (mukando.monthsContributed < 3)
        {
            mukando.borrowingPower = 0f;
            return;
        }

        float gross = mukando.monthsContributed switch
        {
            3 => mukando.totalContributed,
            4 => mukando.totalContributed * 1.5f,
            _ => mukando.totalContributed * 2f
        };

        mukando.borrowingPower = Mathf.Max(0f, gross - mukando.balance);
    }

    // Public so FinanceManager.InitializeFromSetup can call it once income is actually
    // known (ResetAll runs before setup, so its own call computes income * multiplier = 0).
    public void UpdateMoneylenderBorrowingPower()
    {
        float cap = GetMoneylenderCap();
        moneylender.borrowingPower = Mathf.Max(0f, cap - moneylender.balance);
    }

    // 3x current monthly income, rounded to the nearest $50.
    public float GetMoneylenderCap()
    {
        float income = GameManager.Instance != null && GameManager.Instance.financeManager != null
            ? GameManager.Instance.financeManager.currentIncome
            : 0f;

        float raw = income * moneylenderCapIncomeMultiplier;
        return Mathf.Round(raw / moneylenderCapRounding) * moneylenderCapRounding;
    }

    public void ResetMonthlyFlags()
    {
        ContributedThisMonth = false;
        RepaidThisMonth = false;
        BorrowedThisMonth = false;
    }

    // Forced/emergency borrowing. Draws from Mukando first (the cheap channel you built
    // by saving), then falls back to the Moneylender for whatever's left. The player isn't
    // present to choose a term, so each draw uses that account's maximum term.
    // Returns the amount actually borrowed (0 if neither account had any borrowing power)
    // so the caller can tell a real loan from a no-op instead of assuming success.
    public float ForceBorrow(float requiredAmount)
    {
        float remaining = Mathf.Max(0f, requiredAmount);
        if (remaining <= 0f) return 0f;

        float fromMukando = 0f;
        if (mukando.borrowingPower > 0f)
        {
            fromMukando = Mathf.Min(remaining, mukando.borrowingPower);
            ApplyBorrowToAccount(mukando, fromMukando, mukando.maxTermMonths);
            remaining -= fromMukando;

            GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.LoanBorrow,
                "Mukando Forced Loan",
                fromMukando,
                true
            );
            Debug.Log($"[Loan] FORCED loan drawn from Mukando: ${fromMukando:F0} over {mukando.maxTermMonths} months.");
        }

        float fromMoneylender = 0f;
        if (remaining > 0.01f && moneylender.borrowingPower > 0f)
        {
            fromMoneylender = Mathf.Min(remaining, moneylender.borrowingPower);
            ApplyBorrowToAccount(moneylender, fromMoneylender, moneylender.maxTermMonths);
            remaining -= fromMoneylender;

            GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.LoanBorrow,
                "Moneylender Forced Loan",
                fromMoneylender,
                true
            );
            Debug.Log($"[Loan] FORCED loan drawn from Moneylender: ${fromMoneylender:F0} over {moneylender.maxTermMonths} months.");
        }

        float totalBorrowed = fromMukando + fromMoneylender;

        if (totalBorrowed <= 0f)
        {
            Debug.LogWarning("[Loan] Forced loan requested but no borrowing power available from either account.");
            return 0f;
        }

        if (!GameManager.Instance.HasMentorSpokenThisMonth())
        {
            UIManager.Instance.ShowMentorMessage(
                "Taking on debt is sometimes necessary, but always have a repayment plan. " +
                "Missing payments can damage your financial momentum."
            );
            GameManager.Instance.SetMentorSpokeThisMonth(true);
        }

        return totalBorrowed;
    }

    // Some event choices nudge the player's borrowing standing. Mukando is the channel
    // that's about community trust/reputation, so that's what this affects - the
    // Moneylender's cap is derived from income and doesn't move with events.
    public void ModifyBorrowingPower(float amount)
    {
        mukando.borrowingPower = Mathf.Max(0f, mukando.borrowingPower + amount);
        Debug.Log($"[Loan] Mukando borrowing power changed by ${amount:F0}. Now: ${mukando.borrowingPower:F0}");
    }

    // Restores both accounts from a save. Falls back to fresh accounts if a field is
    // missing (loading a save from before this system existed isn't supported - saves
    // break across this change by design - but this keeps a null from propagating).
    public void RestoreFromSave(
        LoanAccount savedMoneylender, LoanAccount savedMukando,
        bool unlocked, bool joined,
        int consecutiveMisses, int recoveryMonthsNeeded)
    {
        moneylender = savedMoneylender ?? new LoanAccount { label = "Moneylender" };
        mukando = savedMukando ?? new LoanAccount { label = "Mukando" };

        // Term/interest config is a live balance parameter, not player state - always take
        // the current Inspector values rather than whatever was saved.
        ApplyAccountConfig();

        loanUnlocked = unlocked;
        mukandoJoined = joined;
        mukandoConsecutiveMisses = consecutiveMisses;
        mukandoRecoveryMonthsNeeded = recoveryMonthsNeeded;
    }

    public void ForceUnlock(float baseAmount = 150f)
    {
        mukando.monthsContributed = 3;
        mukando.totalContributed = baseAmount;
        mukando.borrowingPower = baseAmount;
        loanUnlocked = true;
    }

    public void ResetAll()
    {
        contribution = 50f;
        mukandoJoined = false;

        moneylender = new LoanAccount { label = "Moneylender" };
        mukando = new LoanAccount { label = "Mukando" };
        ApplyAccountConfig();

        // Ndlovu has no waiting period - his borrowing power is live immediately.
        UpdateMoneylenderBorrowingPower();

        mukandoConsecutiveMisses = 0;
        mukandoRecoveryMonthsNeeded = 0;

        ContributedThisMonth = false;
        RepaidThisMonth = false;
        BorrowedThisMonth = false;
        loanUnlocked = false;
    }
}
