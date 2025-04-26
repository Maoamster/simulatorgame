using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening; // Requires DOTween asset

public class DialogueUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Image backgroundPanel;

    [Header("Animation Settings")]
    [SerializeField] private float appearDuration = 0.5f;
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float mouseResponseIntensity = 2f;
    [SerializeField] private float maxTiltAngle = 1.5f;
    [SerializeField] private float tiltSmoothness = 5f;
    [SerializeField] private float breathingIntensity = 0.02f;
    [SerializeField] private float breathingSpeed = 1f;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem textParticles;
    [SerializeField] private Image glowEffect;
    [SerializeField] private RectTransform highlightLine;

    // Private variables
    private Vector3 _initialScale;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector2 _lastMousePosition;
    private Vector2 _targetTilt;
    private Vector2 _currentTilt;
    private float _breathingPhase;
    private bool _isTyping = false;
    private Coroutine _typingCoroutine;
    private Camera _mainCamera;

    private void Awake()
    {
        _initialScale = dialoguePanel.localScale;
        _initialPosition = dialoguePanel.localPosition;
        _initialRotation = dialoguePanel.localRotation;
        _mainCamera = Camera.main;

        // Initialize UI
        dialoguePanel.localScale = Vector3.zero;
        backgroundPanel.color = new Color(0, 0, 0, 0);

        // Hide UI initially
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        // Mouse-based tilt effect
        Vector2 mousePos = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 mouseOffset = (mousePos - screenCenter) / screenCenter; // -1 to 1 range

        _targetTilt = mouseOffset * mouseResponseIntensity;
        _currentTilt = Vector2.Lerp(_currentTilt, _targetTilt, Time.deltaTime * tiltSmoothness);

        // Apply tilt (limited by max angle)
        float tiltX = Mathf.Clamp(_currentTilt.y * maxTiltAngle, -maxTiltAngle, maxTiltAngle);
        float tiltY = Mathf.Clamp(-_currentTilt.x * maxTiltAngle, -maxTiltAngle, maxTiltAngle);
        dialoguePanel.localRotation = Quaternion.Euler(tiltX, tiltY, 0);

        // Breathing effect
        _breathingPhase += Time.deltaTime * breathingSpeed;
        float breathingValue = Mathf.Sin(_breathingPhase) * breathingIntensity + 1f;
        dialoguePanel.localScale = _initialScale * breathingValue;

        // Add subtle movement based on camera motion if player is moving
        if (_mainCamera != null)
        {
            Vector3 cameraForward = _mainCamera.transform.forward;
            Vector3 cameraRight = _mainCamera.transform.right;

            // Subtle position offset based on camera direction
            Vector3 posOffset = (cameraRight * 0.01f) + (cameraForward * 0.01f);
            dialoguePanel.localPosition = _initialPosition + posOffset;
        }

        // Update glow effect
        if (glowEffect != null)
        {
            Color glowColor = glowEffect.color;
            glowColor.a = 0.5f + Mathf.Sin(_breathingPhase * 0.5f) * 0.2f;
            glowEffect.color = glowColor;
        }

        // Update highlight line
        if (highlightLine != null)
        {
            highlightLine.anchoredPosition = new Vector2(
                Mathf.Sin(_breathingPhase * 0.7f) * 50f,
                highlightLine.anchoredPosition.y
            );
        }
    }

    public void Show(string speakerName, string text, Sprite portrait)
    {
        gameObject.SetActive(true);

        // Set content
        speakerNameText.text = speakerName;
        speakerPortrait.sprite = portrait;

        // Clear choices
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        // Animate panel appearance
        dialoguePanel.localScale = Vector3.zero;
        backgroundPanel.color = new Color(0, 0, 0, 0);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(backgroundPanel.DOFade(0.7f, appearDuration * 0.5f));
        sequence.Append(dialoguePanel.DOScale(_initialScale, appearDuration).SetEase(Ease.OutBack));

        // Type text
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
        }
        _typingCoroutine = StartCoroutine(TypeText(text));

        // Play particle effect
        if (textParticles != null)
        {
            textParticles.Play();
        }
    }

    public void Hide()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(dialoguePanel.DOScale(Vector3.zero, appearDuration * 0.5f).SetEase(Ease.InBack));
        sequence.Append(backgroundPanel.DOFade(0, appearDuration * 0.3f));
        sequence.OnComplete(() => gameObject.SetActive(false));
    }

    public void ShowChoices(DialogueData.DialogueChoice[] choices)
    {
        // Clear existing choices
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        // No choices to show
        if (choices == null || choices.Length == 0)
        {
            choicesContainer.gameObject.SetActive(false);
            return;
        }

        choicesContainer.gameObject.SetActive(true);

        // Create choice buttons
        for (int i = 0; i < choices.Length; i++)
        {
            DialogueData.DialogueChoice choice = choices[i];
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);

            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.choiceText;
            }

            // Set button action
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                int choiceIndex = i; // Capture for lambda
                button.onClick.AddListener(() => {
                    DialogueManager.Instance.SelectChoice(choiceIndex);
                });
            }

            // Animate button appearance
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.localScale = Vector3.zero;
            buttonRect.DOScale(1f, 0.3f).SetDelay(i * 0.1f).SetEase(Ease.OutBack);
        }
    }

    private IEnumerator TypeText(string text)
    {
        _isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;

            // Play typing sound
            if (c != ' ' && Random.value > 0.7f)
            {
                // AudioManager.Instance.PlayTypingSound();
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
    }

    public bool IsTyping()
    {
        return _isTyping;
    }

    public void CompleteTyping()
    {
        if (_isTyping && _typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            dialogueText.text = dialogueText.text; // Show full text immediately
            _isTyping = false;
        }
    }
}