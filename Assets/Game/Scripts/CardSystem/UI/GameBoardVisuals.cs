// Create a new script called GameBoardVisuals.cs
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameBoardVisuals : MonoBehaviour
{
    [Header("Board Elements")]
    public Image boardBackground;
    public RectTransform playerFieldArea;
    public RectTransform opponentFieldArea;
    public GameObject cardSlotPrefab;
    public int maxCardSlots = 7;

    [Header("Visual Effects")]
    public ParticleSystem playerAreaParticles;
    public ParticleSystem opponentAreaParticles;
    public GameObject turnIndicator;
    public Image turnIndicatorGlow;

    [Header("Animation Settings")]
    public float slotPulseInterval = 3f;
    public float turnIndicatorPulseDuration = 1f;

    private GameObject[] _playerCardSlots;
    private GameObject[] _opponentCardSlots;

    private void Start()
    {
        // Create card slots
        CreateCardSlots();

        // Start visual effects
        StartPulsingEffects();
    }

    private void CreateCardSlots()
    {
        // Create player card slots
        _playerCardSlots = new GameObject[maxCardSlots];
        for (int i = 0; i < maxCardSlots; i++)
        {
            _playerCardSlots[i] = Instantiate(cardSlotPrefab, playerFieldArea);
            _playerCardSlots[i].name = $"PlayerCardSlot_{i}";
        }

        // Create opponent card slots
        _opponentCardSlots = new GameObject[maxCardSlots];
        for (int i = 0; i < maxCardSlots; i++)
        {
            _opponentCardSlots[i] = Instantiate(cardSlotPrefab, opponentFieldArea);
            _opponentCardSlots[i].name = $"OpponentCardSlot_{i}";
        }

        // Arrange slots
        ArrangeCardSlots();
    }

    private void ArrangeCardSlots()
    {
        // Calculate spacing for player slots
        float playerSlotWidth = _playerCardSlots[0].GetComponent<RectTransform>().rect.width;
        float playerTotalWidth = playerFieldArea.rect.width;
        float playerSpacing = (playerTotalWidth - (playerSlotWidth * maxCardSlots)) / (maxCardSlots + 1);

        // Position player slots
        for (int i = 0; i < maxCardSlots; i++)
        {
            RectTransform slotRect = _playerCardSlots[i].GetComponent<RectTransform>();
            float xPos = -playerTotalWidth / 2 + playerSpacing + (i * (playerSlotWidth + playerSpacing)) + playerSlotWidth / 2;
            slotRect.anchoredPosition = new Vector2(xPos, 0);
        }

        // Calculate spacing for opponent slots
        float opponentSlotWidth = _opponentCardSlots[0].GetComponent<RectTransform>().rect.width;
        float opponentTotalWidth = opponentFieldArea.rect.width;
        float opponentSpacing = (opponentTotalWidth - (opponentSlotWidth * maxCardSlots)) / (maxCardSlots + 1);

        // Position opponent slots
        for (int i = 0; i < maxCardSlots; i++)
        {
            RectTransform slotRect = _opponentCardSlots[i].GetComponent<RectTransform>();
            float xPos = -opponentTotalWidth / 2 + opponentSpacing + (i * (opponentSlotWidth + opponentSpacing)) + opponentSlotWidth / 2;
            slotRect.anchoredPosition = new Vector2(xPos, 0);
        }
    }

    private void StartPulsingEffects()
    {
        // Pulse card slots
        for (int i = 0; i < maxCardSlots; i++)
        {
            int index = i; // Capture for lambda
            float delay = i * 0.2f; // Stagger the animations

            // Player slots pulse
            Image playerSlotImage = _playerCardSlots[index].GetComponent<Image>();
            Color originalPlayerColor = playerSlotImage.color;
            DOTween.Sequence()
                .AppendInterval(delay)
                .AppendCallback(() => {
                    playerSlotImage.DOColor(new Color(originalPlayerColor.r + 0.2f,
                                                     originalPlayerColor.g + 0.2f,
                                                     originalPlayerColor.b + 0.2f,
                                                     originalPlayerColor.a),
                                          slotPulseInterval / 2)
                        .SetLoops(-1, LoopType.Yoyo);
                });

            // Opponent slots pulse
            Image opponentSlotImage = _opponentCardSlots[index].GetComponent<Image>();
            Color originalOpponentColor = opponentSlotImage.color;
            DOTween.Sequence()
                .AppendInterval(delay)
                .AppendCallback(() => {
                    opponentSlotImage.DOColor(new Color(originalOpponentColor.r + 0.2f,
                                                       originalOpponentColor.g + 0.2f,
                                                       originalOpponentColor.b + 0.2f,
                                                       originalOpponentColor.a),
                                            slotPulseInterval / 2)
                        .SetLoops(-1, LoopType.Yoyo);
                });
        }

        // Pulse turn indicator
        if (turnIndicatorGlow != null)
        {
            turnIndicatorGlow.DOFade(0.2f, turnIndicatorPulseDuration)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    public void UpdateTurnIndicator(bool isPlayerTurn)
    {
        if (turnIndicator != null)
        {
            // Rotate indicator to point to current player
            float targetRotation = isPlayerTurn ? 0 : 180;
            turnIndicator.transform.DORotate(new Vector3(0, 0, targetRotation), 0.5f, RotateMode.Fast)
                .SetEase(Ease.OutBack);

            // Change color based on turn
            if (turnIndicatorGlow != null)
            {
                Color targetColor = isPlayerTurn ? Color.green : Color.red;
                turnIndicatorGlow.DOColor(targetColor, 0.5f);
            }
        }
    }

    public void PlayFieldParticles(bool playerSide)
    {
        if (playerSide && playerAreaParticles != null)
        {
            playerAreaParticles.Play();
        }
        else if (!playerSide && opponentAreaParticles != null)
        {
            opponentAreaParticles.Play();
        }
    }
}