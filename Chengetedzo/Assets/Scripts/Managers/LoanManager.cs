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
    [Range(0.05f, 0.25f)]
    public float repaymentRate = 0.1f; // 5%–25%

    public bool PaidThisMonth { get; private set; }
    private bool loanUnlocked = false;
    public bool BorrowedThisMonth { get; private set; }
    public bool IsLoanUnlocked => borrowingPower > 0f;


    public void ProcessContribution()
    {
        Debug.Log("[Loan] ProcessContribution CALLED");

        PaidThisMonth = true;
        totalContributed += contribution;
        monthsContributed++;
        UpdateBorrowingPower();

        Debug.Log($"[Loan] Contributed ${contribution}, Total: ${totalContributed}, Months: {monthsContributed}");
    }

    public void Borrow(float amount)
    {
        if (BorrowedThisMonth)
        {
            Debug.Log("Already borrowed this month.");
            return;
        }

        if (amount <= borrowingPower)
        {
            loanBalance += amount;
            GameManager.Instance.financeManager.cashOnHand += amount;
            borrowingPower -= amount;
            BorrowedThisMonth = true;

            Debug.Log($"Borrowed ${amount}. Remaining power: ${borrowingPower}");
        }
        else
        {
            Debug.Log("Not enough borrowing power!");
        }
    }

    public void UpdateLoans()
    {
        if (loanBalance <= 0f)
            return;

        float repayment = loanBalance * repaymentRate;
        loanBalance -= repayment;

        Debug.Log($"[Loan] Repayment: ${repayment}");
    }

    private void UpdateBorrowingPower()
    {
        float previousPower = borrowingPower;

        if (monthsContributed < 3) borrowingPower = 0f;
        else if (monthsContributed == 3) borrowingPower = totalContributed;
        else if (monthsContributed == 4) borrowingPower = totalContributed * 1.5f;
        else borrowingPower = totalContributed * 2f;

        if (!loanUnlocked && borrowingPower > 0f)
        {
            loanUnlocked = true;

            UIManager.Instance.ShowLoanTopButton();
        }
    }

    public void ResetMonthlyFlags()
    {
        PaidThisMonth = false;
        BorrowedThisMonth = false;
    }
}
