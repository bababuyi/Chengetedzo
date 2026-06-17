using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    [SerializeField] private int adults = 1;
    [SerializeField] private int children = 0;
    [SerializeField] private int originalAdults = 1;
    public int OriginalAdults => Mathf.Max(1, originalAdults);

    [SerializeField] private float financialMomentum;
    [SerializeField] private float familyMorale;
    [SerializeField] private float socialMorale;

    public float FinancialMomentum => financialMomentum;
    public float FamilyMorale => familyMorale;
    public float SocialMorale => socialMorale;
    public float CompositeMorale => (familyMorale * 0.6f) + (socialMorale * 0.4f);
    public float FinalScore => (financialMomentum + CompositeMorale) / 2f;
    public int RawAdults => adults;
    public int Adults
    {
        get => Mathf.Max(1, adults);
        set => adults = Mathf.Max(1, value);
    }

    public int Children
    {
        get => Mathf.Max(0, children);
        set => children = Mathf.Max(0, value);
    }

    public void SetInitialHousehold(int adultCount, int childCount)
    {
        adults = Mathf.Max(1, adultCount);
        children = Mathf.Max(0, childCount);
        originalAdults = adults;
        Debug.Log($"[Household] Initial set — adults: {adults}, children: {children}, originalAdults: {originalAdults}");
    }

    public void SetOriginalAdults(int value)
    {
        originalAdults = Mathf.Max(1, value);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        adults = Mathf.Max(1, adults);
        children = Mathf.Max(0, children);
        financialMomentum = Mathf.Clamp(financialMomentum, -100f, 100f);
    }

    public void ModifyMomentum(float amount)
    {
        float oldValue = financialMomentum;
        financialMomentum += amount;
        financialMomentum = Mathf.Clamp(financialMomentum, -100f, 100f);
        Debug.Log($"[Momentum] {oldValue:F1} → {financialMomentum:F1}");
    }

    public void SetMomentum(float value)
    {
        financialMomentum = Mathf.Clamp(value, -100f, 100f);
    }

    public void ModifyFamilyMorale(float amount)
    {
        float oldValue = familyMorale;
        familyMorale += amount;
        familyMorale = Mathf.Clamp(familyMorale, -100f, 100f);
        Debug.Log($"[FamilyMorale] {oldValue:F1} → {familyMorale:F1}");
    }

    public void ModifySocialMorale(float amount)
    {
        float oldValue = socialMorale;
        socialMorale += amount;
        socialMorale = Mathf.Clamp(socialMorale, -100f, 100f);
        Debug.Log($"[SocialMorale] {oldValue:F1} → {socialMorale:F1}");
    }

    public void SetFamilyMorale(float value)
    {
        familyMorale = Mathf.Clamp(value, -100f, 100f);
    }

    public void SetSocialMorale(float value)
    {
        socialMorale = Mathf.Clamp(value, -100f, 100f);
    }

    public void ResetPlayerData()
    {
        financialMomentum = 0f;
        familyMorale = 0f;
        socialMorale = 0f;
        adults = 1;
        children = 0;
        originalAdults = 1;
    }

    public void RemoveAdult()
    {
        if (adults > 0)
        {
            adults--;
            Debug.Log($"[Household] Adult removed. Adults remaining: {adults}");
        }
    }

    public void RemoveChild()
    {
        if (children > 0)
        {
            children--;
            Debug.Log($"[Household] Child removed. Children remaining: {children}");
        }
    }
}