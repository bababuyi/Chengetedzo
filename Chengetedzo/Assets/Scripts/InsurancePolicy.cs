using UnityEngine;

public class InsurancePolicy : MonoBehaviour
{
    public string policyName;
    public float premiumAmount;
    public int waitingPeriodMonths;
    public int monthsPaid = 0;
    public int missedPayments = 0;
    public bool isActive => monthsPaid >= waitingPeriodMonths;
    public bool isLapsed => missedPayments >= 2;
}

