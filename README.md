# Chengetedzo: Financial Literacy Simulation Game

A Unity WebGL game set in Zimbabwe that teaches financial literacy through lived experience. Players run a household budget as one of three characters, an informal trader, a formal sector employee, or a smallholder farmer, across a simulated 12 month year.

---

## Overview

**Chengetedzo** (meaning "protection" in Shona) puts players inside the financial life of a Zimbabwean household. Each month they manage income and expenses, respond to unexpected life events, and choose whether to take out insurance, borrow money, or build savings. The goal is not to win, it is to understand.

The game is designed for financial education contexts and can be used by NGOs, financial institutions, schools, and community programmes. Target audience is ages 9 to 16.

**Platform:** WebGL, 1280x720, landscape. Distributed on itch.io and embedded on the project website.

---

## Gameplay Loop

Each month follows a fixed flow:

```
Forecast -> Insurance -> Loan -> Simulation -> Events -> Report -> Next Month
```

1. **Monthly News (Forecast)** sets headline signals hinting at which risks are elevated for the coming month
2. **Insurance Selection** lets players choose which plans to carry based on their assets and the forecast
3. **Loan Panel** *(unlocked after consistent saving)* offers borrowing and repayment rate adjustment
4. **Simulation** runs income in, expenses out, events fire
5. **Event Popups** deliver unexpected events, some with choice mechanics
6. **Monthly Report** gives the full financial breakdown with bar charts
7. **Year End Review** summarises insurance value, resilience metrics, and a mentor reflection

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

127 event assets across seven pools:

| Pool | Count |
|---|---|
| Choice events | 37 |
| Health | 19 |
| Agriculture | 18 |
| Economic | 15 |
| Weather | 14 |
| Opportunity | 13 |
| Crime | 10 |

- Weighted probability with seasonal filters and asset requirements
- Event chains: major events can trigger follow up events in later months
- Event pressure: pressure builds each month without events, raising the likelihood of one firing
- Choice events: players pick from two or three responses with different financial and momentum outcomes
- Events are authored in CSV and imported to ScriptableObjects. Two importers exist, one for flat events and one for the EVENT/CHOICE row format

### Choice Event Presentation

Choice events arrive as messages on a phone screen from named senders. 25 distinct senders appear across the 37 choice events, deliberately consolidated from an earlier 36 so that characters recur rather than every message coming from a stranger.

Senders the player would plausibly have in their contacts get a portrait. Senders they would not, such as a vet officer or a shop, get a neutral silhouette. Organisations appear as names.

### Insurance System

Eight insurance types with waiting periods, deductibles, and eligibility requirements:

- Funeral Cover, Health Insurance, Education Rider, Hospital Cash Back
- Personal Accident Cover, Motor Insurance (3rd Party), Home Insurance, Agricultural Insurance

An event covered by more than one policy pays out from every applicable policy. Claims made inside a waiting period, or against a lapsed plan, are denied with an explanatory message rather than silently failing.

### Savings

Players set a monthly contribution on the savings panel, capped at what the current committed budget can sustain. The cap amortises school fees across the year rather than subtracting a full term bill in one month. The contribution can be changed at any point in the run, and a guard skips the contribution in months where cash on hand cannot cover it.

### Mentor System

A financial mentor delivers contextual guidance based on momentum zone changes, recovery from negative stretches, forced loan patterns, and mid year and year end reflections. Tutorial sequences use a transparent mentor overlay.

### Financial Momentum

A score from -100 to +100 tracking financial behaviour patterns. Affected by consistent saving and insurance payment, repeated over budget months, forced emergency loans, and recovery from low points.

### Loan System

Supports voluntary borrowing and forced emergency loans when cash goes negative. Missed repayments compound debt.

### Save System

Full game state persists across sessions via a JSON save file, including ledger history, income effects, insurance plan states, loan balance, and momentum.

### Visual Simulation

The simulation background lives in world space as layered SpriteRenderers, not on the Canvas. Sorting layers run Sky, Clouds, Skyline, Treeline. `SeasonalBackgroundManager` drives the layers by season and weather, `CloudSpawner` handles drifting clouds in summer while winter and rainy seasons use scrolling layers. `SkyFlybySpawner` adds occasional planes, helicopters and birds.

---

## Art

The mentor character and the simulation background were produced by **Bill Masuku**.

Character portraits, speech bubbles, event icons and phone art were produced by **Jean Roux**. Fifteen characters, each with a bust and a circular contact photo version.

Speech bubbles are nine sliced with deliberately asymmetric borders, wider on the tail side so the tail sits inside a corner segment and does not distort when the bubble grows to fit its text. The response side uses a mirrored sprite so the tail points the other way.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs              # Core game loop, phase management
│   │   ├── UIManager.cs                # Panel routing and popup management
│   │   ├── EventManager.cs             # Event generation and resolution
│   │   ├── InsuranceManager.cs         # Plans, premiums, and claims
│   │   ├── FinanceManager.cs           # Income, expenses, savings
│   │   ├── LoanManager.cs              # Borrowing and repayment
│   │   ├── ForecastManager.cs          # Monthly news generation
│   │   ├── TutorialManager.cs          # Guided tutorial sequences
│   │   ├── SettingsManager.cs          # Audio and mentor hint settings
│   │   ├── PlayerDataManager.cs        # Household size and momentum
│   │   ├── VisualSimulationManager.cs  # Weather overlays
│   │   └── AudioManager.cs
│   ├── Panel Controllers/
│   │   ├── SetupPanelController.cs
│   │   ├── BudgetPanelController.cs    # Savings contribution and withdrawal
│   │   ├── ExpensesPanelController.cs
│   │   ├── ForecastPanelController.cs
│   │   ├── InsurancePanel.cs
│   │   ├── LoanPanelController.cs
│   │   ├── GlossaryPanel.cs
│   │   └── SettingsPanelController.cs
│   ├── EventData.cs                    # ScriptableObject: event definition
│   ├── EventDatabase.cs                # ScriptableObject: event collection
│   ├── ResolvedEvent.cs                # Runtime event result
│   ├── ChoiceResultPopup.cs
│   ├── MonthlyFinancialLedger.cs       # Per month transaction tracking
│   ├── FinancialEntry.cs
│   ├── MonthlyReportPanel.cs           # Report UI population
│   ├── SaveSystem.cs                   # JSON persistence
│   ├── GameSaveData.cs                 # Save data schema
│   ├── PlayerSetupData.cs
│   ├── GoalDefs.cs                     # Savings goal definitions
│   ├── MentorLines.cs                  # Mentor dialogue strings
│   ├── ForecastLines.cs                # Forecast headline strings
│   ├── FamilyLines.cs                  # Per profile family prompt voices
│   ├── SeasonalBackgroundManager.cs    # Season and weather layer driver
│   ├── CloudSpawner.cs
│   ├── CloudDrift.cs
│   ├── ScrollingLayer.cs
│   ├── WindScroller.cs
│   ├── SkyFlybySpawner.cs              # Planes, helicopters, birds
│   ├── UIAnimator.cs
│   ├── GameUtils.cs
│   ├── InsuranceToggle.cs
│   ├── BudgetPieChart.cs
│   ├── YearEndGraph.cs
│   └── MonthlyBarChart.cs
├── GameData/
│   └── Events/
│       ├── Agriculture/
│       ├── ChoiceEvents/
│       ├── Crime/
│       ├── Economic/
│       ├── Health/
│       ├── Opportunity/
│       ├── Weather/
│       ├── Events.csv                  # Flat event source
│       └── EventDatabase.asset
├── Sprites/
│   ├── Characters/                     # 15 characters, bust and round
│   ├── Jean Art/                       # Icons, phones, bubbles, city layers
│   └── Simulation Background - Seasons/
└── WebGLTemplates/
    └── Chengetedzo/                    # Custom loading screen
```

---

## Development Notes

### Phase Guards

`GameManager.CurrentPhase` controls which systems can apply money changes. All financial mutations go through `ApplyMoneyChange()` and are logged to the `MonthlyFinancialLedger`.

### Popup Architecture

A single `IsPopupActive` flag prevents concurrent popups. Events, choice prompts, and mentor messages queue through the UIManager. The transparent mentor overlay used by tutorial sequences calls `ShowMentorMessageTransparent()` and sets `mentorSpokeThisMonth = true` so `EvaluateMentor()` cannot fire at the same time.

There are two distinct event popups. `EventPopup` is the card with a header image, used for events with no sender. `EventChoicePopup` is the phone chat, used for choice events and for insurance claim denial messages via `ShowMessagePopup`.

### Canvas Layering

`PopUpLayer` must be the last child in the Canvas hierarchy to render above all panels. The simulation background is no longer on the Canvas, it is world space geometry, so sorting layers rather than hierarchy order govern its ordering.

### Headless Simulation

`GameManager.IsHeadlessSimulation` enables automated stress test runs via the Unity Editor context menu. Profiles: Informal, Formal, Farmer, ZW Low Class, ZW Middle Class, ZW High Class.

### Gotchas worth knowing before you change anything

- **Serialized values do not update when code defaults change.** The scene keeps whatever was saved. `GameManager.totalMonths` has a code default of 24 while the shipped scene runs 12, so a freshly added GameManager would not match the shipped build.
- **Sorting layer always beats Z position.** The camera is orthographic, so Z gives no parallax and no size falloff. Z is organisational only.
- Everything in the project uses **100 Pixels Per Unit**.
- **Nine slicing requires Mesh Type set to Full Rect.** It defaults to Tight, where sliced rendering misbehaves silently.
- Several sprites are imported as **Sprite Mode: Multiple**, so the assignable asset is the child sprite such as `speech_bubble_001_0`, not the parent texture. Dragging the parent onto an Image field silently does nothing.
- A **Content Size Fitter on a child of a layout group** fights that group. Unity flags this in the Inspector and it should be heeded.
- Cloud templates must be **inactive** in the scene. Active ones self destruct via `CloudDrift` on the first frame.
- `ChoiceEvents.csv`'s header comment is wrong about CHOICE columns 9 and 10. They are `affectsLoan` and `borrowingPowerChange`, not `startsChain` and `followUpDelay`. Choice rows cannot chain, so delayed payoffs are proxied as income percentage effects.

### Writing copy

Choice event descriptions are capped at 150 characters and result strings at roughly 100, so that a long message plus a response plus two choice buttons still fits at 720p.

---

## Building

WebGL, 1280x720.

- **Player Settings, Resolution and Presentation, WebGL Template** must be set to `Chengetedzo`, not `Default`. The Default template shows Unity's loading bar.
- **Publishing Settings, Decompression Fallback** should be enabled. itch.io handles Gzip, but a self hosted embed generally will not send the right content encoding header, and the build fails to load without the fallback.
- The Unity splash screen is already disabled in Player Settings.

---

## Built With

- **Unity 6** (6000.0.60f1), WebGL target
- **C#**
- **TextMeshPro**

---

## Credits

- Design and programming: Baraka
- Mentor character and simulation background: Bill Masuku
- Character portraits, speech bubbles, icons and phone art: Jean Roux
- Published by AUBiK Business Solutions

---

## License

This project is proprietary. All rights reserved.
