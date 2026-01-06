using UnityEngine;

public class SavingsManager : MonoBehaviour
{
    public float schoolFeesSavings = 0f;
    public float generalSavings = 0f;
    public float monthlyInterestRate = 0.02f;
    public float LastMonthSavings { get; private set; }

    public void DepositToSchoolFees(float amount)
    {
        schoolFeesSavings += amount;
        Debug.Log($"Deposited ${amount} to School Fees Savings");
    }

    public void DepositToGeneral(float amount)
    {
        generalSavings += amount;
        Debug.Log($"Deposited ${amount} to General Savings");
    }

    public void AccrueInterest()
    {
        LastMonthSavings = schoolFeesSavings + generalSavings;
        schoolFeesSavings += schoolFeesSavings * monthlyInterestRate;
        generalSavings += generalSavings * monthlyInterestRate;
        Debug.Log($"Savings interest applied: {monthlyInterestRate * 100}%");
    }
}
