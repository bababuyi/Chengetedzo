using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceResultPopup : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public Button closeButton;

    private System.Action _onClose;

    public void Show(string result, System.Action onClose)
    {
        resultText.text = result;
        _onClose = onClose;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() =>
        {
            gameObject.SetActive(false);
            _onClose?.Invoke();
        });

        gameObject.SetActive(true);
    }
}