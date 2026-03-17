using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    [SerializeField] private int adults = 1;
    [SerializeField] private int children = 0;

    [SerializeField] private float financialMomentum;
    public float FinancialMomentum => financialMomentum;

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

        Debug.Log($"Momentum changed from {oldValue} → {financialMomentum}");
    }

    public void SetMomentum(float value)
    {
        financialMomentum = Mathf.Clamp(value, -100f, 100f);
    }
    public void ResetPlayerData()
    {
        financialMomentum = 0f;
        adults = 1;
        children = 0;
    }
    public void RemoveAdult()
    {
        if (adults > 1)
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