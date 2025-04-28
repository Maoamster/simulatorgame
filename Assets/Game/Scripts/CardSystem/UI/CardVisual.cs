using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class CardVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Card References")]
    public Image cardBackground;
    public Image cardArtwork;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI manaCostText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public GameObject creatureStatsPanel;

    [Header("Card Style")]
    public Image cardBorder;
    public Image cardTypeIcon;
    public Image rarityGem;
    public Image manaCrystal;
    public GameObject tauntFrame;
    public ParticleSystem rarityParticles;

    [Header("Visual Effects")]
    public GameObject glowEffect;
    public ParticleSystem playParticles;

    [Header("Animation Settings")]
    public float hoverScale = 1.2f;
    public float hoverDuration = 0.2f;
    public float dragScale = 1.1f;
    public float playAnimDuration = 0.5f;

    // Private variables
    private bool _isSelected = false;
    private Card _targetCard = null;
    private LineRenderer _attackLine;
    public GameObject selectionHighlight;
    public GameObject targetedHighlight;
    private GameObject _fieldHighlight;
    private Color _normalFieldColor;
    private RectTransform _playerFieldRect;
    private Transform _originalParent;
    private Card _card;
    private Player _owner;
    private bool _isDragging = false;
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private bool _isOnField = false;

    private void Awake()
    {
        _attackLine = gameObject.AddComponent<LineRenderer>();
        _attackLine.startWidth = 2f;
        _attackLine.endWidth = 2f;
        _attackLine.material = new Material(Shader.Find("Sprites/Default"));
        _attackLine.startColor = Color.red;
        _attackLine.endColor = Color.red;
        _attackLine.positionCount = 2;
        _attackLine.enabled = false;

        if (selectionHighlight != null)
            selectionHighlight.SetActive(false);

        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _originalScale = transform.localScale;

        if (glowEffect != null)
            glowEffect.SetActive(false);
    }

    private void Update()
    {
        // Update attack line if needed
        if (_attackLine.enabled)
        {
            UpdateAttackLine();
        }
    }

    public Player GetOwner()
    {
        return _owner;
    }

    public Card GetCard()
    {
        return _card;
    }

    private void CreateFieldHighlight()
    {
        if (_fieldHighlight == null && _owner != null && _owner.fieldArea != null)
        {
            // Get the field's image component
            Image fieldImage = _owner.fieldArea.GetComponent<Image>();
            if (fieldImage != null)
            {
                _normalFieldColor = fieldImage.color;
            }
        }
    }

    public void SetupCard(Card card, Player owner)
    {
        _card = card;
        _owner = owner;

        // Get reference to player field
        _playerFieldRect = owner.fieldArea.GetComponent<RectTransform>();

        // Set the reference back to this visual instance
        card.visualInstance = this;

        // Set basic card info
        cardNameText.text = card.cardName;
        cardDescriptionText.text = card.cardDescription;
        manaCostText.text = card.manaCost.ToString();

        // Set card artwork
        if (card.cardArtwork != null)
            cardArtwork.sprite = card.cardArtwork;

        // Set card background color
        cardBackground.color = card.cardColor;

        // Set up creature stats if applicable
        if (card.type == Card.CardType.Creature)
        {
            creatureStatsPanel.SetActive(true);
            attackText.text = card.attack.ToString();
            healthText.text = card.health.ToString();
        }
        else
        {
            creatureStatsPanel.SetActive(false);
        }

        // Set up visual elements based on card properties
        if (tauntFrame != null)
            tauntFrame.SetActive(card.hasTaunt);

        // Set rarity visual
        if (rarityGem != null)
        {
            switch (card.rarity)
            {
                case Card.Rarity.Common:
                    rarityGem.color = Color.white;
                    break;
                case Card.Rarity.Rare:
                    rarityGem.color = Color.blue;
                    if (rarityParticles != null)
                        rarityParticles.Play();
                    break;
                case Card.Rarity.Epic:
                    rarityGem.color = new Color(0.5f, 0, 0.5f); // Purple
                    if (rarityParticles != null)
                    {
                        var main = rarityParticles.main;
                        main.startColor = new Color(0.5f, 0, 0.5f);
                        rarityParticles.Play();
                    }
                    break;
                case Card.Rarity.Legendary:
                    rarityGem.color = new Color(1f, 0.5f, 0); // Orange
                    if (rarityParticles != null)
                    {
                        var main = rarityParticles.main;
                        main.startColor = new Color(1f, 0.5f, 0);
                        rarityParticles.Play();
                    }
                    break;
            }
        }

        // Set card type icon
        if (cardTypeIcon != null)
        {
            switch (card.type)
            {
                case Card.CardType.Creature:
                    cardTypeIcon.sprite = CardGameManager.Instance.creatureIcon;
                    break;
                case Card.CardType.Spell:
                    cardTypeIcon.sprite = CardGameManager.Instance.spellIcon;
                    break;
                    // Add other types as needed
            }
        }

        // Set mana crystal color based on cost
        if (manaCrystal != null)
        {
            if (card.manaCost <= 3)
                manaCrystal.color = Color.green;
            else if (card.manaCost <= 6)
                manaCrystal.color = Color.blue;
            else
                manaCrystal.color = new Color(0.5f, 0, 0.5f); // Purple for high cost
        }
    }

    public void UpdateCardVisual()
    {
        // Update stats for creatures
        if (_card.type == Card.CardType.Creature && creatureStatsPanel.activeSelf)
        {
            attackText.text = _card.attack.ToString();
            healthText.text = _card.health.ToString();
        }
    }

    public void SetFieldCard()
    {
        _isOnField = true;
    }

    private bool IsPointOverField(Vector2 screenPoint)
    {
        // Convert screen point to viewport point
        Vector2 viewportPoint = Camera.main.ScreenToViewportPoint(screenPoint);

        // Convert field rect corners to viewport points
        Vector3[] corners = new Vector3[4];
        _playerFieldRect.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            corners[i] = Camera.main.WorldToViewportPoint(corners[i]);
        }

        // Check if viewport point is inside the field rect
        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

        bool isInside = viewportPoint.x >= minX && viewportPoint.x <= maxX &&
                        viewportPoint.y >= minY && viewportPoint.y <= maxY;

        Debug.Log($"Screen point: {screenPoint}, Viewport: {viewportPoint}");
        Debug.Log($"Field bounds: ({minX},{minY}) to ({maxX},{maxY})");
        Debug.Log($"Is inside field: {isInside}");

        return isInside;
    }

    public bool IsPlayable()
    {
        // Check if it's our turn
        if (!CardGameManager.Instance.IsPlayerTurn(_owner))
            return false;

        // Check if we have enough mana
        if (_owner.currentMana < _card.manaCost)
            return false;

        // Check if we're in hand (not already played)
        if (_isOnField)
            return false;

        return true;
    }

    public bool CanAttack()
    {
        // Must be a creature on the field
        if (!_isOnField || _card.type != Card.CardType.Creature)
            return false;

        // Check if it's our turn
        if (!CardGameManager.Instance.IsPlayerTurn(_owner))
            return false;

        // Check if creature can attack this turn
        if (_card is CreatureCard creatureCard && !creatureCard.canAttackThisTurn)
            return false;

        return true;
    }

    #region Interface Implementations
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            transform.DOScale(_originalScale * hoverScale, hoverDuration).SetEase(Ease.OutQuad);

            if (glowEffect != null && (IsPlayable() || CanAttack()))
                glowEffect.SetActive(true);

            // Show enlarged preview
            CardGameManager.Instance.ShowCardPreview(_card);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad);

            if (glowEffect != null)
                glowEffect.SetActive(false);

            // Hide preview
            CardGameManager.Instance.HideCardPreview();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsPlayable() && !CanAttack())
            return;

        _isDragging = true;
        _originalPosition = transform.position;

        // Temporarily remove from layout group influence
        if (!_isOnField)
        {
            // Save original parent
            Transform originalParent = transform.parent;

            // Move to canvas directly to prevent layout group influence
            transform.SetParent(GetComponentInParent<Canvas>().transform);

            // Store original parent to return to if needed
            _originalParent = originalParent;
        }

        // Scale card
        transform.DOScale(_originalScale * dragScale, hoverDuration);

        // Make card semi-transparent
        _canvasGroup.alpha = 0.8f;

        // Bring to front
        transform.SetAsLastSibling();

        // Start targeting mode if attacking from field
        if (CanAttack())
        {
            CardGameManager.Instance.StartTargeting(_card, _owner);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        // Move card with mouse
        Vector3 screenPoint = new Vector3(eventData.position.x, eventData.position.y, 10);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPoint);
        transform.position = worldPosition;

        // Highlight field if we're over it
        if (IsPlayable())
        {
            Image fieldImage = _owner.fieldArea.GetComponent<Image>();
            if (fieldImage != null)
            {
                if (IsPointOverField(eventData.position))
                {
                    // Highlight field
                    fieldImage.color = new Color(0.5f, 1f, 0.5f, 0.5f); // Green highlight
                }
                else
                {
                    // Reset field color
                    fieldImage.color = _normalFieldColor;
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Reset field highlight
        Image fieldImage = _owner.fieldArea.GetComponent<Image>();
        if (fieldImage != null)
        {
            fieldImage.color = _normalFieldColor;
        }

        if (!_isDragging)
            return;

        _isDragging = false;

        // Reset transparency
        _canvasGroup.alpha = 1f;

        // Check if card was dropped on a valid target
        if (CanAttack())
        {
            // Attack logic (unchanged)
            Card target = CardGameManager.Instance.GetTargetUnderMouse();
            if (target != null)
            {
                // Attack target
                CardGameManager.Instance.AttackCard(_card, target);
            }
            else
            {
                // Return to original position
                transform.DOMove(_originalPosition, hoverDuration).SetEase(Ease.OutQuad);
            }

            CardGameManager.Instance.EndTargeting();
        }
        else if (IsPlayable())
        {
            // Use our direct method to check if over field
            if (IsPointOverField(eventData.position))
            {
                Debug.Log("Attempting to play card: " + _card.cardName);

                // Play the card
                bool success = _owner.PlayCard(_card);

                if (success)
                {
                    // Play animation
                    PlayCardAnimation();
                }
                else
                {
                    // Return to hand
                    ReturnCardToHand();
                }
            }
            else
            {
                Debug.Log("Not over field, returning to hand");
                // Return to hand
                ReturnCardToHand();
            }
        }
    }

    private void ReturnCardToHand()
    {
        // Return to original parent (hand area)
        transform.SetParent(_originalParent);

        // Reset scale
        transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad);

        // Force layout refresh if parent has a layout group
        if (_originalParent.GetComponent<LayoutGroup>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent.GetComponent<RectTransform>());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // If it's a creature on the field that can attack
        if (CanAttack())
        {
            // If not already selected, select this card
            if (!_isSelected)
            {
                CardGameManager.Instance.SelectAttacker(this);
            }
            else
            {
                // If already selected, deselect
                CardGameManager.Instance.DeselectAttacker(this);
            }
        }
        // If it's an enemy creature that can be targeted
        else if (_isOnField && _card.type == Card.CardType.Creature &&
                 _owner != CardGameManager.Instance.GetCurrentPlayer())
        {
            // Try to set as target for currently selected card
            CardGameManager.Instance.SetAttackTarget(this);
        }
        // Double click to play card from hand
        else if (eventData.clickCount == 2 && IsPlayable())
        {
            bool success = _owner.PlayCard(_card);

            if (success)
            {
                PlayCardAnimation();
            }
        }
    }
    #endregion

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (selectionHighlight != null)
            selectionHighlight.SetActive(selected);

        // Play selection sound/effect
        if (selected)
        {
            AudioSource.PlayClipAtPoint(CardGameManager.Instance.cardSelectSound, transform.position);
            transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
        }
    }

    public void SetTargeted(bool targeted)
    {
        if (targetedHighlight != null)
            targetedHighlight.SetActive(targeted);

        // Play targeted sound/effect
        if (targeted)
        {
            AudioSource.PlayClipAtPoint(CardGameManager.Instance.cardTargetSound, transform.position);
            transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
        }
    }

    public void SetAttackTarget(Card targetCard)
    {
        _targetCard = targetCard;

        // Update attack line
        if (_targetCard != null && _targetCard.visualInstance != null)
        {
            _attackLine.enabled = true;
            UpdateAttackLine();
        }
        else
        {
            _attackLine.enabled = false;
        }
    }

    private void UpdateAttackLine()
    {
        if (_targetCard != null && _targetCard.visualInstance != null && _attackLine.enabled)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = _targetCard.visualInstance.transform.position;

            // Adjust line to start/end at card edges rather than centers
            Vector3 direction = (endPos - startPos).normalized;
            float cardRadius = GetComponent<RectTransform>().rect.width * 0.4f;

            startPos += direction * cardRadius;
            endPos -= direction * cardRadius;

            _attackLine.SetPosition(0, startPos);
            _attackLine.SetPosition(1, endPos);
        }
    }



    public void PlayAICardAnimation()
    {
        // Play particles
        if (playParticles != null)
        {
            playParticles.Play();
        }

        // Play sound
        if (_card.playSound != null)
        {
            AudioSource.PlayClipAtPoint(_card.playSound, Camera.main.transform.position);
        }

        // Simple animation for AI playing a card
        transform.DOScale(_originalScale * 1.2f, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                transform.DOScale(_originalScale, 0.2f)
                    .SetEase(Ease.InQuad);
            });
    }

    public void PlayCardAnimation()
    {
        // Play particles
        if (playParticles != null)
        {
            playParticles.Play();
        }

        // Play sound
        if (_card.playSound != null)
        {
            AudioSource.PlayClipAtPoint(_card.playSound, Camera.main.transform.position);
        }

        // Animate to field position
        if (_card.type == Card.CardType.Creature)
        {
            // Set as field card
            SetFieldCard();

            // Reset scale
            transform.DOScale(_originalScale, playAnimDuration).SetEase(Ease.OutBack);

            // No need to set position - let the layout group handle it
        }
        else
        {
            // For spells, animate and destroy
            transform.DOScale(_originalScale * 1.5f, playAnimDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(Vector3.zero, playAnimDuration * 0.5f)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            Destroy(gameObject);
                        });
                });
        }
    }
}