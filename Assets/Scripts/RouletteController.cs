using UnityEngine;

public class RouletteController : MonoBehaviour
{
    private Rigidbody2D rigid;

    [Header("Spin Settings")]
    public float maxSpeed = 1400f;          // 시작 속도 (도/초)
    public float decayFactor = 0.985f;      // 감속 계수 (0.98~0.99)
    public float snapThreshold = 60f;       // 이 속도 아래 snap 시작
    public float snapDuration = 2.5f;       // snap 시간 (길면 자연)

    private bool isSpinning;
    private float targetAngle;
    private float snapTimer;

    public bool IsSpinning => isSpinning;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        if (rigid == null)
        {
            enabled = false;
            return;
        }
        rigid.bodyType = RigidbodyType2D.Kinematic;  // 자동 Kinematic
    }

    public void StartSpin(Item resultItem)
    {
        if (isSpinning) return;

        // 목표: 결과 중심 + 8~12 바퀴 (길게 돌려 자연스럽게)
        int extraSpins = Random.Range(8, 13);
        targetAngle = -(resultItem.startAngle + 360f * extraSpins);  // 시계 방향 (-)

        rigid.angularVelocity = -maxSpeed;  // 시계 방향 시작
        isSpinning = true;
        snapTimer = 0f;
    }

    void FixedUpdate()
    {
        if (!isSpinning) return;

        float speed = Mathf.Abs(rigid.angularVelocity);

        // 지수 감속만으로 천천히 멈춤
        rigid.angularVelocity *= 0.992f;

        // 속도가 거의 0이면 정지 처리
        if (speed < 5f)
        {
            rigid.angularVelocity = 0f;
            isSpinning = false;
        }
    }

}