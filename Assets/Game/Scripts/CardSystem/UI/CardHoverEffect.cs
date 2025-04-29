using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CardHoverEffect : MonoBehaviour
{
    [Header("Hover Settings")]
    [Range(0f, 10f)] public float hoverHeight = 0.5f;
    [Range(0f, 1f)] public float hoverDuration = 0.2f;
    [Range(0f, 1f)] public float swayAmount = 0.05f;
    [Range(0f, 2f)] public float swayDuration = 1f;

    [Header("3D Tilt Settings")]
    [Range(0f, 20f)] public float maxTiltAngle = 10f;
    [Range(0f, 1f)] public float tiltSpeed = 0.2f;
    [Range(0f, 1f)] public float returnSpeed = 0.5f;
    public bool invertTilt = false;

    [Header("Selection Settings")]
    [Range(0f, 1f)] public float selectedHoverHeight = 0.8f;
    [Range(0f, 1f)] public float selectionBounceAmount = 0.1f;
    [Range(0f, 1f)] public float selectionBounceDuration = 0.3f;

    [Header("References")]
    public Transform cardFront;
    public Transform cardArt;
    public Transform cardFrame;
    public Transform cardText;

    // Private variables
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Vector3 _startScale;
    private bool _isHovering = false;
    private bool _isSelected = false;
    private bool _isDragging = false;
    private Sequence _swaySequence;
    private Sequence _hoverSequence;
    private CardVisual _cardVisual;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Camera _mainCamera;
    private bool _initialized = false;

    private void Awake()
    {
        _cardVisual = GetComponent<CardVisual>();
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _mainCamera = Camera.main;

        // If references aren't set, try to find them
        if (cardFront == null) cardFront = transform;
        if (cardArt == null) cardArt = transform.Find("CardArt");
        if (cardFrame == null) cardFrame = transform.Find("CardBackground");
        if (cardText == null) cardText = transform.Find("CardNameText");
    }

    private void Start()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (_initialized)
            return;

        _startPosition = transform.localPosition;
        _startRotation = transform.localRotation;
        _startScale = transform.localScale;

        // Start the idle sway animation
        StartSwayAnimation();

        _initialized = true;
    }

    private void OnEnable()
    {
        // Reset position and rotation when enabled
        if (_initialized)
        {
            transform.localPosition = _startPosition;
            transform.localRotation = _startRotation;
            transform.localScale = _startScale;

            // Start the idle sway animation
            StartSwayAnimation();
        }
    }

    private void OnDisable()
    {
        // Kill all animations when disabled
        KillAllAnimations();
    }

    private void OnDestroy()
    {
        // Kill all animations when destroyed
        KillAllAnimations();
    }

    private void KillAllAnimations()
    {
        if (_swaySequence != null)
        {
            _swaySequence.Kill(false);
            _swaySequence = null;
        }

        if (_hoverSequence != null)
        {
            _hoverSequence.Kill(false);
            _hoverSequence = null;
        }
    }

    private void Update()
    {
        // Only apply 3D tilt effect when hovering and not dragging
        if (_isHovering && !_isSelected && !_isDragging && this != null && gameObject != null && gameObject.activeInHierarchy)
        {
            Apply3DTiltEffect();
        }
    }

    private void StartSwayAnimation()
    {
        // Kill any existing sway animation
        if (_swaySequence != null)
        {
            _swaySequence.Kill(false);
            _swaySequence = null;
        }

        // Check if the object is still valid
        if (this == null || !gameObject.activeInHierarchy)
            return;

        // Create a new sway animation
        _swaySequence = DOTween.Sequence();

        // Add subtle rotation sway
        _swaySequence.Append(transform.DOLocalRotate(new Vector3(0, 0, swayAmount), swayDuration / 2).SetEase(Ease.InOutSine));
        _swaySequence.Append(transform.DOLocalRotate(new Vector3(0, 0, -swayAmount), swayDuration / 2).SetEase(Ease.InOutSine));

        // Loop the animation
        _swaySequence.SetLoops(-1, LoopType.Yoyo);
        _swaySequence.SetUpdate(true);
    }

    public void OnCardHoverEnter()
    {
        InitializeIfNeeded();

        if (_isSelected || _isDragging || this == null || !gameObject.activeInHierarchy)
            return;

        _isHovering = true;

        // Kill any existing hover animation
        if (_hoverSequence != null)
        {
            _hoverSequence.Kill(false);
            _hoverSequence = null;
        }

        // Create a new hover animation
        _hoverSequence = DOTween.Sequence();

        // Move the card up
        _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y + hoverHeight, hoverDuration).SetEase(Ease.OutQuad));

        // Pause the sway animation
        if (_swaySequence != null)
            _swaySequence.Pause();

        // Reset rotation to prepare for tilt effect
        transform.localRotation = _startRotation;
    }

    public void OnCardHoverExit()
    {
        if (_isSelected || _isDragging || this == null || !gameObject.activeInHierarchy)
            return;

        _isHovering = false;

        // Kill any existing hover animation
        if (_hoverSequence != null)
        {
            _hoverSequence.Kill(false);
            _hoverSequence = null;
        }

        // Create a new hover exit animation
        _hoverSequence = DOTween.Sequence();

        // Move the card back down
        _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y, hoverDuration).SetEase(Ease.OutQuad));

        // Return to original rotation
        _hoverSequence.Join(transform.DOLocalRotate(Vector3.zero, returnSpeed).SetEase(Ease.OutQuad));

        // Resume the sway animation
        if (_swaySequence != null)
            _swaySequence.Play();
    }

    public void OnCardSelected(bool selected)
    {
        InitializeIfNeeded();

        if (this == null || !gameObject.activeInHierarchy)
            return;

        _isSelected = selected;

        // Kill any existing hover animation
        if (_hoverSequence != null)
        {
            _hoverSequence.Kill(false);
            _hoverSequence = null;
        }

        // Create a new selection animation
        _hoverSequence = DOTween.Sequence();

        if (selected)
        {
            // Pause the sway animation
            if (_swaySequence != null)
                _swaySequence.Pause();

            // Move the card up higher
            _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y + selectedHoverHeight, hoverDuration).SetEase(Ease.OutQuad));

            // Add a little bounce effect
            _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y + selectedHoverHeight - selectionBounceAmount, selectionBounceDuration / 2).SetEase(Ease.OutQuad));
            _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y + selectedHoverHeight, selectionBounceDuration / 2).SetEase(Ease.OutQuad));

            // Reset rotation
            transform.localRotation = _startRotation;
        }
        else
        {
            // If we're still being hovered over, go back to hover height
            if (_isHovering)
            {
                _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y + hoverHeight, hoverDuration).SetEase(Ease.OutQuad));
            }
            else
            {
                // Otherwise go back to start position
                _hoverSequence.Append(transform.DOLocalMoveY(_startPosition.y, hoverDuration).SetEase(Ease.OutQuad));

                // Resume the sway animation
                if (_swaySequence != null)
                    _swaySequence.Play();
            }
        }
    }

    public void OnBeginDrag()
    {
        InitializeIfNeeded();

        if (this == null || !gameObject.activeInHierarchy)
            return;

        _isDragging = true;

        // Kill all animations
        if (_swaySequence != null)
            _swaySequence.Pause();

        if (_hoverSequence != null)
        {
            _hoverSequence.Kill(false);
            _hoverSequence = null;
        }

        // Save the current position as the start position for when we return
        _startPosition = transform.localPosition;

        // Reset rotation
        transform.localRotation = Quaternion.identity;
    }

    public void OnDrag()
    {
        // Nothing special needed during drag - the CardVisual handles movement
    }

    public void OnEndDrag(bool returnToHand)
    {
        if (this == null || !gameObject.activeInHierarchy)
            return;

        _isDragging = false;

        if (returnToHand)
        {
            // Let the card visual handle returning to hand
            // We'll update our state when the card is back in position
        }
        else
        {
            // Card was played - we can stop all our effects
            KillAllAnimations();
        }
    }

    public void UpdateStartPosition(Vector3 newPosition)
    {
        if (this == null || !gameObject.activeInHierarchy)
            return;

        _startPosition = newPosition;
    }

    private void Apply3DTiltEffect()
    {
        if (_canvas == null || _rectTransform == null || _mainCamera == null)
            return;

        // Get mouse position in screen space
        Vector3 mousePos = Input.mousePosition;

        // Convert to local position relative to the card
        Vector2 localMousePos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, mousePos, _canvas.worldCamera, out localMousePos))
            return;

        // Calculate tilt based on mouse position
        float halfWidth = _rectTransform.rect.width * 0.5f;
        float halfHeight = _rectTransform.rect.height * 0.5f;

        float xTilt = Mathf.Clamp(localMousePos.x / halfWidth, -1f, 1f) * maxTiltAngle;
        float yTilt = Mathf.Clamp(localMousePos.y / halfHeight, -1f, 1f) * maxTiltAngle;

        // Invert tilt if needed
        if (invertTilt)
        {
            xTilt = -xTilt;
            yTilt = -yTilt;
        }

        // Apply rotation smoothly
        Quaternion targetRotation = Quaternion.Euler(-yTilt, xTilt, 0);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, tiltSpeed);

        // Apply parallax effect to card elements if they exist
        if (cardArt != null)
        {
            cardArt.localPosition = new Vector3(xTilt * -0.01f, yTilt * -0.01f, cardArt.localPosition.z);
        }

        if (cardFrame != null)
        {
            cardFrame.localPosition = new Vector3(xTilt * -0.005f, yTilt * -0.005f, cardFrame.localPosition.z);
        }

        if (cardText != null)
        {
            cardText.localPosition = new Vector3(xTilt * -0.015f, yTilt * -0.015f, cardText.localPosition.z);
        }
    }
}