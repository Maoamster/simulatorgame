using UnityEngine;

public class MouseResponseUI : MonoBehaviour
{
    [Header("Response Settings")]
    [SerializeField] private float maxRotationAngle = 3.0f; // Increased for visibility
    [SerializeField] private float responsiveness = 5.0f;
    [SerializeField] private float returnSpeed = 3.0f;
    [SerializeField] private bool invertXAxis = false;
    [SerializeField] private bool invertYAxis = false;
    [SerializeField] private float hoverScaleAmount = 1.05f;
    [SerializeField] private float positionShiftAmount = 5f;

    [Header("Mouse Detection")]
    [SerializeField] private bool onlyRespondWhenMouseOver = true;
    [SerializeField] private RectTransform detectionArea;

    private RectTransform rectTransform;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isMouseOver = false;
    private Vector2 mousePos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalRotation = rectTransform.localRotation;
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.localPosition;

        if (detectionArea == null)
            detectionArea = rectTransform;
    }

    private void Update()
    {
        // Check if mouse is over the UI
        if (onlyRespondWhenMouseOver)
        {
            isMouseOver = RectTransformUtility.RectangleContainsScreenPoint(
                detectionArea,
                Input.mousePosition,
                Camera.main);
        }
        else
        {
            isMouseOver = true;
        }

        // Get normalized mouse position (-1 to 1 range)
        mousePos.x = (Input.mousePosition.x / Screen.width) * 2 - 1;
        mousePos.y = (Input.mousePosition.y / Screen.height) * 2 - 1;

        // Apply inversion if needed
        if (invertXAxis) mousePos.x *= -1;
        if (invertYAxis) mousePos.y *= -1;

        if (isMouseOver)
        {
            // Handle scaling
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                originalScale * hoverScaleAmount,
                Time.deltaTime * responsiveness);

            // Handle position shift
            Vector3 shift = new Vector3(mousePos.x, mousePos.y, 0) * positionShiftAmount;
            rectTransform.localPosition = Vector3.Lerp(
                rectTransform.localPosition,
                originalPosition + shift,
                Time.deltaTime * responsiveness);

            // Handle rotation - create a new rotation based on mouse position
            Quaternion targetRotation = originalRotation *
                Quaternion.Euler(-mousePos.y * maxRotationAngle, mousePos.x * maxRotationAngle, 0);

            rectTransform.localRotation = Quaternion.Slerp(
                rectTransform.localRotation,
                targetRotation,
                Time.deltaTime * responsiveness);
        }
        else
        {
            // Return to original values when mouse is not over
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                originalScale,
                Time.deltaTime * returnSpeed);

            rectTransform.localPosition = Vector3.Lerp(
                rectTransform.localPosition,
                originalPosition,
                Time.deltaTime * returnSpeed);

            rectTransform.localRotation = Quaternion.Slerp(
                rectTransform.localRotation,
                originalRotation,
                Time.deltaTime * returnSpeed);
        }
    }
}