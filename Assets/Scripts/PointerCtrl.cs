using UnityEngine;

// UI 바늘 컨트롤러
// 1) 룰렛에 설치된 핀과 충돌 시 물리적으로 튕김
// 2) 충돌이 끝나면 스프링으로 원위치 복귀
// 3) 룰렛 결과 계산에는 관여하지 않음

[RequireComponent(typeof(Rigidbody2D))]
public class PointerCtrl : MonoBehaviour
{
    [Header("Spring Settings")]
    [SerializeField] private float springStrength = 150f;   // 복원력
    [SerializeField] private float damping = 12f;           // 감쇠
    [SerializeField] private float maxAngle = 60f;          // 최대 회전 각

    private RectTransform rect;
    private Rigidbody2D rb;

    private float currentAngle;
    private float angularVelocity;

    private bool isColliding = false; // 핀에 밀리고 있는 중인지

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        rb = GetComponent<Rigidbody2D>();

        // UI용 Rigidbody 세팅
        rb.gravityScale = 0f;
        rb.freezeRotation = false;
        rb.angularDrag = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 핀과 충돌 시
        if (collision.collider.CompareTag("RoulettePin"))
        {
            isColliding = true;

            // 충돌 지점의 상대 속도를 기준으로 회전 토크 부여
            float impactForce = collision.relativeVelocity.magnitude;
            rb.AddTorque(-impactForce * 5f, ForceMode2D.Impulse);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("RoulettePin"))
        {
            isColliding = false;

            // 물리 각속도를 스프링 시스템으로 인계
            angularVelocity = rb.angularVelocity;
        }
    }

    private void LateUpdate()
    {
        // 충돌 중에는 물리 엔진에 전적으로 맡김
        if (isColliding)
            return;

        // 현재 회전 각도 추출
        currentAngle = NormalizeAngle(rect.localEulerAngles.z);

        // 스프링 복원 계산
        float displacement = currentAngle;
        float springForce = -springStrength * displacement;
        float dampingForce = -damping * angularVelocity;

        angularVelocity += (springForce + dampingForce) * Time.deltaTime;
        currentAngle += angularVelocity * Time.deltaTime;

        currentAngle = Mathf.Clamp(currentAngle, 0f, maxAngle);

        // 물리 영향 제거 후 직접 회전 적용
        rb.angularVelocity = 0f;
        rect.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
