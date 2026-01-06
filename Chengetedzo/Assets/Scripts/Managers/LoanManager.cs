using UnityEngine;

public class LoanManager : MonoBehaviour
{
    public float contribution = 1f;  // Monthly pool contribution
    public float totalContributed;
    public float loanBalance;
    public float borrowingPower = 0f;
    public int monthsContributed = 0;
    public bool PaidThisMonth { get; private set; }

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
        else Debug.Log("Not enough borrowing power!");
    }

    public void UpdateLoans()
    {
        if (loanBalance > 0)
        {
            float repayment = loanBalance * 0.1f; // 10% repayment/month
            loanBalance -= repayment;
            Debug.Log($"Loan repayment: ${repayment}");
        }
    }

    private void UpdateBorrowingPower()
    {
        if (monthsContributed < 3) borrowingPower = 0f;
        else if (monthsContributed == 3) borrowingPower = totalContributed;
        else if (monthsContributed == 4) borrowingPower = totalContributed * 1.5f;
        else borrowingPower = totalContributed * 2f;
    }
    public void ResetMonthlyFlags()
    {
        PaidThisMonth = false;
    }
}
