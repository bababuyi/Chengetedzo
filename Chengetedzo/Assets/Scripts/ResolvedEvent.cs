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

    public bool affectsExpenses;
    public string expenseCategoryName;
    public float expenseFlatChange;
    public int expenseEffectMonths;

    public bool hasChoices;
    public List<EventData.ChoiceOption> choices;
    public string senderName;
    public string senderRelation;
    public EventPool pool;
}