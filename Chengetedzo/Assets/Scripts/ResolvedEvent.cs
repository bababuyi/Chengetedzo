using UnityEngine;

[System.Serializable]
public class ResolvedEvent
{
    public string title;
    public string description;

    public float moneyChange;
    public float incomePercentChange;
    public int incomeDurationMonths;

    public Sprite icon;

    public InsuranceManager.InsuranceType type;

    public float lossPercent;

    public float actualMoneyChange;
    public float insurancePayout;
}