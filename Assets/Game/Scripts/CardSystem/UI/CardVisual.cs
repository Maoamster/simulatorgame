using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections;

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
    private CardVisual _targetCardVisual;

    [Header("Card Back")]
    public Sprite cardBackSprite; // Assign this in the Inspector

    [Header("Card Style")]
    public Image cardBorder;
    public Image cardTypeIcon;
    public Image rarityGem;
    public Image manaCrystal;
    public GameObject tauntFrame;
    public ParticleSystem rarityParticles;
    private Material _attackLineMaterial;
    private float _lineScrollSpeed = 2f;

    [Header("Visual Effects")]
    public GameObject glowEffect;
    public ParticleSystem playParticles;

    [Header("Animation Settings")]
    public float hoverScale = 1.2f;
    public float hoverDuration = 0.2f;
    public float dragScale = 1.1f;
    public float playAnimDuration = 0.5f;

    // Private variables
    private bool _isOpponentCard = false;
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
        // Initialize components
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _originalScale = transform.localScale;

        // Initialize _normalFieldColor
        _normalFieldColor = new Color(1f, 1f, 1f, 1f); // Default white

        // Initialize attack line
        if (_attackLine == null)
        {
            _attackLine = gameObject.AddComponent<LineRenderer>();
            _attackLine.startWidth = 1f;
            _attackLine.endWidth = 1f;

            // Create a material with a dash pattern
            _attackLineMaterial = new Material(Shader.Find("Sprites/Default"));
            _attackLineMaterial.color = Color.red;

            // Create a simple dash pattern
            Texture2D dashTexture = new Texture2D(16, 2, TextureFormat.RGBA32, false);
            Color[] colors = new Color[32];
            for (int i = 0; i < 32; i++)
            {
                colors[i] = (i < 8) ? Color.white : Color.clear;
            }
            dashTexture.SetPixels(colors);
            dashTexture.Apply();
            dashTexture.wrapMode = TextureWrapMode.Repeat;

            _attackLineMaterial.mainTexture = dashTexture;
            _attackLine.material = _attackLineMaterial;

            _attackLine.startColor = Color.red;
            _attackLine.endColor = Color.red;
            _attackLine.positionCount = 2;
            _attackLine.enabled = false;
        }

        // Initialize UI elements
        if (selectionHighlight != null)
            selectionHighlight.SetActive(false);

        if (glowEffect != null)
            glowEffect.SetActive(false);
    }

    private void Update()
    {
        // Update attack line if needed
        if (_attackLine != null && _attackLine.enabled)
        {
            UpdateAttackLine();
        }
    }

    public void ClearAttackLine()
    {
        _targetCardVisual = null;
        if (_attackLine != null)
        {
            _attackLine.enabled = false;
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

        // Initialize _playerFieldRect
        if (_owner != null && _owner.fieldArea != null)
        {
            _playerFieldRect = _owner.fieldArea.GetComponent<RectTransform>();
        }

        // Set the reference back to this visual instance
        card.visualInstance = this;

        // Check if this is an opponent's hand card
        bool isOpponentHandCard = (owner != CardGameManager.Instance.playerOne && !_isOnField);
        _isOpponentCard = (owner != CardGameManager.Instance.playerOne);

        if (isOpponentHandCard)
        {
            // Show card back for opponent's hand cards
            ShowCardBack();
        }
        else
        {
            // Show normal card front
            ShowCardFront();

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
        }

        // Set appropriate scale based on owner
        if (_isOpponentCard)
        {
            // This is an opponent card - set to normal scale
            transform.localScale = Vector3.one;
        }
        else
        {
            _originalScale = transform.localScale;
        }
    }

    public void UpdateCardVisual()
    {
        // Update stats for creatures
        if (_card.type == Card.CardType.Creature && creatureStatsPanel.activeSelf)
        {
            CreatureCard creatureCard = _card as CreatureCard;
            if (creatureCard != null)
            {
                attackText.text = creatureCard.currentAttack.ToString();
                healthText.text = creatureCard.currentHealth.ToString();

                // Visual feedback for damaged cards
                if (creatureCard.currentHealth < creatureCard.health)
                {
                    healthText.color = Color.red;
                }
                else
                {
                    healthText.color = Color.white;
                }
            }
        }
    }

    public void SetFieldCard()
    {
        _isOnField = true;

        // If this was an opponent's hand card, now show the front
        if (_isOpponentCard)
        {
            ShowCardFront();

            // Set basic card info
            cardNameText.text = _card.cardName;
            cardDescriptionText.text = _card.cardDescription;
            manaCostText.text = _card.manaCost.ToString();

            // Set card artwork
            if (_card.cardArtwork != null)
                cardArtwork.sprite = _card.cardArtwork;

            // Set card background color
            cardBackground.color = _card.cardColor;

            // Set up creature stats if applicable
            if (_card.type == Card.CardType.Creature)
            {
                creatureStatsPanel.SetActive(true);
                attackText.text = _card.attack.ToString();
                healthText.text = _card.health.ToString();
            }
        }

        // Set appropriate scale based on owner
        if (_isOpponentCard)
        {
            // This is an opponent card - ensure it's at normal scale
            transform.localScale = Vector3.one;
        }
        else
        {
            // Player card - use original scale
            transform.localScale = _originalScale;
        }
    }

    private bool IsPointOverField(Vector2 screenPoint)
    {
        // Check if _playerFieldRect is initialized
        if (_playerFieldRect == null)
        {
            // Try to initialize it
            if (_owner != null && _owner.fieldArea != null)
            {
                _playerFieldRect = _owner.fieldArea.GetComponent<RectTransform>();
            }

            // If still null, we can't check
            if (_playerFieldRect == null)
            {
                Debug.LogWarning("Cannot check if point is over field: _playerFieldRect is null");
                return false;
            }
        }

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
        if (_card is CreatureCard creatureCard)
        {
            // If the card has already attacked this turn, it can't attack again
            if (creatureCard.hasAttackedThisTurn)
                return false;

            // If the card can't attack this turn (e.g., summoning sickness), it can't attack
            if (!creatureCard.canAttackThisTurn)
                return false;
        }

        return true;
    }

    private void ShowCardBack()
    {
        // Hide all card front elements based on your hierarchy
        Transform cardNameText = transform.Find("CardNameText");
        if (cardNameText != null) cardNameText.gameObject.SetActive(false);

        Transform cardDescriptionText = transform.Find("CardDescriptionText");
        if (cardDescriptionText != null) cardDescriptionText.gameObject.SetActive(false);

        Transform manaCostBg = transform.Find("ManaCostBg");
        if (manaCostBg != null) manaCostBg.gameObject.SetActive(false);

        Transform statsPanel = transform.Find("StatsPanel");
        if (statsPanel != null) statsPanel.gameObject.SetActive(false);

        // Show card back image
        Transform cardBackground = transform.Find("CardBackground");
        if (cardBackground != null)
        {
            Image bgImage = cardBackground.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = Color.white; // Reset to white to show the sprite properly
            }
        }

        // Set card art to card back sprite
        Transform cardArt = transform.Find("CardArt");
        if (cardArt != null)
        {
            Image artImage = cardArt.GetComponent<Image>();
            if (artImage != null)
            {
                // Use the assigned card back sprite
                if (cardBackSprite != null)
                {
                    artImage.sprite = cardBackSprite;
                    artImage.color = Color.white; // Ensure full opacity
                }
                else
                {
                    // Fallback if no card back sprite is assigned
                    artImage.color = new Color(0.2f, 0.2f, 0.8f);
                }
            }
        }
    }

    private void ShowCardFront()
    {
        // Show all card front elements
        if (cardNameText != null) cardNameText.gameObject.SetActive(true);
        if (cardDescriptionText != null) cardDescriptionText.gameObject.SetActive(true);
        if (manaCostText != null) manaCostText.gameObject.SetActive(true);

        // Reset card artwork color
        if (cardArtwork != null)
        {
            cardArtwork.color = Color.white;
        }
    }

    #region Interface Implementations
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Skip hover effects for opponent cards if they're in the opponent's field
        if (_isOpponentCard && _isOnField)
        {
            // Still show preview for opponent cards
            CardGameManager.Instance.ShowCardPreview(_card);
            return;
        }

        if (!_isDragging)
        {
            // Call the hover effect if it exists
            CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
            if (hoverEffect != null)
            {
                hoverEffect.OnCardHoverEnter();
            }
            else
            {
                // Fall back to the original scale animation if no hover effect
                transform.DOScale(_originalScale * hoverScale, hoverDuration).SetEase(Ease.OutQuad);
            }

            if (glowEffect != null && (IsPlayable() || CanAttack()))
                glowEffect.SetActive(true);

            // Show enlarged preview
            CardGameManager.Instance.ShowCardPreview(_card);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Skip hover effects for opponent cards if they're in the opponent's field
        if (_isOpponentCard && _isOnField)
        {
            // Still hide preview
            CardGameManager.Instance.HideCardPreview();
            return;
        }

        if (!_isDragging)
        {
            // Call the hover effect if it exists
            CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
            if (hoverEffect != null)
            {
                hoverEffect.OnCardHoverExit();
            }
            else
            {
                // Fall back to the original scale animation if no hover effect
                transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad);
            }

            if (glowEffect != null)
                glowEffect.SetActive(false);

            // Hide preview
            CardGameManager.Instance.HideCardPreview();
        }
    }


    // In the OnBeginDrag method
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsPlayable() && !CanAttack())
            return;

        _isDragging = true;
        _originalPosition = transform.position;

        // Notify the hover effect
        CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
        if (hoverEffect != null)
        {
            hoverEffect.OnBeginDrag();
        }

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

        // Highlight field if we're over it and we have a valid field rect
        if (IsPlayable() && _owner != null && _owner.fieldArea != null)
        {
            Image fieldImage = _owner.fieldArea.GetComponent<Image>();
            if (fieldImage != null)
            {
                // Initialize _normalFieldColor if needed
                if (_normalFieldColor.a == 0) // Check if color is uninitialized
                {
                    _normalFieldColor = fieldImage.color;
                }

                bool isOverField = false;

                // Only check if point is over field if we have a valid _playerFieldRect
                if (_playerFieldRect != null)
                {
                    isOverField = IsPointOverField(eventData.position);
                }

                if (isOverField)
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
        if (!_isDragging)
            return;

        _isDragging = false;

        // Reset transparency
        _canvasGroup.alpha = 1f;

        bool returnToHand = true;

        // Check if card was dropped on a valid target
        if (CanAttack())
        {
            // Check if dropped on enemy player
            Player targetPlayer = CardGameManager.Instance.GetPlayerUnderMouse();
            if (targetPlayer != null && targetPlayer != _owner)
            {
                // Check if opponent has taunt creatures
                if (!targetPlayer.HasTauntCreatures())
                {
                    // Attack player directly
                    CardGameManager.Instance.AttackPlayer(_card, targetPlayer);
                    returnToHand = false;
                }
            }
            else
            {
                // Check if dropped on enemy card
                Card target = CardGameManager.Instance.GetTargetUnderMouse();
                if (target != null)
                {
                    // Attack target
                    CardGameManager.Instance.AttackCard(_card, target);
                    returnToHand = false;
                }
            }

            CardGameManager.Instance.EndTargeting();
        }
        else if (IsPlayable())
        {
            // Check if card was dropped on player field specifically
            if (DropZone.currentHoveredZone != null &&
                DropZone.currentHoveredZone.zoneType == DropZone.ZoneType.PlayerField)
            {
                // Play the card
                bool success = _owner.PlayCard(_card);

                if (success)
                {
                    // Play animation
                    PlayCardAnimation();
                    returnToHand = false;
                }
            }
        }

        // Notify the hover effect
        CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
        if (hoverEffect != null)
        {
            hoverEffect.OnEndDrag(returnToHand);
        }

        if (returnToHand)
        {
            // Return to hand
            ReturnToHand();
        }
    }

    private void ReturnToHand()
    {
        // Return to original parent (hand area)
        if (_originalParent != null)
        {
            transform.SetParent(_originalParent);
        }

        // Reset scale
        transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad);

        // Force layout refresh if parent has a layout group
        if (_originalParent != null && _originalParent.GetComponent<LayoutGroup>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_originalParent.GetComponent<RectTransform>());
        }

        // Update the hover effect's start position after layout rebuild
        CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
        if (hoverEffect != null)
        {
            // Wait a frame for layout to complete
            StartCoroutine(UpdateHoverEffectStartPosition());
        }
    }

    private IEnumerator UpdateHoverEffectStartPosition()
    {
        // Wait for end of frame to ensure layout is complete
        yield return new WaitForEndOfFrame();

        // Update the hover effect's start position
        CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
        if (hoverEffect != null)
        {
            hoverEffect.UpdateStartPosition(transform.localPosition);
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
        Debug.Log($"Card clicked: {_card.cardName}, IsOnField: {_isOnField}, Type: {_card.type}, CanAttack: {CanAttack()}, IsOpponentCard: {_isOpponentCard}");

        // Check if we have a selected attacker first
        if (CardGameManager.Instance.HasSelectedAttacker())
        {
            // If this is an opponent card on the field, it can be targeted
            if (_isOpponentCard && _isOnField && _card.type == Card.CardType.Creature)
            {
                Debug.Log($"Targeting opponent card: {_card.cardName}");
                CardGameManager.Instance.SetAttackTarget(this);
                return;
            }

            // If this is our own selected attacker, deselect it
            if (!_isOpponentCard && _isSelected)
            {
                Debug.Log($"Deselecting attacker: {_card.cardName}");
                CardGameManager.Instance.DeselectAttacker(this);
                return;
            }
        }

        // If no attacker is selected or we didn't handle targeting above:

        // Prevent other interactions with opponent cards
        if (_isOpponentCard)
            return;

        // If it's a creature on the field that can attack
        if (CanAttack())
        {
            // Select this card as attacker
            Debug.Log($"Selecting attacker: {_card.cardName}");
            CardGameManager.Instance.SelectAttacker(this);
        }
        // Double click to play card from hand
        else if (eventData.clickCount == 2 && IsPlayable())
        {
            Debug.Log($"Playing card: {_card.cardName}");
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

        // Call the selection effect if it exists
        CardHoverEffect hoverEffect = GetComponent<CardHoverEffect>();
        if (hoverEffect != null)
        {
            hoverEffect.OnCardSelected(selected);
        }
        else
        {
            // Fall back to the original punch animation if no hover effect
            if (selected)
            {
                AudioSource.PlayClipAtPoint(CardGameManager.Instance.cardSelectSound, transform.position);
                transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 0.5f);
            }
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

    public void SetAttackTarget(CardVisual targetCardVisual)
    {
        Debug.Log($"Setting attack target: {targetCardVisual.GetCard().cardName} for {_card.cardName}");

        _targetCardVisual = targetCardVisual;

        // Update attack line
        if (_targetCardVisual != null)
        {
            _attackLine.enabled = true;
            UpdateAttackLine();
            Debug.Log("Attack line enabled");
        }
        else
        {
            _attackLine.enabled = false;
            Debug.Log("Attack line disabled");
        }
    }

    private void UpdateAttackLine()
    {
        if (_targetCardVisual != null && _attackLine.enabled)
        {
            // Get the exact positions of both cards
            Vector3 startCardCenter = transform.position;
            Vector3 endCardCenter = _targetCardVisual.transform.position;

            // Get the size of both cards
            RectTransform startRect = GetComponent<RectTransform>();
            RectTransform endRect = _targetCardVisual.GetComponent<RectTransform>();

            // Calculate the direction vector
            Vector3 direction = (endCardCenter - startCardCenter).normalized;

            // Calculate the exact edge points
            float startCardRadius = startRect.rect.width * 0.5f;
            float endCardRadius = endRect.rect.width * 0.5f;

            // Calculate the exact start and end positions
            Vector3 startPos = startCardCenter + (direction * startCardRadius);
            Vector3 endPos = endCardCenter - (direction * endCardRadius);

            // Set the line positions
            _attackLine.SetPosition(0, startPos);
            _attackLine.SetPosition(1, endPos);

            // Update the scrolling texture
            if (_attackLineMaterial != null)
            {
                float offset = Time.time * _lineScrollSpeed;
                _attackLineMaterial.SetTextureOffset("_MainTex", new Vector2(-offset, 0));
            }
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