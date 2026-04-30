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

    //  HEALTH
    public static readonly ForecastLine[] Health =
    {
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Clinics Report Rise in Seasonal Illnesses among Youth",
            "Local health facilities are seeing an increase in children with flu-like symptoms."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Respiratory Infections More Common This Month",
            "Health workers advise households to manage damp or cold conditions to prevent flu complications."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Waterborne Illness Risk Elevated",
            "Public health officials advise boiling drinking water following recent rainfall and drainage disruptions."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Diarrhoeal Cases Rising in Several Areas",
            "Community health workers urge proper food storage and handwashing as cases of diarrhoea increase."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Skin Ailments Increase in Dry Conditions",
            "Dermatologists note a rise in skin irritation and infections linked to dry, dusty weather."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Allergy Season Underway",
            "Medical professionals advise families to keep living spaces dust-free to manage rising seasonal allergies."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Health Officials Warn of Strain on Hospitals",
            "Medical professionals caution that hospital admissions are rising due to seasonal illness."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Doctors Urge Preventative Care as Illness Spreads",
            "Preventative measures are being encouraged as more cases of illness appear across communities."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Healthcare Services Under Pressure",
            "Increased demand for medical care may affect household finances through higher consultation fees."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Influenza Outbreaks Reported in Schools",
            "Public health officials advise vaccination and hygiene practices as influenza spreads through school settings."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Medication Costs Rising Amid Seasonal Demand",
            "Pharmacies report shortages of common medications as demand spikes this month."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Food Poisoning Risk Elevated",
            "Health officials advise proper food storage and hygiene as heat accelerates spoilage."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Rise in Emergency Admissions Raises Concern",
            "Hospitals report more emergency cases than usual this month. Uninsured households face significant out-of-pocket costs."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Cholera Alert Issued for Affected Districts",
            "Authorities warn of cholera risk following flooding. Communities are urged to access clean water and seek immediate treatment."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Heat Exhaustion Cases Surge",
            "Medical professionals report a spike in heat-related illness. Staying hydrated and avoiding outdoor work at peak hours is strongly advised."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Public Health Emergency Declared in Two Provinces",
            "Authorities have raised the alert level after a surge in serious illness. Medical costs are expected to rise significantly."
        ),
    };

    //  LIVESTOCK
    public static readonly ForecastLine[] Livestock =
    {
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Farmers Alerted to Tick-Borne Disease Risk",
            "Veterinary authorities advise farmers to begin dipping programs and monitor for early signs of tick-borne illness."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Livestock Health Under Watch",
            "Veterinary services are monitoring reports of sickness in livestock. Routine checks are recommended."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Vaccination Campaigns Underway",
            "Farmers are advised to complete primary vaccinations to protect young stock ahead of the wet season."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Animal Exhibition Health Checks Advised",
            "Veterinary authorities advise stringent health checks for any livestock participating in public shows this month."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Supplementary Feeding Recommended",
            "Farmers are advised to introduce high-energy feed to prevent herd condition loss during lean grazing months."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Mild,
            "Parasite Risk Elevated in Wet Conditions",
            "Wet fields are accelerating parasite spread. Routine deworming and dipping are strongly advised this month."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Veterinary Officials Warn of Livestock Disease Risk",
            "Early signs of illness among animals have been detected in nearby areas. Farmers are advised to isolate sick stock immediately."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Concerns Grow Over Cattle Disease Spread",
            "Experts warn that animal diseases can spread quickly if precautions are not taken. Report unusual symptoms to the vet."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Movement Restrictions Implemented on Livestock",
            "Authorities advise against unnecessary stock movement this month to help contain ongoing disease outbreaks."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Feed Contamination Concerns Raised",
            "Reports of contaminated feed batches are circulating. Farmers are urged to verify suppliers and inspect deliveries carefully."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Water Contamination Affecting Livestock",
            "Polluted water sources are creating health risks for herds. Farmers are urged to provide clean, tested water."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Warning,
            "Vet Services Report Unusual Mortality in Herds",
            "Unexplained livestock deaths have been reported in several districts. Farmers should contact vet services immediately if they observe similar signs."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Farmers Warned of Potential Herd Losses",
            "Veterinary officials stress urgent preventative action as disease risk reaches its highest level of the season."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Foot and Mouth Disease Detected Nearby",
            "Authorities have confirmed cases in adjacent areas. Movement bans may follow. Farmers without livestock insurance face significant exposure."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Severe Herd Stress Reported",
            "Extreme conditions have left livestock underfed and weakened. Farmers are advised to limit animal activity and prioritize water supply."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Disease,
            ForecastIntensity.Severe,
            "Mass Livestock Mortality Risk Elevated",
            "A combination of disease pressure and poor pasture conditions has created critical risk. Culling unproductive animals early may limit financial damage."
        ),
    };

    //  CROPS
    public static readonly ForecastLine[] Crops =
    {
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Mild,
            "Agricultural Experts Warn of Poor Growing Conditions",
            "Changing weather patterns may affect crop yields in the coming months. Early preparation is advised."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Mild,
            "Soil Moisture Levels Below Average",
            "Dry conditions are reducing soil moisture. Farmers are advised to consider conservation tillage to retain water."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Mild,
            "Irrigation Management Flagged as Critical",
            "Agricultural experts advise careful water use this month to support crop maturation in dry conditions."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Mild,
            "Input Purchasing Window Opening",
            "Financial advisors suggest setting aside budgets now for seeds and fertilizer ahead of the planting season."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Mild,
            "Post-Harvest Storage Advice Issued",
            "Agricultural extension workers advise finalising drying floors and verifying storage hygiene for the upcoming grain harvest."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Mild,
            "Wheat Crop Water Stress Monitoring Advised",
            "Agricultural experts advise continuous monitoring of winter crops for signs of water stress as dry conditions persist."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Farmers Advised to Prepare for Crop Challenges",
            "Authorities caution that this season may bring increased agricultural risks. Crop insurance is worth reviewing."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Concerns Rise Over Crop Health",
            "Reports suggest crops may be vulnerable to pests and environmental stress. Early intervention is recommended."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Seed Quality Failures Reported This Season",
            "Some farmers have reported poor germination rates. Agricultural officers urge sourcing certified seed only."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Fertilizer Prices Climbing",
            "Input suppliers warn of supply constraints pushing fertilizer prices upward. Advance purchasing could protect margins."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Pest Pressure Expected to Increase",
            "Agricultural extension warns that dry conditions often push pests toward remaining green crops. Early monitoring is essential."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Land Preparation Delays Could Affect Yields",
            "Officers advise starting soil preparation early this month to avoid rushed planting that reduces germination success."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Crop Yields Face Serious Threats",
            "Early indicators suggest farmers should prepare for possible significant losses this season."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Drought Conditions Worsening",
            "Prolonged dry spells are severely affecting crop viability. Farmers without savings or insurance face real financial danger."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Emergency Water Purchase May Be Unavoidable",
            "Water scarcity is at critical levels in several farming areas. Budgeting for emergency water procurement is strongly advised."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Early Growth Threatened by Pests in Wet Fields",
            "Agricultural experts advise diligent weeding to prevent rapid pest spread in waterlogged fields."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Fertilizer Application Window is Now",
            "Agricultural officers advise precise application during the growth phase while soil moisture remains adequate."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Crop Storage Mould Risk Rising",
            "High humidity is increasing the risk of mould in stored grain. Farmers advised to check ventilation in storage facilities."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Planting Season Begins",
            "Agricultural extension workers advise beginning planting immediately upon adequate moisture receipt for best germination results."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Waterlogged Fields Delaying Planting",
            "Excess rainfall is preventing normal field access. Farmers are advised to prioritize drainage before planting."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Flooding Risk to Planted Crops",
            "Meteorologists warn of continued heavy rains. Low-lying fields are at high risk of crop loss from flooding."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Soil Erosion Threat After Heavy Rains",
            "Intense rainfall events are stripping topsoil. Farmers with exposed fields are advised to implement erosion controls."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Crop Disease Pressure High in Humid Conditions",
            "High humidity is creating ideal conditions for fungal crop diseases. Preventative treatment is advised where affordable."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Severe,
            "Flash Flood Warning for Low-Lying Farmland",
            "Severe rainfall is expected to cause flash flooding. Crops in flood-prone areas face near-total loss without drainage infrastructure."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Severe,
            "Hail Damage Expected in Some Districts",
            "Meteorological services have issued a hail warning. Crops at the heading stage are at highest risk of irreversible damage."
        ),
    };

    //  ECONOMIC
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
            ForecastIntensity.Mild,
            "Transport Fares Expected to Rise",
            "Kombi and ZUPCO operators warn of price increases following rising fuel costs. Budget for higher commuting expenses."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Mild,
            "School Levy Notices Being Sent Home",
            "Parents are reporting unexpected school fee demands. Setting aside a school buffer is advised ahead of each term."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Mild,
            "Grocery Price Increases Reported",
            "Supermarkets have adjusted prices upward on staple goods. Bulk purchasing while prices are stable may save money."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Mild,
            "Utility Bills Set to Increase",
            "ZESA and water authorities have signalled tariff adjustments. Households are advised to review monthly utility budgets."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Mild,
            "Informal Sector Activity Slows",
            "Market traders report reduced buyer activity this month. Informal workers are advised to build short-term cash buffers."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Companies Signal Possible Cost-Cutting Measures",
            "Employers hint at restructuring that could affect workers through reduced hours or delayed salaries."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Rising Prices Expected Across Key Goods",
            "Consumers are advised to plan for increased living costs as supply disruptions push prices upward."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Household Budgets May Face Pressure",
            "Experts suggest families prepare for tighter financial conditions over the coming weeks."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Fuel Price Surge Affecting All Sectors",
            "Rising fuel costs are flowing through into transport, food, and utility prices simultaneously — squeezing household budgets from every direction."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Currency Pressure Raising Import Costs",
            "Currency weakness is raising the price of imported goods even when local incomes remain flat. Groceries and medication are most affected."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Loan Conditions Tightening",
            "Banks are reporting stricter lending criteria this month. Households relying on credit for emergencies should review their options."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Warning,
            "Market Prices Volatile for Agricultural Goods",
            "Traders are reporting unpredictable price swings for maize, vegetables, and livestock. Selling decisions should be made carefully."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Severe,
            "Job Security Concerns Growing",
            "Economic analysts warn of potential instability in employment. Workers in informal and contract roles are most exposed."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Severe,
            "Inflation Accelerating Faster Than Expected",
            "Price increases are outpacing household income adjustments. Emergency savings are increasingly critical this month."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Severe,
            "Export Restrictions Disrupting Agricultural Markets",
            "Government policy has blocked access to key export channels. Farmers and traders dependent on a single market are most at risk."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.EconomicStress,
            ForecastIntensity.Severe,
            "Credit Markets Seizing Up",
            "Access to borrowing has tightened sharply. Households without savings have limited fallback options if an unexpected cost hits."
        ),
    };

    //  CRIME
    public static readonly ForecastLine[] Crime =
    {
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "Police Warn of Rising Property Crime",
            "Authorities advise residents to secure gates and fences as opportunistic theft increases."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "Street Crime Alert Issued",
            "Police advise pedestrians to remain alert and avoid displaying cash or valuables in public."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "Heating Material Theft on the Rise",
            "Police advise homeowners to secure firewood and coal supplies against pilferage during winter months."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "School District Theft Alert",
            "Police advise parents and students to secure bags and valuables during the school term."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "Opportunistic Theft Near Harvest Areas",
            "Security services advise farmers to monitor early harvest stores and improve lighting around storage areas."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "Public Event Theft Warning",
            "Police advise visitors to major public events and markets to keep wallets and phones secured."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Mild,
            "Unoccupied Home Theft Warning",
            "Police advise neighbours to look out for each other's properties during school holiday periods."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Increase in Break-Ins Reported",
            "Several neighbourhoods are reporting higher burglary incidents. Improving access security is strongly recommended."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Residents Advised to Secure Valuables",
            "Police urge households to store cash in mobile money or bank accounts rather than keeping it at home."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Crime Watch Alerts Issued Across Districts",
            "Law enforcement warns of increased criminal activity. Community watch groups are being encouraged to activate."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Market Robbery Incidents Rising",
            "Police report a rise in cash theft at markets. Traders are advised to use mobile money and limit visible cash handling."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Vehicle and Equipment Theft Increasing",
            "Farmers and tradespeople are advised to secure equipment overnight and consider additional locks on vehicles."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Retail Crime Peaks as Market Activity Rises",
            "Police advise businesses and shoppers to heighten security measures as commercial activity increases this month."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Warning,
            "Festive Season Theft Warning",
            "Police advise vigilance in urban centres and crowded transit hubs as festive activity increases theft risk."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Severe,
            "Livestock Theft Hotspots Identified",
            "Police have identified areas with elevated livestock theft activity. Farmers are urged to pen animals securely overnight."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Severe,
            "Armed Robbery Incidents Reported",
            "Authorities report an increase in armed incidents at businesses and markets. Avoid carrying large amounts of cash."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Severe,
            "Fraud and Scam Activity Surging",
            "Police warn of a wave of financial scams targeting households. Offers that require urgent action or upfront payment should be treated with extreme caution."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.CrimeWave,
            ForecastIntensity.Severe,
            "Transport Hijacking Risk Elevated",
            "Goods are being targeted during transport to market. Farmers and traders are advised to travel in groups and vary routes."
        ),
    };

    //  WEATHER
    public static readonly ForecastLine[] Weather =
    {

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Heavy Rains Expected Next Month",
            "Weather experts warn of possible localised flooding. Clearing storm drains now is advised."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "High Humidity and Persistent Rains Forecast",
            "Residents are advised to clear gutters and check roof drainage before rains intensify."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Early Rains Signal Start of Wet Season",
            "Residents advised to check roofs and clear drains before the heavy rains arrive."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Mild,
            "Rainfall Bringing Relief But Also Risk",
            "While rains ease water shortages, flood-prone areas should prepare for waterlogging and road damage."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Storm Warnings Issued for Several Areas",
            "Authorities advise preparation for severe storms. Secure loose property and avoid low-lying areas."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Peak Summer Storms Expected",
            "Authorities advise monitoring weather reports daily for flash flood risks over the coming weeks."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Flood Risk Increasing",
            "Communities in low-lying areas are advised to move valuables to higher ground and prepare for possible displacement."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Lightning Risk Elevated",
            "Meteorological services warn of increased lightning activity. Avoid open fields and unprotected structures during storms."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Warning,
            "Road Damage Expected After Heavy Rainfall",
            "Transport authorities warn that road conditions will deteriorate. Allow extra travel time and watch for potholes and washed-out sections."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Severe,
            "Flash Flood Warning Issued",
            "Communities advised to plan for serious damage. Property insurance should be reviewed immediately if you own a home."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Severe,
            "Cyclone-Level Rains Approaching",
            "Authorities have raised the weather alert to its highest level. Residents in exposed areas should move to safer shelter."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Wet,
            ForecastIntensity.Severe,
            "Severe Flooding Affecting Multiple Districts",
            "Infrastructure damage is widespread. Expect disruptions to transport, markets, and utility supply for several weeks."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Dry Conditions Raise Concern",
            "Experts warn of prolonged dry spells affecting farming, water supply, and household livelihoods."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Water Scarcity Risk Increasing",
            "Reservoirs are dropping below seasonal norms. Households are advised to manage water use carefully."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Wildfire Risk Elevated in Dry Conditions",
            "Authorities advise strict fire safety practices. Avoid burning trash or dry debris near homes or farmland."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Warning,
            "Temperatures Rising Ahead of Peak Heat Period",
            "Households are advised to prepare water storage and ventilation ahead of the hottest months."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Severe Drought Conditions Expected",
            "Water shortages may seriously harm farming and livestock. Households without savings are most at risk."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Extreme Heat Warning Issued",
            "Authorities warn of extreme heat affecting farms, homes, and workers. Outdoor activity should be minimised during peak hours."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Dry,
            ForecastIntensity.Severe,
            "Drought Emergency Declared in Affected Provinces",
            "Food and water insecurity is now at critical levels. Households are urged to prioritise emergency reserves."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Warning,
            "Extreme Temperatures Expected",
            "Weather services caution residents to prepare for harsh heat. Stay hydrated and limit exposure during peak sun hours."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Warning,
            "Dehydration Risk Alert",
            "Public health officials advise increasing water intake to prevent heat-related fatigue and illness."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Warning,
            "Windy and Dusty Conditions Forecast",
            "Authorities advise securing loose property and minimising outdoor exposure during dust storms this month."
        ),

        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Severe,
            "Heatwave Conditions Forecast",
            "Authorities warn of extreme heat affecting farms and homes. Ensure livestock and vulnerable family members have access to shade and water."
        ),
        new ForecastLine(
            ForecastManager.ForecastSignal.Heat,
            ForecastIntensity.Severe,
            "Peak Heat Season Begins",
            "Authorities advise strict fire safety in arid conditions. Power outages may increase as cooling demand surges."
        ),
    };
}