using UnityEngine;

// 바늘에 붙이는 컴포넌트

public class PointerCtrl : MonoBehaviour
{
    [Header("Spring Physics")]
    [SerializeField] private float springStrength = 150f; // 복원력 강화
    [SerializeField] private float damping = 12f;         // 저항값 최적화

    private RectTransform pointerRect;
    private const float MAX_POINTER_ANGLE = 60f;

    private float currentAngle = 0f;
    private float currentVelocity = 0f;
    private bool isPushedThisFrame = false;

    private void Awake()
    {
        pointerRect = GetComponent<RectTransform>();
    }

    // GameManager에서 호출 (주입 즉시 반영)
    public void UpdatePointer(float pushRatio)
    {
        if (pushRatio > 0.001f)
        {
            isPushedThisFrame = true;

            // 비율에 따른 목표 각도 계산
            float targetAngle = pushRatio * MAX_POINTER_ANGLE;

            // 반응성을 위해 보간 없이 즉시 추종하거나 매우 높은 속도로 따라감
            currentAngle = targetAngle;
            currentVelocity = 0f;

            // 주입된 프레임에서 바로 회전 적용
            ApplyRotation();
        }
        else
        {
            isPushedThisFrame = false;
        }
    }

    private void LateUpdate() // 룰렛 이동이 끝난 후 최종 연산
    {
        // 핀의 압박이 없는 상태에서만 탄성 복귀 수행
        if (!isPushedThisFrame)
        {
            ApplySpringForce(0f);
            ApplyRotation();
        }
    }

    private void ApplySpringForce(float targetAngle)
    {
        float displacement = currentAngle - targetAngle;
        float springForce = -springStrength * displacement;
        float dampingForce = -damping * currentVelocity;

        currentVelocity += (springForce + dampingForce) * Time.deltaTime;
        currentAngle += currentVelocity * Time.deltaTime;
    }

    private void ApplyRotation()
    {
        float finalAngle = Mathf.Clamp(currentAngle, 0f, MAX_POINTER_ANGLE + 10f);
        pointerRect.localRotation = Quaternion.Euler(0, 0, finalAngle);
    }
}