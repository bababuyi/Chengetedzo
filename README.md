# Chengetedzo — Financial Literacy Simulation Game

A Unity mobile game set in Zimbabwe that teaches financial literacy through lived experience. Players navigate real financial decisions as one of three characters — an informal trader, a formal sector employee, or a smallholder farmer — across a simulated 24-month period.

---

## Overview

**Chengetedzo** (meaning "protection" in Shona) puts players inside the financial life of a Zimbabwean household. Each month, they manage income and expenses, respond to unexpected life events, and choose whether to take out insurance, borrow money, or build savings. The goal is not to win — it is to understand.

The game is designed for financial education contexts and can be used by NGOs, financial institutions, schools, and community programmes.

---

## Gameplay Loop

Each month follows a fixed flow:

```
Forecast → Insurance → Loan → Simulation → Events → Report → Next Month
```

1. **Monthly News (Forecast)** — Headline signals hint at what risks are elevated for the coming month
2. **Insurance Selection** — Players choose which plans to carry based on their assets and the forecast
3. **Loan Panel** *(unlocked after consistent saving)* — Borrow or adjust repayment rates
4. **Simulation** — Income arrives, expenses leave, events fire
5. **Event Popups** — Unexpected events appear with optional choice mechanics
6. **Monthly Report** — Full financial breakdown with bar charts
7. **Year-End Review** — Summary of insurance value, resilience metrics, and mentor reflection

---

## Playable Profiles

| Profile | Character | Context |
|---|---|---|
| **Informal Worker** | Tendai | Market trader in Mbare, Harare. Variable income, renting, no assets |
| **Formal Worker** | Chido | Accounts clerk at a logistics firm. Stable salary, owns a car |
| **Farmer** | Sekuru Moyo | Smallholder in Mashonaland. Owns land and livestock, seasonal income |

Players may also choose **Free Mode** to configure their own financial profile from scratch.

---

## Key Systems

### Event System
- 100+ events across categories: Health, Weather, Agriculture, Economic, Crime, Opportunity, Choice
- Weighted probability with seasonal filters and asset requirements
- Event chains: major events can trigger follow-up events in subsequent months
- Event pressure system: pressure builds each month without events, increasing likelihood
- Choice events: players pick from 2–3 responses with different financial and momentum outcomes

### Insurance System
Eight insurance types with waiting periods, deductibles, and eligibility requirements:
- Funeral Cover, Health Insurance, Education Rider, Hospital Cash Back
- Personal Accident Cover, Motor Insurance (3rd Party), Home Insurance, Agricultural Insurance

### Mentor System
A financial mentor (Chido's backstory framing) delivers contextual guidance based on:
- Momentum zone changes
- Recovery from negative stretches
- Forced loan patterns
- Mid-year and year-end reflections

### Financial Momentum
A score from -100 to +100 tracking financial behaviour patterns. Affected by:
- Consistent saving and insurance payment
- Repeated over-budget months
- Forced emergency loans
- Recovery from low points

### Loan System
Unlocked after sustained savings contributions. Supports voluntary borrowing and forced emergency loans when cash goes negative. Missed repayments compound debt.

### Save System
Full game state persists across sessions via JSON save file. Includes ledger history, income effects, insurance plan states, loan balance, and momentum.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs          # Core game loop, phase management
│   │   ├── UIManager.cs            # Panel routing and popup management
│   │   ├── EventManager.cs         # Event generation and resolution
│   │   ├── InsuranceManager.cs     # Plans, premiums, and claims
│   │   ├── FinanceManager.cs       # Income, expenses, savings
│   │   ├── LoanManager.cs          # Borrowing and repayment
│   │   ├── ForecastManager.cs      # Monthly news generation
│   │   ├── TutorialManager.cs      # Guided tutorial sequences
│   │   ├── SettingsManager.cs      # Audio and mentor hint settings
│   │   ├── PlayerDataManager.cs    # Household size and momentum
│   │   └── AudioManager.cs
│   ├── Panel Controllers/
│   │   ├── SetupPanelController.cs
│   │   ├── BudgetPanelController.cs
│   │   ├── ForecastPanelController.cs
│   │   ├── InsurancePanel.cs
│   │   ├── LoanPanelController.cs
│   │   └── SettingsPanelController.cs
│   ├── GameManager.cs              # (see Managers)
│   ├── EventData.cs                # ScriptableObject: event definition
│   ├── EventDatabase.cs            # ScriptableObject: event collection
│   ├── ResolvedEvent.cs            # Runtime event result
│   ├── MonthlyFinancialLedger.cs   # Per-month transaction tracking
│   ├── MonthlyReportPanel.cs       # Report UI population
│   ├── SaveSystem.cs               # JSON persistence
│   ├── GameSaveData.cs             # Save data schema
│   ├── MentorLines.cs              # All mentor dialogue strings
│   ├── ForecastLines.cs            # All forecast headline strings
│   ├── BudgetPieChart.cs
│   ├── YearEndGraph.cs
│   └── MonthlyBarChart.cs
└── GameData/
    └── Events/
        ├── Health/
        ├── Weather/
        ├── Agriculture/
        ├── Economic/
        ├── Crime/
        ├── Opportunity/
        ├── Funeral/
        ├── Home/
        ├── Motor/
        ├── Education/
        └── ChoiceEvents/
```

---

## Development Notes

### Phase Guards
`GameManager.CurrentPhase` controls which systems can apply money changes. All financial mutations go through `ApplyMoneyChange()` and are logged to the `MonthlyFinancialLedger`.

### Popup Architecture
A single `IsPopupActive` flag prevents concurrent popups. Events, choice prompts, and mentor messages all queue through `ShowPopup()` / `CloseActivePopup()`. The transparent mentor overlay (tutorial sequences) uses `ShowMentorMessageTransparent()` and sets `mentorSpokeThisMonth = true` to block `EvaluateMentor()` from firing simultaneously.

### Canvas Layering
`PopUpLayer` must be the last child in the Canvas hierarchy to render above all panels. Profile selection and the main menu panels sit below it.

### Headless Simulation
`GameManager.IsHeadlessSimulation` enables automated stress-test runs via the Unity Editor context menu. Profiles: Informal, Formal, Farmer, ZW Low Class, ZW Middle Class, ZW High Class.

---

## Built With

- **Unity** (mobile target, portrait layout)
- **C#**

---

## License

This project is proprietary. All rights reserved.
