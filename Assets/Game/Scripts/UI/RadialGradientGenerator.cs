using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RadialGradientGenerator : MonoBehaviour
{
    [Header("Gradient Settings")]
    [SerializeField] private int textureSize = 256;
    [SerializeField] private Color centerColor = Color.white;
    [SerializeField] private Color outerColor = Color.clear;

    [Header("Pulse Settings")]
    [SerializeField] private float minAlpha = 0.2f;
    [SerializeField] private float maxAlpha = 0.6f;
    [SerializeField] private float pulseDuration = 2.0f; // Time in seconds for a complete pulse cycle
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Smoothing curve

    private Image glowImage;
    private Color currentColor;

    private void Awake()
    {
        // Get the Image component
        glowImage = GetComponent<Image>();
        if (glowImage != null)
        {
            // Create and apply the radial gradient
            glowImage.sprite = CreateRadialGradient();

            // Set the Image Type to Simple
            glowImage.type = Image.Type.Simple;

            // Disable raycast for the glow effect
            glowImage.raycastTarget = false;

            // Store the initial color
            currentColor = glowImage.color;

            // Start the pulsing effect
            StartCoroutine(PulseGlow());
        }
    }

    private Sprite CreateRadialGradient()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize);

        float centerX = textureSize / 2f;
        float centerY = textureSize / 2f;
        float maxDistance = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distanceFromCenter = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                float normalizedDistance = Mathf.Clamp01(distanceFromCenter / maxDistance);

                Color pixelColor = Color.Lerp(centerColor, outerColor, normalizedDistance);
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator PulseGlow()
    {
        float time = 0;

        while (true)
        {
            // Calculate the normalized time (0 to 1) within the pulse cycle
            time += Time.deltaTime;
            float normalizedTime = (time % pulseDuration) / pulseDuration;

            // Apply the animation curve for smoother pulsing
            float curveValue = pulseCurve.Evaluate(normalizedTime);

            // Calculate the current alpha value
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, curveValue);

            // Update the image color with the new alpha
            currentColor.a = alpha;
            glowImage.color = currentColor;

            yield return null;
        }
    }
}