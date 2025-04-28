using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardSlotVisual : MonoBehaviour
{
    public Image slotImage;
    public Image glowImage;

    [Header("Animation Settings")]
    public float pulseDuration = 1.5f;
    public float pulseIntensity = 0.2f;
    public float hoverScale = 1.05f;

    private Color _originalColor;
    private Color _originalGlowColor;
    private Sequence _pulseSequence;

    private void Awake()
    {
        if (slotImage == null)
            slotImage = GetComponent<Image>();

        if (glowImage == null && transform.childCount > 0)
            glowImage = transform.GetChild(0).GetComponent<Image>();

        // Store original colors
        if (slotImage != null)
            _originalColor = slotImage.color;

        if (glowImage != null)
            _originalGlowColor = glowImage.color;
    }

    private void Start()
    {
        // Start subtle pulsing animation
        StartPulseAnimation();
    }

    private void StartPulseAnimation()
    {
        // Stop any existing sequence
        if (_pulseSequence != null)
            _pulseSequence.Kill();

        // Create new sequence
        _pulseSequence = DOTween.Sequence();

        // Pulse the main slot
        if (slotImage != null)
        {
            Color targetColor = new Color(
                _originalColor.r + pulseIntensity,
                _originalColor.g + pulseIntensity,
                _originalColor.b + pulseIntensity,
                _originalColor.a
            );

            _pulseSequence.Append(slotImage.DOColor(targetColor, pulseDuration / 2))
                          .Append(slotImage.DOColor(_originalColor, pulseDuration / 2));
        }

        // Pulse the glow
        if (glowImage != null)
        {
            _pulseSequence.Join(glowImage.DOFade(_originalGlowColor.a + pulseIntensity, pulseDuration / 2))
                          .Join(glowImage.DOFade(_originalGlowColor.a, pulseDuration / 2));
        }

        // Loop forever
        _pulseSequence.SetLoops(-1, LoopType.Restart);
    }

    public void Highlight(bool active)
    {
        // Cancel pulse animation
        if (_pulseSequence != null)
            _pulseSequence.Pause();

        if (active)
        {
            // Highlight the slot
            if (slotImage != null)
            {
                slotImage.DOColor(Color.green, 0.3f);
            }

            if (glowImage != null)
            {
                glowImage.DOColor(new Color(0, 1, 0, 0.5f), 0.3f);
                glowImage.DOFade(0.8f, 0.3f);
            }

            // Scale up slightly
            transform.DOScale(hoverScale, 0.3f);
        }
        else
        {
            // Return to normal
            if (slotImage != null)
            {
                slotImage.DOColor(_originalColor, 0.3f);
            }

            if (glowImage != null)
            {
                glowImage.DOColor(_originalGlowColor, 0.3f);
            }

            // Scale back to normal
            transform.DOScale(1f, 0.3f);

            // Resume pulse animation
            if (_pulseSequence != null)
                _pulseSequence.Play();
        }
    }

    public void ShowCardPlaced()
    {
        // Stop pulse animation
        if (_pulseSequence != null)
            _pulseSequence.Kill();

        // Flash effect
        if (slotImage != null)
        {
            Sequence placeSequence = DOTween.Sequence();
            placeSequence.Append(slotImage.DOColor(Color.white, 0.2f))
                         .Append(slotImage.DOColor(new Color(0.3f, 0.3f, 0.3f, 0.3f), 0.3f));
        }

        // Hide glow
        if (glowImage != null)
        {
            glowImage.DOFade(0, 0.3f);
        }
    }

    public void ShowCardRemoved()
    {
        // Flash effect
        if (slotImage != null)
        {
            Sequence removeSequence = DOTween.Sequence();
            removeSequence.Append(slotImage.DOColor(Color.red, 0.2f))
                          .Append(slotImage.DOColor(_originalColor, 0.3f));
        }

        // Show glow again
        if (glowImage != null)
        {
            glowImage.DOFade(_originalGlowColor.a, 0.3f);
        }

        // Restart pulse animation
        StartPulseAnimation();
    }
}