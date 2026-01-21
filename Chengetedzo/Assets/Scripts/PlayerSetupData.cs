using UnityEngine;

[System.Serializable]
public class PlayerSetupData
{
    [Header("Income")]
    public float minIncome;
    public float maxIncome;
    public bool isIncomeStable;

    [Header("Household")]
    public int adults;
    public int children;

    [Header("Education")]
    public bool hasSchoolFees;
    public float schoolFeesAmount;

    [Header("Assets")]
    public HousingType housing;
    public bool ownsCar;
    public bool ownsFarm;
}

public enum HousingType
{
    Renting,
    OwnsHouse
}