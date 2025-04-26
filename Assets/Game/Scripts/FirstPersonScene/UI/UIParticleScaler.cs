using UnityEngine;
using TMPro;

public class UIParticleScaler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float particleSizeMultiplier = 0.1f;
    [SerializeField] private float emissionRateMultiplier = 0.5f;

    private ParticleSystem _particles;
    private ParticleSystem.MainModule _main;
    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.ShapeModule _shape;

    private void Start()
    {
        _particles = GetComponent<ParticleSystem>();
        _main = _particles.main;
        _emission = _particles.emission;
        _shape = _particles.shape;

        if (targetText != null)
        {
            UpdateParticleScale();
        }
    }

    public void UpdateParticleScale()
    {
        // Get text dimensions
        targetText.ForceMeshUpdate();
        Vector2 textSize = targetText.rectTransform.rect.size;

        // Scale shape to match text area
        _shape.scale = new Vector3(textSize.x, textSize.y, 0.1f);

        // Scale particle size based on text size
        float textHeight = targetText.fontSize;
        _main.startSize = textHeight * particleSizeMultiplier;

        // Adjust emission rate based on text area
        float textArea = textSize.x * textSize.y;
        _emission.rateOverTime = textArea * emissionRateMultiplier * 0.001f;
    }
}