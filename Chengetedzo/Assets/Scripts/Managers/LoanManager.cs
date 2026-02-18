using UnityEngine;

public class LoanManager : MonoBehaviour
{
    [Header("Contribution")]
    public float contribution = 50f;  // Monthly pool contribution
    public float totalContributed;
    public int monthsContributed = 0;

    [Header("Loan State")]
    public float loanBalance;
    public float borrowingPower = 0f;

    [Header("Repayment Settings")]
    [Range(0.05f, 0.5f)]
    public float repaymentRate = 0.1f; // 5%–50%

    [Header("Default Tracking")]
    public int missedPayments = 0;
    public int onTimePayments = 0;

    //public bool PaidThisMonth { get; private set; }
    private bool loanUnlocked = false;

    public bool ContributedThisMonth { get; private set; }
    public bool RepaidThisMonth { get; private set; }

    public bool BorrowedThisMonth { get; private set; }
    public bool IsLoanUnlocked => borrowingPower > 0f;
    public bool CanForceLoan => IsLoanUnlocked && borrowingPower > 0f;

    public void ProcessContribution()
    {
        if (GameManager.Instance.financeManager.cashOnHand < contribution)
        {
            ContributedThisMonth = false;
            return;
        }

        GameManager.Instance.financeManager.cashOnHand -= contribution;
        ContributedThisMonth = true;

        totalContributed += contribution;
        monthsContributed++;
        UpdateBorrowingPower();
    }

    public bool Borrow(float amount)
    {
        if (BorrowedThisMonth)
        {
            Debug.Log("Already borrowed this month.");
            return false;
        }

        if (amount > borrowingPower)
        {
            Debug.Log("Not enough borrowing power!");
            return false;
        }

        loanBalance += amount;
        GameManager.Instance.financeManager.cashOnHand += amount;
        borrowingPower -= amount;
        BorrowedThisMonth = true;

        Debug.Log($"Borrowed ${amount}. Remaining power: ${borrowingPower}");
        return true;
    }

    public void UpdateLoans()
    {
        RepaidThisMonth = false;

        if (loanBalance <= 0f)
            return;

        float repayment = loanBalance * repaymentRate;

        if (GameManager.Instance.financeManager.cashOnHand >= repayment)
        {
            GameManager.Instance.financeManager.cashOnHand -= repayment;
            loanBalance -= repayment;

            onTimePayments++;
            RepaidThisMonth = true;

            if (missedPayments > 0)
                missedPayments--;

            if (onTimePayments == 2)
            {
                UIManager.Instance.ShowMentorMessage(
                    MentorLines.LoanRecovery[
                        Random.Range(0, MentorLines.LoanRecovery.Length)
                    ]);
            }

            Debug.Log($"[Loan] Repayment: ${repayment:F0}");
        }
        else
        {
            MissedPayment();
        }
    }

    //private void IncreaseRepaymentRate()
    //{
        //repaymentRate += 0.10f;
        //repaymentRate = Mathf.Clamp(repaymentRate, 0.05f, 0.5f);
    //}

    private void UpdateBorrowingPower()
    {
        float previousPower = borrowingPower;

        if (monthsContributed < 3) borrowingPower = 0f;
        //else if (monthsContributed == 3) borrowingPower = totalContributed;
        //else if (monthsContributed == 4) borrowingPower = totalContributed * 1.5f;
        //else borrowingPower = totalContributed * 2f;

        else if (monthsContributed == 3) borrowingPower = totalContributed;
        else if (monthsContributed == 4) borrowingPower = totalContributed * 1.5f;
        else borrowingPower = totalContributed * 2f;

        borrowingPower = Mathf.Max(0f, borrowingPower - loanBalance);


        if (!loanUnlocked && borrowingPower > 0f)
        {
            loanUnlocked = true;

            UIManager.Instance.ShowLoanTopButton();
        }
    }

    public void ResetMonthlyFlags()
    {
        ContributedThisMonth = false;
        RepaidThisMonth = false;
        BorrowedThisMonth = false;
    }

    public void ForceBorrow(float requiredAmount)
    {
        if (!IsLoanUnlocked || borrowingPower <= 0f)
            return;

        float amount = Mathf.Min(requiredAmount, borrowingPower);

        loanBalance += amount;
        borrowingPower -= amount;
        GameManager.Instance.financeManager.cashOnHand += amount;

        Debug.Log($"[Loan] FORCED loan issued: ${amount:F0}");
    }

    private void MissedPayment()
    {
        missedPayments++;

        repaymentRate = Mathf.Min(repaymentRate + 0.10f, 0.50f);

        PlayerDataManager.Instance.ModifyMomentum(-4f);

        Debug.Log($"[Loan] Missed payment. Repayment rate now {repaymentRate * 100f}%");

        if (missedPayments == 3)
        {
            PlayerDataManager.Instance.ModifyMomentum(-6f);

            UIManager.Instance.ShowMentorMessage(
                MentorLines.MissedLoan[
                    Random.Range(0, MentorLines.MissedLoan.Length)
                ]);
        }
    }

    [Header("Repayment Limits")]
    public float minRepaymentRate = 0.05f;
    public float maxRepaymentRate = 0.50f;

    public void SetRepaymentRate(float value)
    {
        repaymentRate = Mathf.Clamp(value, minRepaymentRate, maxRepaymentRate);
    }
}
