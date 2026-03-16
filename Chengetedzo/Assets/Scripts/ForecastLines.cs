public static class ForecastLines
{
    public enum ForecastIntensity
    {
        Mild,
        Warning,
        Severe
    }

    public struct ForecastLine
    {
        public ForecastManager.ForecastSignal signal;
        public ForecastIntensity intensity;
        public string headline;
        public string body;

        public ForecastLine(
            ForecastManager.ForecastSignal signal,
            ForecastIntensity intensity,
            string headline,
            string body)
        {
            this.signal = signal;
            this.intensity = intensity;
            this.headline = headline;
            this.body = body;
        }
    }

    public static readonly ForecastLine[] Health =
    {
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Clinics Report Rise in Seasonal Illnesses",
            "Local health facilities are seeing an increase in patients with flu-like symptoms."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Health Officials Warn of Strain on Hospitals",
            "Medical professionals caution that hospitals may experience higher admissions."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Doctors Urge Preventative Care as Illness Spreads",
            "Preventative measures are being encouraged as more cases of illness appear."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Rise in Emergency Admissions Raises Concern",
            "Hospitals report more emergency cases than usual this month."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Healthcare Services Under Pressure",
            "Increased demand for medical care may affect households financially."
        )
    };

    public static readonly ForecastLine[] Livestock =
{
    new ForecastLine(
        ForecastManager.ForecastSignal.Disease,
        ForecastIntensity.Warning,
        "Veterinary Officials Warn of Livestock Disease Risk",
        "Early signs of illness among animals have been detected in nearby areas."
        ),

    new ForecastLine(
        ForecastManager.ForecastSignal.Disease,
        ForecastIntensity.Mild,
        "Farmers Alerted to Possible Animal Health Issues",
        "Authorities advise farmers to remain cautious as animal health concerns rise."
        ),

    new ForecastLine(
        ForecastManager.ForecastSignal.Disease,
        ForecastIntensity.Mild,
        "Livestock Health Under Watch",
        "Veterinary services are monitoring reports of sickness in livestock populations."
        ),

    new ForecastLine(
        ForecastManager.ForecastSignal.Disease,
        ForecastIntensity.Warning,
        "Concerns Grow Over Cattle Disease Spread",
        "Experts warn that animal diseases can spread quickly if precautions are not taken."
        ),

    new ForecastLine(
        ForecastManager.ForecastSignal.Disease,
        ForecastIntensity.Severe,
        "Farmers Warned of Potential Herd Losses",
        "Veterinary officials stress the importance of preparation during this period."
        )
    };

    public static readonly ForecastLine[] Crops =
    {
        new ForecastLine(
        ForecastManager.ForecastSignal.Dry,
        ForecastIntensity.Mild,
        "Agricultural Experts Warn of Poor Growing Conditions",
        "Changing weather patterns may affect crop yields in the coming months."
        ),

        new ForecastLine(
        ForecastManager.ForecastSignal.Dry,
        ForecastIntensity.Warning,
        "Farmers Advised to Prepare for Crop Challenges",
        "Authorities caution that this season may bring increased agricultural risks."
        ),

        new ForecastLine(
        ForecastManager.ForecastSignal.Dry,
        ForecastIntensity.Warning,
        "Concerns Rise Over Crop Health",
        "Reports suggest crops may be vulnerable to pests and environmental stress."
        ),

        new ForecastLine(
        ForecastManager.ForecastSignal.Dry,
        ForecastIntensity.Mild,
        "Uncertain Conditions Ahead for Farmers",
        "Experts warn that yields may fluctuate due to seasonal factors."
        ),

    new ForecastLine(
        ForecastManager.ForecastSignal.Dry,
        ForecastIntensity.Severe,
        "Crop Yields Face Potential Threats",
        "Early indicators suggest farmers should prepare for possible losses."
        )
    };

    public static readonly ForecastLine[] Economic =
{
    new ForecastLine(
        ForecastManager.ForecastSignal.EconomicStress,
        ForecastIntensity.Mild,
        "Economic Uncertainty Looms Over Households",
        "Analysts warn that financial stability may be tested in the coming months."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.EconomicStress,
        ForecastIntensity.Warning,
        "Companies Signal Possible Cost-Cutting Measures",
        "Employers hint at restructuring that could affect workers."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.EconomicStress,
        ForecastIntensity.Warning,
        "Rising Prices Expected Across Key Goods",
        "Consumers are advised to plan for increased living costs."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.EconomicStress,
        ForecastIntensity.Severe,
        "Job Security Concerns Grow",
        "Economic analysts warn of potential instability in employment."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.EconomicStress,
        ForecastIntensity.Warning,
        "Household Budgets May Face Pressure",
        "Experts suggest families prepare for tighter financial conditions."
    )
};

    public static readonly ForecastLine[] Crime =
{
    new ForecastLine(
        ForecastManager.ForecastSignal.CrimeWave,
        ForecastIntensity.Mild,
        "Police Warn of Rising Property Crime",
        "Authorities advise residents to remain vigilant."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.CrimeWave,
        ForecastIntensity.Warning,
        "Increase in Break-Ins Reported",
        "Several neighborhoods report higher burglary incidents."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.CrimeWave,
        ForecastIntensity.Warning,
        "Residents Advised to Secure Valuables",
        "Police urge households to take precautions."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.CrimeWave,
        ForecastIntensity.Warning,
        "Crime Watch Alerts Issued",
        "Law enforcement warns of increased criminal activity."
    ),

    new ForecastLine(
        ForecastManager.ForecastSignal.CrimeWave,
        ForecastIntensity.Severe,
        "Burglary Hotspots Identified",
        "Police advise residents in affected areas to prepare."
    )
};

    public static readonly ForecastLine[] Weather =
{
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Heavy Rains Expected Next Month",
            "Weather experts warn of possible localized flooding."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Storm Warnings Issued for Several Areas",
            "Authorities advise preparation for severe storms."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Severe,
            "Flood Risk Increases with Incoming Rains",
            "Communities advised to plan for possible damage."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Dry Conditions Raise Concern",
            "Experts warn of prolonged dry spells affecting livelihoods."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Severe Drought Conditions Expected",
            "Water shortages may seriously harm farming and livestock."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Warning,
            "Extreme Temperatures Expected",
            "Weather services caution residents to prepare for harsh heat."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Severe,
            "Heatwave Conditions Forecasted",
            "Authorities warn of extreme heat affecting farms and homes."
        )
    };
}