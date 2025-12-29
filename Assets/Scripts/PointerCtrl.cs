using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바늘이 핀 통과 시 항상 최대 각도까지 확 눕게 만든 버전
/// impulse 프레임 독립적 강화 + 감지 범위 넓힘
/// 느릴 때 더 꺾이는 문제 해결
/// </summary>
public class PointerCtrl : MonoBehaviour
{
    [Header("Spring Physics Settings")]
    [SerializeField] private float springStrength = 80f;          // 복귀 강도 (올려서 빠르게 복귀)
    [SerializeField] private float damping = 10f;                 // 감쇠 (낮춰서 진동 남김)
    [SerializeField] private float maxDeflectionAngle = 60f;      // 최대 꺾임 각도 (조정 가능)

    [Header("Impulse Settings (핀 통과 시 충격 강도)")]
    [SerializeField] private float impulseStrength = 300f;        // 프레임 독립적: 200~400 추천, 높을수록 세게 눕음

    [Header("Pin Interaction Settings")]
    [SerializeField] private float triggerAngleThreshold = 12f;   // 감지 범위 넓혀 빠른 속도에서도 확실히 트리거

    private GameManager gm;
    private RectTransform pointerRect;

    private float currentAngle = 0f;
    private float currentVelocity = 0f;

    private float previousRouletteZ = 0f;

    private void Awake()
    {
        pointerRect = GetComponent<RectTransform>();
        if (pointerRect == null)
        {
            Debug.LogError("[PointerCtrl] RectTransform 없음");
            enabled = false;
        }
    }

    private void Start()
    {
        gm = GameManager.inst;
        if (gm == null)
        {
            Debug.LogError("[PointerCtrl] GameManager 못 찾음");
            enabled = false;
            return;
        }

        if (pointerRect != null)
        {
            pointerRect.localRotation = Quaternion.Euler(0, 0, 0);
        }
        currentAngle = 0f;
        currentVelocity = 0f;
        previousRouletteZ = gm.CurZ;
    }

    private void LateUpdate()
    {
        if (gm == null || pointerRect == null || !gm.isSpinning)
        {
            ApplySpringForce(0f);
            UpdatePointerRotation();
            return;
        }

        float currentRouletteZ = gm.CurZ;
        float normalizedAngle = Mathf.Repeat(currentRouletteZ + 180f, 360f);
        float closestPinAngle = FindClosestNextPinAngle(normalizedAngle);

        float angleToPin = Mathf.DeltaAngle(normalizedAngle, closestPinAngle);

        bool isPassingPin = false;
        float prevNormalized = Mathf.Repeat(previousRouletteZ + 180f, 360f);
        float prevAngleToPin = Mathf.DeltaAngle(prevNormalized, closestPinAngle);

        if (prevAngleToPin > -triggerAngleThreshold &&
            angleToPin <= 0f &&
            Mathf.Abs(angleToPin) < triggerAngleThreshold)
        {
            isPassingPin = true;
        }

        if (isPassingPin)
        {
            // 프레임 독립적 강한 충격 → 항상 최대 각도 근처까지 눕게
            currentVelocity += impulseStrength * gm.Direction * -1f;
        }

        ApplySpringForce(0f);
        UpdatePointerRotation();

        previousRouletteZ = currentRouletteZ;
    }

    private void ApplySpringForce(float targetAngle)
    {
        float displacement = currentAngle - targetAngle;
        float springForce = -springStrength * displacement;
        float dampingForce = -damping * currentVelocity;

        float acceleration = springForce + dampingForce;

        currentVelocity += acceleration * Time.deltaTime;
        currentAngle += currentVelocity * Time.deltaTime;

        currentAngle = Mathf.Clamp(currentAngle, -maxDeflectionAngle, maxDeflectionAngle);
    }

    private void UpdatePointerRotation()
    {
        pointerRect.localRotation = Quaternion.Euler(0, 0, currentAngle);
    }

    private float FindClosestNextPinAngle(float currentNormalizedAngle)
    {
        float minDiff = float.MaxValue;
        float closest = 0f;
        float oneSegment = gm.VAngle;

        for (int i = 0; i < gm.itemDatas.Length; i++)
        {
            float pinAngle = i * oneSegment;
            float diff = Mathf.DeltaAngle(currentNormalizedAngle, pinAngle);

            if (diff > -oneSegment * 0.5f)
            {
                float absDiff = Mathf.Abs(diff);
                if (absDiff < minDiff)
                {
                    minDiff = absDiff;
                    closest = pinAngle;
                }
            }
        }

        return closest;
    }

}