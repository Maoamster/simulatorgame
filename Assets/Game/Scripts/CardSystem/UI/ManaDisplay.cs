using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ManaDisplay : MonoBehaviour
{
    public GameObject manaCrystalPrefab;
    public Transform crystalContainer;
    public TextMeshProUGUI manaText;
    public int maxCrystals = 10;

    private GameObject[] _manaCrystals;
    private int _currentMana = 0;
    private int _maxMana = 0;

    private void Start()
    {
        // Create mana crystals
        _manaCrystals = new GameObject[maxCrystals];
        for (int i = 0; i < maxCrystals; i++)
        {
            _manaCrystals[i] = Instantiate(manaCrystalPrefab, crystalContainer);
            _manaCrystals[i].name = $"ManaCrystal_{i}";

            // Position crystals in a row or arc
            RectTransform crystalRect = _manaCrystals[i].GetComponent<RectTransform>();
            float xPos = i * (crystalRect.rect.width + 5); // 5 is spacing
            crystalRect.anchoredPosition = new Vector2(xPos, 0);

            // Start all crystals as empty
            SetCrystalFilled(i, false, false);
        }

        // Initialize display
        UpdateManaDisplay(0, 0);
    }

    public void UpdateManaDisplay(int current, int max)
    {
        // Store values
        _currentMana = current;
        _maxMana = max;

        // Update text
        if (manaText != null)
        {
            manaText.text = $"{_currentMana}/{_maxMana}";
        }

        // Update crystals
        for (int i = 0; i < maxCrystals; i++)
        {
            bool isActive = i < _maxMana;
            bool isFilled = i < _currentMana;

            // Animate changes
            bool wasActive = _manaCrystals[i].activeSelf;
            bool wasFilled = _manaCrystals[i].GetComponent<Image>().color.b > 0.5f;

            if (isActive != wasActive || isFilled != wasFilled)
            {
                SetCrystalFilled(i, isActive, isFilled);
            }
        }
    }

    private void SetCrystalFilled(int index, bool active, bool filled)
    {
        if (index < 0 || index >= _manaCrystals.Length)
            return;

        GameObject crystal = _manaCrystals[index];
        crystal.SetActive(active);

        if (active)
        {
            Image crystalImage = crystal.GetComponent<Image>();
            Color targetColor = filled ? new Color(0, 0.5f, 1f) : new Color(0.3f, 0.3f, 0.3f);

            // Animate color change without scaling
            crystalImage.DOColor(targetColor, 0.3f);

            // Ensure crystal is at correct scale
            crystal.transform.localScale = Vector3.one;
        }
    }

    public void AnimateManaCrystalUse(int amount)
    {
        for (int i = _currentMana - 1; i >= _currentMana - amount; i--)
        {
            if (i >= 0 && i < _manaCrystals.Length)
            {
                // Flash the crystal
                GameObject crystal = _manaCrystals[i];
                Image crystalImage = crystal.GetComponent<Image>();

                Sequence sequence = DOTween.Sequence();
                sequence.Append(crystalImage.DOColor(Color.white, 0.15f));
                sequence.Append(crystalImage.DOColor(new Color(0.3f, 0.3f, 0.3f), 0.15f));

                // Shake the crystal without scaling
                crystal.transform.DOShakePosition(0.3f, 5f, 10, 90, false, true);

                // Ensure crystal is at correct scale
                crystal.transform.localScale = Vector3.one;
            }
        }
    }
}