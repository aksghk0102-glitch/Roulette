using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HingeJoint2D))]
public class PointerPhysics : MonoBehaviour
{
    [Header("Motor Settings")]
    [SerializeField] private float motorForce = 2000f;      // 모터 토크 한계
    [SerializeField] private float motorSpeedRecover = 0f;  // 0도로 돌아갈 때 모터 속도
    [SerializeField] private float motorSpeedIdle = 0f;     // 충돌 없는 상태에서의 모터 속도
    [SerializeField] private float bounceAngular = 150f;    // 충돌 반동 만큼 각속도 추가

    private HingeJoint2D hinge;
    private JointMotor2D motor;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint2D>();
        hinge.useMotor = true;
    }

    private void FixedUpdate()
    {
        // 현재 각도가 0도와 다르면 모터를 0도 방향으로 설정
        float current = hinge.jointAngle;

        // 0으로 수렴할 때 부드럽게 모터 속도를 설정
        motor.motorSpeed = motorSpeedRecover;
        motor.maxMotorTorque = motorForce;
        hinge.motor = motor;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Pin"))
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            // Pin과 충돌하면 현재 angularVelocity에 반동 추가
            rb.angularVelocity += bounceAngular;
        }
    }
}
