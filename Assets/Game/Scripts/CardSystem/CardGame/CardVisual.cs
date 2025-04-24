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

    [Header("Visual Effects")]
    public GameObject glowEffect;
    public ParticleSystem playParticles;

    [Header("Animation Settings")]
    public float hoverScale = 1.2f;
    public float hoverDuration = 0.2f;
    public float dragScale = 1.1f;
    public float playAnimDuration = 0.5f;

    // Private variables
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
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _originalScale = transform.localScale;

        if (glowEffect != null)
            glowEffect.SetActive(false);
    }

    public Player GetOwner()
    {
        return _owner;
    }

    public Card GetCard()
    {
        return _card;
    }

    public void SetupCard(Card card, Player owner)
    {
        _card = card;
        _owner = owner;

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

    private bool IsPlayable()
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

    private bool CanAttack()
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _isDragging = false;

        // Reset transparency
        _canvasGroup.alpha = 1f;

        // Check if card was dropped on a valid target
        if (CanAttack())
        {
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
            // Check if card was dropped on play area
            if (CardGameManager.Instance.IsOverPlayArea(eventData.position))
            {
                // Play the card
                bool success = _owner.PlayCard(_card);

                if (success)
                {
                    // Play animation
                    PlayCardAnimation();
                }
                else
                {
                    // Return to original position
                    transform.DOMove(_originalPosition, hoverDuration).SetEase(Ease.OutQuad);
                    transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad);
                }
            }
            else
            {
                // Return to original position
                transform.DOMove(_originalPosition, hoverDuration).SetEase(Ease.OutQuad);
                transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Double click to play card
        if (eventData.clickCount == 2 && IsPlayable())
        {
            bool success = _owner.PlayCard(_card);

            if (success)
            {
                PlayCardAnimation();
            }
        }
    }
    #endregion

    private void PlayCardAnimation()
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
            // Move to field
            transform.DOMove(_owner.fieldArea.position, playAnimDuration)
                .SetEase(Ease.OutBack);

            // Set as field card
            SetFieldCard();
        }
        else
        {
            // For spells, animate and destroy
            transform.DOScale(_originalScale * 1.5f, playAnimDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    transform.DOScale(Vector3.zero, playAnimDuration * 0.5f)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() => {
                            Destroy(gameObject);
                        });
                });
        }
    }
}