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

    public void ProcessContribution()
    {
        PaidThisMonth = true;
        totalContributed += contribution;
        monthsContributed++;
        UpdateBorrowingPower();

        Debug.Log($"[Loan] Contributed ${contribution}, Power: ${borrowingPower}");
    }

    public void Borrow(float amount)
    {
        if (amount <= borrowingPower)
        {
            loanBalance += amount;
            Debug.Log($"Borrowed ${amount} from pool");
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

        //Loan unlock moment
        if (!loanUnlocked && borrowingPower > 0f)
        {
            loanUnlocked = true;
            UIManager.Instance.ShowLoanPanel();
        }
    }

    public void ResetMonthlyFlags()
    {
        PaidThisMonth = false;
    }
}
