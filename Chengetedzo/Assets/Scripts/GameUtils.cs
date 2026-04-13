using UnityEngine;

public class GameUtils : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static string FormatMoney(float amount) => $"${amount:N0}";
}
