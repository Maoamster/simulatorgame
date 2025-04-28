using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HealthDisplay : MonoBehaviour
{
    public Image healthBar;
    public TextMeshProUGUI healthText;
    public GameObject damageEffect;
    public GameObject healEffect;

    private int _maxHealth = 30;
    private int _currentHealth = 30;

    private void Start()
    {
        UpdateHealthDisplay(_currentHealth);
    }

    public void UpdateHealthDisplay(int newHealth)
    {
        // Store old health for animation
        int oldHealth = _currentHealth;
        _currentHealth = newHealth;

        // Update text
        if (healthText != null)
        {
            healthText.text = _currentHealth.ToString();
        }

        // Update health bar
        if (healthBar != null)
        {
            float healthPercent = (float)_currentHealth / _maxHealth;
            healthBar.DOFillAmount(healthPercent, 0.5f);

            // Change color based on health
            if (healthPercent < 0.3f)
                healthBar.DOColor(Color.red, 0.5f);
            else if (healthPercent < 0.6f)
                healthBar.DOColor(Color.yellow, 0.5f);
            else
                healthBar.DOColor(Color.green, 0.5f);
        }

        // Play appropriate effect
        if (newHealth < oldHealth)
        {
            PlayDamageEffect(oldHealth - newHealth);
        }
        else if (newHealth > oldHealth)
        {
            PlayHealEffect(newHealth - oldHealth);
        }
    }

    private void PlayDamageEffect(int amount)
    {
        // Shake the display
        transform.DOShakePosition(0.5f, 10f, 10, 90, false, true);

        // Show damage amount
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform);
            TextMeshProUGUI damageText = effect.GetComponentInChildren<TextMeshProUGUI>();
            if (damageText != null)
            {
                damageText.text = "-" + amount.ToString();
            }

            // Animate the damage text
            effect.transform.DOLocalMoveY(100f, 1f);
            effect.GetComponent<CanvasGroup>().DOFade(0, 1f).OnComplete(() => {
                Destroy(effect);
            });
        }
    }

    private void PlayHealEffect(int amount)
    {
        // Pulse the display
        transform.DOScale(1.1f, 0.2f).SetLoops(2, LoopType.Yoyo);

        // Show heal amount
        if (healEffect != null)
        {
            GameObject effect = Instantiate(healEffect, transform);
            TextMeshProUGUI healText = effect.GetComponentInChildren<TextMeshProUGUI>();
            if (healText != null)
            {
                healText.text = "+" + amount.ToString();
            }

            // Animate the heal text
            effect.transform.DOLocalMoveY(100f, 1f);
            effect.GetComponent<CanvasGroup>().DOFade(0, 1f).OnComplete(() => {
                Destroy(effect);
            });
        }
    }
}