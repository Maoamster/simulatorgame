using UnityEngine;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    [SerializeField] private float promptAnimDuration = 0.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Hide prompt initially
        if (interactionPromptPanel != null)
        {
            interactionPromptPanel.SetActive(false);
        }
    }

    public void ShowInteractionPrompt(string text)
    {
        if (interactionPromptPanel == null || interactionPromptText == null) return;

        interactionPromptText.text = text;
        interactionPromptPanel.SetActive(true);

        // Animate prompt appearance
        interactionPromptPanel.transform.localScale = Vector3.zero;
        interactionPromptPanel.transform.DOScale(1f, promptAnimDuration).SetEase(Ease.OutBack);
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptPanel == null) return;

        interactionPromptPanel.transform.DOScale(0f, promptAnimDuration).SetEase(Ease.InBack)
            .OnComplete(() => interactionPromptPanel.SetActive(false));
    }
}