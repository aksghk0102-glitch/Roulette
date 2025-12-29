using UnityEngine;

public class PointerCtrl : MonoBehaviour
{
    [Header("Spring Physics")]
    [SerializeField] private float springStrength = 100f;
    [SerializeField] private float damping = 10f;

    private GameManager gm;
    private RectTransform pointerRect;

    private float maxDeflectionAngle;
    private float currentAngle = 0f;
    private float currentVelocity = 0f;

    private void Start()
    {
        gm = GameManager.inst;
        pointerRect = GetComponent<RectTransform>();

        InitializePhysics();
    }

    private void InitializePhysics()
    {
        // 바늘 하단 피벗 기준 전체 길이
        float pointerLength = pointerRect.sizeDelta.y;

        // [프로퍼티 참조] 핀 배치 거리 + 핀 자체의 반지름
        float escapeDistance = gm.PinOffset + gm.PinRadius;

        if (pointerLength > escapeDistance)
        {
            // 바늘 끝이 핀의 최외곽 경계를 넘어야 하는 물리적 임계 각도
            float cosVal = escapeDistance / pointerLength;
            maxDeflectionAngle = Mathf.Acos(cosVal) * Mathf.Rad2Deg;
        }
        else
        {
            maxDeflectionAngle = 10f;
        }
    }

    private void LateUpdate()
    {
        // [프로퍼티 참조] gm.isSpinning 대신 관련 상태 확인 가능하나 
        // 기존 흐름 유지를 위해 gm 존재 여부 우선 확인
        if (gm == null)
        {
            ApplySpringForce(0f);
            UpdatePointerRotation();
            return;
        }

        // 180도(정점) 기준 가장 가까운 핀 파악
        float normalizedRoulette = Mathf.Repeat(gm.CurZ + 180f, 360f);
        float closestPin = FindClosestPin(normalizedRoulette);
        float angleToPin = Mathf.DeltaAngle(normalizedRoulette, closestPin);

        // 핀의 물리적 범위 내에 있는지 확인
        bool isInRange = angleToPin > -2f && angleToPin < maxDeflectionAngle;

        // [프로퍼티 참조] gm.Direction 사용
        if (gm.Direction < 0 && isInRange)
        {
            // 핀의 중심 좌표 계산 (Local Space)
            float rad = angleToPin * Mathf.Deg2Rad;
            // [프로퍼티 참조] gm.PinOffset
            float pinX = Mathf.Sin(rad) * gm.PinOffset;
            float pinY = Mathf.Cos(rad) * gm.PinOffset;

            // [프로퍼티 참조] gm.PinRadius (두께 보정)
            // Atan2를 통해 바늘이 핀의 옆면을 정확히 추적하여 깊게 눕도록 계산
            float targetAngle = Mathf.Atan2(pinX + gm.PinRadius, pinY) * Mathf.Rad2Deg;

            currentAngle = Mathf.Max(currentAngle, targetAngle);
            currentVelocity = 0f;
        }
        else
        {
            ApplySpringForce(0f);
        }

        UpdatePointerRotation();
    }

    private void ApplySpringForce(float targetAngle)
    {
        float displacement = currentAngle - targetAngle;
        float springForce = -springStrength * displacement;
        float dampingForce = -damping * currentVelocity;
        float acceleration = springForce + dampingForce;

        currentVelocity += acceleration * Time.deltaTime;
        currentAngle += currentVelocity * Time.deltaTime;

        // 물리적 최대 굴절치 제한
        currentAngle = Mathf.Clamp(currentAngle, -5f, maxDeflectionAngle + 5f);
    }

    private void UpdatePointerRotation()
    {
        pointerRect.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private float FindClosestPin(float currentAngle)
    {
        float minDiff = float.MaxValue;
        float closest = 0f;

        for (int i = 0; i < gm.Pins.Count; i++)
        {
            // [프로퍼티 참조] gm.VAngle
            float pinAngle = i * gm.VAngle;
            float diff = Mathf.DeltaAngle(currentAngle, pinAngle);
            if (Mathf.Abs(diff) < minDiff)
            {
                minDiff = Mathf.Abs(diff);
                closest = pinAngle;
            }
        }
        return closest;
    }
}