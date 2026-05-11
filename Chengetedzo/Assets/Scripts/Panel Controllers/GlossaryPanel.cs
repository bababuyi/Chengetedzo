using UnityEngine;
using TMPro;

public class GlossaryPanel : MonoBehaviour
{
    [Header("References")]
    public Transform contentParent;
    public GameObject entryPrefab;

    private static readonly (string term, string definition)[] Entries =
    {
        ("Premium",
         "The regular payment you make to keep your insurance active. Think of it as a monthly subscription."),

        ("Claim",
         "When you ask your insurer to pay out after something goes wrong."),

        ("Payout",
         "The money the insurer gives you when a claim is approved."),

        ("Waiting Period",
         "The months you must pay premiums before you can make a claim. Subscribing early means you are protected sooner."),

        ("Lapsed Policy",
         "Insurance that has been cancelled because premiums were missed. A lapsed policy offers no cover — even if you paid for months before."),

        ("Deductible",
         "The portion of a loss you pay yourself before insurance covers the rest. For example: if your deductible is $500 and your claim is $2,000, you pay $500 and insurance covers $1,500."),

        ("Coverage Limit",
         "The maximum amount your insurer will pay for a single claim. Losses above this limit come out of your own pocket."),

        ("Third-Party Motor",
         "Insurance that covers damage you cause to others in a road accident — not your own vehicle. Required by law in Zimbabwe."),

        ("Funeral Cover",
         "Insurance that pays out when a family member dies, to help cover burial costs. Waiting periods apply."),

        ("Hospital Cash Back",
         "A fixed daily payment for every day a family member spends in hospital, up to a set number of days per year."),

        ("Personal Accident Cover",
         "A lump sum paid if the breadwinner dies or is permanently disabled in an accident. Gives the family time to recover financially."),

        ("Education Rider",
         "Insurance that continues paying school fees if a parent dies, so children can stay in school."),

        ("Agricultural Insurance",
         "Cover for farmers against crop failure or livestock losses from disease, drought, or bad weather."),

        ("Home Insurance",
         "Covers the cost of repairing or rebuilding your home after damage from fire, flooding, or storms."),

        ("Beneficiary",
         "The person who receives the insurance payout — usually a family member named when you take out the policy."),

        ("Insured Value",
         "The amount your asset is covered for. For a home this is usually its replacement cost, not its market price."),

        ("Savings",
         "Money set aside regularly so you have a buffer for unexpected costs. Your first line of defence before insurance."),

        ("Emergency Fund",
         "A savings reserve kept specifically for unexpected expenses — medical bills, job loss, or urgent repairs."),

        ("Borrowing Power",
         "In this game: the maximum amount you are allowed to borrow based on your savings history."),

        ("Financial Momentum",
         "In this game: a measure of how well your financial habits are holding up. Positive habits raise it; crises and forced loans lower it."),
    };

    private void OnEnable()
    {
        PopulateIfEmpty();
    }

    private bool _populated = false;

    private void PopulateIfEmpty()
    {
        if (_populated) return;
        _populated = true;

        if (contentParent == null || entryPrefab == null)
        {
            Debug.LogError("[Glossary] contentParent or entryPrefab not assigned.");
            return;
        }

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var (term, definition) in Entries)
        {
            GameObject row = Instantiate(entryPrefab, contentParent);

            TMP_Text termText = row.transform.Find("TermText")?.GetComponent<TMP_Text>();
            TMP_Text defText = row.transform.Find("DefinitionText")?.GetComponent<TMP_Text>();

            if (termText != null) termText.text = term;
            if (defText != null) defText.text = definition;
        }
    }
}