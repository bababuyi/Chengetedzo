using UnityEngine;

// Personal goal ("the dream") definitions — one per guided profile, plus a
// Free Mode pool. Static data + a benefit action per goal. GameManager owns
// the runtime state (GoalBuilt, freeGoalIndex, etc.); this class only knows
// what a goal is called, what it costs, and what it does when built.
public static class GoalDefs
{
    public class GoalDef
    {
        public string title;
        public float target;
        public System.Action Apply;
    }

    public static readonly GoalDef Informal = new GoalDef
    {
        title = "Her own market stall",
        target = 500f,
        Apply = () => GameManager.Instance.ApplyIncomeEffect(8f, -1)
    };

    public static readonly GoalDef Formal = new GoalDef
    {
        title = "Weekend delivery side-business",
        target = 700f,
        Apply = () => GameManager.Instance.ApplyIncomeEffect(7f, -1)
    };

    public static readonly GoalDef Farmer = new GoalDef
    {
        title = "A second cow",
        target = 450f,
        Apply = () =>
        {
            GameManager.Instance.ApplyIncomeEffect(6f, -1);
            var fm = GameManager.Instance.financeManager;
            if (fm != null)
                fm.livestockInsuredValue += 450f;
        }
    };

    public class FreeGoalDef
    {
        public string title;
        public System.Action Apply;
    }

    // target is computed at Free Mode setup confirm (see GameManager.RollFreeGoalIfNeeded)
    public static readonly FreeGoalDef[] FreeModePool =
    {
        new FreeGoalDef
        {
            title = "A sewing machine business",
            Apply = () => GameManager.Instance.ApplyIncomeEffect(6f, -1)
        },
        new FreeGoalDef
        {
            title = "A bicycle for the family",
            Apply = () =>
            {
                var gm = GameManager.Instance;
                float baseline = gm.financeManager != null ? gm.financeManager.transport : 0f;
                float cut = baseline * 0.25f;
                if (cut > 0f)
                    gm.ApplyExpenseEffect(ExpenseCategory.Transport, -cut, -1);
            }
        },
        new FreeGoalDef
        {
            // No mechanical benefit — the lesson IS the benefit.
            title = "A proper emergency fund",
            Apply = () => PlayerDataManager.Instance.ModifyMomentum(2f)
        }
    };

    public static string GetActiveGoalTitle()
    {
        var gm = GameManager.Instance;
        if (gm == null) return "";

        if (gm.IsGuidedMode)
        {
            return gm.CurrentProfileType switch
            {
                GameManager.ProfileType.Informal => Informal.title,
                GameManager.ProfileType.Formal => Formal.title,
                GameManager.ProfileType.Farmer => Farmer.title,
                _ => ""
            };
        }

        int idx = gm.FreeGoalIndex;
        if (idx < 0 || idx >= FreeModePool.Length) return "";
        return FreeModePool[idx].title;
    }

    public static float GetActiveGoalTarget()
    {
        var gm = GameManager.Instance;
        if (gm == null) return 0f;

        if (gm.IsGuidedMode)
        {
            return gm.CurrentProfileType switch
            {
                GameManager.ProfileType.Informal => Informal.target,
                GameManager.ProfileType.Formal => Formal.target,
                GameManager.ProfileType.Farmer => Farmer.target,
                _ => 0f
            };
        }

        return gm.FreeGoalTarget;
    }

    public static void ApplyActiveGoalBenefit()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.IsGuidedMode)
        {
            switch (gm.CurrentProfileType)
            {
                case GameManager.ProfileType.Informal: Informal.Apply(); break;
                case GameManager.ProfileType.Formal: Formal.Apply(); break;
                case GameManager.ProfileType.Farmer: Farmer.Apply(); break;
            }
            return;
        }

        int idx = gm.FreeGoalIndex;
        if (idx >= 0 && idx < FreeModePool.Length)
            FreeModePool[idx].Apply?.Invoke();
    }
}
