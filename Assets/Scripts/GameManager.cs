using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using DG.Tweening;

// ================================================================================
// 25.12.23(화) 추가 작업 사항 메모
// - 룰렛 생성 시 핀이 함께 생성되도록 수정 
// - 룰렛이 시계 방향으로 회전하도록 수정 (targetZ를 잡는 부분을 음수화)
// ================================================================================ 

public class GameManager : MonoBehaviour
{
    public static GameManager inst;     // 싱글톤

    [Header("Roullette Data")]
    public Item[] itemDatas;
    float rateSum;          // Item 내 rate의 총합

    [Header("UI")]
    [SerializeField] RectTransform roulletteParent;       // 룰렛 조각의 부모 오브젝트
    [SerializeField] GameObject partPrefab;           // 룰렛 조각 프리펩
                                                      //    * Image > fill 옵션 사용
    [SerializeField] Text resultText;                 // 당첨 영역 라벨 표시
    [SerializeField] Text informationText;            // 메세지 영역 라벨 표시

    [SerializeField] Button pushStart_Btn;
    [SerializeField] Button showJson_Btn;

    [Header("Result Table")]
    [SerializeField] Text curRoundText;
    [SerializeField] Text curLabelText;
    [SerializeField] Text curValueText;
    [SerializeField] Text curRateText;
    int curRound = 0;

    [Header("Direction")]
    [SerializeField] float spinTime = 1f;             // 한 바퀴를 도는 데 걸리는 회전 시간
    public bool isSpinning = false;                // 회전 중 인지 체크 > 상태 머신 역할 플래그
    int minSpinCount = 5;                   // 연출 시 룰렛을 회전 시킬 최저 횟수
    int maxSpinCount = 10;                  // 연출 시 룰렛을 회전 시킬 최대 횟수
    // 룰렛의 다양한 회전 연출 (Dotween 라이브러리 활용)
    enum SpinPattern
    {
        //Smooth, Back, Elastic, Bounce,
        Pass, NotPass, Smooth,
        Count
    }
    float vAngle;       // 룰렛의 한 영역이 차지하는 각도
    float lastZ;        // 룰렛 각도 저장

    [Header("Pin Settings")]
    [SerializeField] GameObject pinPrefab;      // 핀을 룰렛의 시작 지점을 참조하여 생성
    [SerializeField] float pinOffset = 100f;           // 핀이 룰렛 중심으로부터 생성될 거리 계산
    float pinRadius = 5f;   // 핀 크기 정보를 담기 위한 변수
    List<RectTransform> pins = new List<RectTransform>();

    float direction = -1;       // -1 = 시계 방향
    // 감속 구간 판정
    [SerializeField] float slowThreshold = 30f;
    float smoothVel;

    // 프로퍼티
    public List<RectTransform> Pins => pins;
    public float PinRadius => pinRadius;
    public float CurZ { get; private set; }     // 룰렛의 현재 각도
    public float DeltaZ { get; private set; }   // 각도 변화량
    public bool IsSlowing { get; private set; } // 감속 구간 체크
    public float VAngle => vAngle;
    public float Direction => direction;
    public float SmoothVel => smoothVel;

    private void Awake()
    {
        if (inst == null)
            inst = this;
        else
            Destroy(gameObject);

        LoadData();
    }

    private void Start()
    {
        // 버튼 리스너 연결
        if (pushStart_Btn != null)
            pushStart_Btn.onClick.AddListener(ClickStartButton);
        if (showJson_Btn != null)
            showJson_Btn.onClick.AddListener(ShowJson);

        // UI 초기화
        curRoundText.text = "0";
        curLabelText.text = "0";
        curValueText.text = "0";
        curRateText.text = "0";
    }

    void LateUpdate()
    {
        float z = roulletteParent.eulerAngles.z;

        DeltaZ = Mathf.DeltaAngle(lastZ, z);
        CurZ = z;

        float rawVel = Mathf.Abs(CurZ) / Mathf.Max(Time.deltaTime, 0.0001f);
        smoothVel = Mathf.Lerp(smoothVel, rawVel, Time.deltaTime * 6f);

        IsSlowing = smoothVel < slowThreshold;

        lastZ = z;
    }

    void LoadData()
    {
        string filePath = Path.Combine(Application.dataPath, "Table", "rate.json");

        if (File.Exists(filePath))
        {
            // 파일 불러오기
            string jsonString = File.ReadAllText(filePath);

            // 파싱 용으로 문자열 수정
            // 배열을 { "items": [ ] } 형태로 수정 
            string jsonToParse = "{\"items\":" +jsonString + "}";

            ItemWrapper wrapper = JsonUtility.FromJson<ItemWrapper>(jsonToParse);
            itemDatas = wrapper.items;

            Debug.Log($"룰렛 항목 로드 완료 / 항목 : {itemDatas.Length} ");

            // 룰렛 정보 초기화 및 생성
            CreateRoullette();
        }
        else
        {
            Debug.Log($"json 파일 로드 실패 : {filePath} ");
        }
    }

    void CreateRoullette()
    {
        // 데이터 초기화
        rateSum = 0;

        foreach (var item in itemDatas)
            rateSum += item.rate;

        int itemCount = itemDatas.Length;           // 동적으로 생성할 룰렛 항목의 수
        vAngle = 360f / itemCount;       // 한 칸이 차지하는 공간의 각도 계산
        float imgFill = 1f / itemCount;


        // 룰렛 생성
        for(int i = 0; i < itemCount; i++)
        {
            // 실제 당첨 확률 계산
            itemDatas[i].probability = (float)itemDatas[i].rate / rateSum;

            // 각도 값 저장
            itemDatas[i].startAngle = vAngle * i;
            itemDatas[i].endAngle = vAngle * (i + 1);

            // 룰렛 조각 생성
            GameObject part = Instantiate(partPrefab, roulletteParent);

            Image img = part.GetComponent<Image>();
            if (img != null)
            {
                img.fillAmount = imgFill;
                // 구분을 위해 색상 변경
                img.color = (i % 2 == 0) ? Color.white : new Color(0.9f, 0.9f,0.9f);
            }

            // 회전
            part.transform.localRotation = Quaternion.Euler(0, 0, -vAngle * i);

            // 텍스트 설정
            Text partLabel = part.GetComponentInChildren<Text>();
            if(partLabel != null)
            {
                // 텍스트 수정 및 부채꼴 중앙으로 위치하도록 회전
                partLabel.text = itemDatas[i].label + "\n\n\n\n\n\n\n";     // 임시 
                partLabel.transform.localRotation = Quaternion.Euler(0,0, -vAngle / 2f);
            }
        }

        // 핀 생성
        pins.Clear();
        for (int i = 0; i < itemCount; i++)
        {
            if (pinPrefab != null)
            {
                GameObject pin = Instantiate(pinPrefab, roulletteParent);

                // 핀의 각도 결정 -> 룰렛의 경계면에 위치하도록 Item.startAngle 참조
                float angle = -vAngle * i;
                float rad = angle * Mathf.Deg2Rad;

                // 좌표 계산
                float x = Mathf.Sin(rad) * pinOffset;
                float y = Mathf.Cos(rad) * pinOffset;

                RectTransform rt = pin.GetComponent<RectTransform>();
                rt.localPosition = new Vector3(x, y, 0);
                rt.localRotation = Quaternion.Euler(0, 0, angle);

                pins.Add(rt);
            }
        }

        // 초기 생성 시 핀의 위치와 바늘이 겹치는 경우 룰렛을 살짝 회전시켜 어색한 연출 회피
        roulletteParent.localRotation = Quaternion.Euler(0, 0, vAngle / 2);

    }

    public void ClickStartButton()
    {
        if (isSpinning) return;

        roulletteParent.DOKill();       // Dotween 중복 호출 방지
        StartCoroutine(StartSpin());
    }

    IEnumerator StartSpin()
    {
        curRound++;
        isSpinning = true;
        informationText.text = "Waiting...";

        // 결과 생성
        Item resultItem = GetResult();
        // 회전 연출 패턴 랜덤 설정
        SpinPattern pattern = SpinPattern.Pass; //(SpinPattern)Random.Range(0, (int)SpinPattern.Count);
        // 룰렛의 회전 수 설정
        int ranSpinCount = Random.Range(minSpinCount, maxSpinCount + 1);

        // baseZ
        float baseZ = resultItem.startAngle + 180f  // 목표 위치 + 바늘 방향 보정 = 절대값
            + (360 * ranSpinCount) * direction;     // 추가 회전 수 * 방향
       
        // 연출 전까지 회전 시간 계산
        float directTime = spinTime * ranSpinCount;

        Sequence seq = DOTween.Sequence();
        float addTime = Random.Range(1f, 3f);     // 추가 바늘 연출 시간
        // 시퀀스 2 - 정지 연출
        //Debug.Log(pattern);
        switch (pattern)
        {
            // 넘길듯 말듯 넘어가는 연출
            case SpinPattern.Pass:
                {
                    float targetZ = baseZ + vAngle/2f - pinRadius;     // 최종 지점
                    float midZ = baseZ + vAngle/2f + pinRadius;     // 걸려서 멈추는 지점

                    seq.Append(roulletteParent
                        .DORotate(new Vector3(0, 0, midZ),
                        directTime, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutQuad));
                    seq.Append(roulletteParent
                        .DORotate(new Vector3(0, 0, targetZ), addTime)
                        .SetEase(Ease.InOutBack));
                }
                    
                break;

            // 넘길듯 말듯 안 넘어가는 연출
            case SpinPattern.NotPass:
                {
                    float midZ = baseZ - pinRadius*0.2f;  // 걸려서 멈추는 지점
                    float targetZ = baseZ + pinRadius; // 최종 지점
           
                    seq.Append(roulletteParent
                        .DORotate(new Vector3(0, 0, midZ)
                        , directTime, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutQuart));
                    seq.Append(roulletteParent
                        .DORotate(new Vector3(0, 0, targetZ), addTime)
                        .SetEase(Ease.OutBack));
                }
                break;

            // 힘없이 확정 > 별도의 연출 없음
            case SpinPattern.Smooth:
                {
                    float offSet = Random.Range(pinRadius, vAngle/2);
                    float targetZ = baseZ + offSet;
                    seq.Append(roulletteParent
                        .DORotate(new Vector3(0, 0, targetZ),
                        directTime, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutQuart));
                }
                break;

        }

        // 회전 중 텍스트 실시간 변경
        seq.OnUpdate(() =>
            {
                float z = roulletteParent.localRotation.eulerAngles.z;
                UpdatePointer(z);
            });

        // 연출 끝날 때까지 대기
        yield return seq.WaitForCompletion();

        // 패턴이 완료되면 상태 플래그와 결과 처리
        isSpinning = false;
        ShowResult(resultItem);
    }

    // Dotween 연출 시 매 프레임 업데이트 + 대기 시간 처리
    //IEnumerator PlayTween(Tween t, float spinZ)
    //{
    //    yield return t
    //        .OnUpdate(() => UpdateResultText(spinZ))
    //        .WaitForCompletion();
    //}

    void UpdatePointer(float spinZ)
    {
        // 결과 텍스트
        float result = Mathf.Repeat(spinZ + 180f, 360f);
        int itemIdx = Mathf.FloorToInt(result / vAngle);
        itemIdx = Mathf.Clamp(itemIdx, 0, itemDatas.Length - 1);
        resultText.text = itemDatas[itemIdx].label;
    }


    public Item GetResult()
    {
        float ranValue = Random.Range(0f, 1f);  // 랜덤 발생
        float cumulative = 0f;                  // 누적 확률 값

        foreach (var item in itemDatas)
        {
            cumulative += item.probability;
            if (ranValue <= cumulative)
                return item;
        }

        return itemDatas[0];
    }

    void ShowResult(Item targetItem)
    {
        curRoundText.text = curRound.ToString();
        curLabelText.text = targetItem.label;
        curValueText.text = targetItem.value.ToString("N0");        
        curRateText.text = targetItem.probability.ToString("F3");   // 저장해둔 실제 값 표시

        informationText.text = $"{targetItem.label}";
    }

    public void ShowJson()
    {
        string filePath = Path.Combine(Application.dataPath, "Table", "rate.json");
        
        // 파일이 존재하는 지 검사 후 실행
        if (File.Exists(filePath))
            System.Diagnostics.Process.Start("notePad.exe", filePath);
        else
            Debug.Log($"파일을 찾을 수 없습니다. : {filePath}");
    }
}
