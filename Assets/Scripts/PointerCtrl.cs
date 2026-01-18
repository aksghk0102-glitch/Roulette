using UnityEngine;

// ¹Ù´Ã¿¡ ºÙÀÌ´Â ÄÄÆ÷³ÍÆ®
public class PointerCtrl : MonoBehaviour
{
    [Header("Spring Physics")]
    [SerializeField] private float springStrength = 180f;
    [SerializeField] private float damping = 18f;

    private RectTransform pointerRect;

    private float currentAngle = 0f;
    private float currentVelocity = 0f;
    private bool isPushedThisFrame = false;

    private const float MAX_POINTER_ANGLE = 60f;

    private void Awake()
    {
        pointerRect = GetComponent<RectTransform>();
    }

    // ÇÉÀÌ ´ê¾ÒÀ» ¶§ "Æ¨±è" ÀÌº¥Æ®
    public void OnPinHit(float hitPower)
    {
        currentVelocity = -hitPower; // ¹Ý´ë¹æÇâÀ¸·Î Æ¨±è
    }

    // GameManager¿¡¼­ ¸Å ÇÁ·¹ÀÓ Àü´Þ
    public void UpdatePointer(float pushRatio)
    {
        if (pushRatio > 0.001f)
        {
            float force = pushRatio * springStrength;
            currentVelocity += force * Time.deltaTime;
            isPushedThisFrame = true;
        }
        else
        {
            isPushedThisFrame = false;
        }
    }

    private void LateUpdate()
    {
        // ½ºÇÁ¸µ º¹±Í
        float targetAngle = 0f;

        float displacement = currentAngle - targetAngle;
        float springForce = -springStrength * displacement;
        float dampingForce = -damping * currentVelocity;

        currentVelocity += (springForce + dampingForce) * Time.deltaTime;
        currentAngle += currentVelocity * Time.deltaTime;

        float finalAngle = Mathf.Clamp(currentAngle, 0f, MAX_POINTER_ANGLE);
        pointerRect.localRotation = Quaternion.Euler(0, 0, finalAngle);
    }
}
