using UnityEngine;
using UnityEngine.UI;

public class InsuranceSelectionUI : MonoBehaviour
{
    public Toggle funeralToggle;
    public Toggle educationToggle;
    public Toggle groceryToggle;
    public Toggle hospitalToggle;
    public Toggle microMedicalToggle;

    private InsuranceManager insuranceManager;

    private void Start()
    {
        insuranceManager = FindFirstObjectByType<InsuranceManager>();

        funeralToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) insuranceManager.BuyInsurance(InsuranceManager.InsuranceType.Funeral);
            else insuranceManager.CancelInsurance(InsuranceManager.InsuranceType.Funeral);
        });

        educationToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) insuranceManager.BuyInsurance(InsuranceManager.InsuranceType.Education);
            else insuranceManager.CancelInsurance(InsuranceManager.InsuranceType.Education);
        });

        groceryToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) insuranceManager.BuyInsurance(InsuranceManager.InsuranceType.Grocery);
            else insuranceManager.CancelInsurance(InsuranceManager.InsuranceType.Grocery);
        });

        hospitalToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) insuranceManager.BuyInsurance(InsuranceManager.InsuranceType.Hospital);
            else insuranceManager.CancelInsurance(InsuranceManager.InsuranceType.Hospital);
        });

        microMedicalToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) insuranceManager.BuyInsurance(InsuranceManager.InsuranceType.MicroMedical);
            else insuranceManager.CancelInsurance(InsuranceManager.InsuranceType.MicroMedical);
        });
    }
}
