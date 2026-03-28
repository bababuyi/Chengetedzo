using UnityEngine;
using System.Collections.Generic;

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
    public float insurancePayout;

    public bool hasChoices;
    public List<EventData.ChoiceOption> choices;
    public string senderName;
    public string senderRelation;
}