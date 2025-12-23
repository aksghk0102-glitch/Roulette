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
    bool isSpinning = false;                // 회전 중 인지 체크 > 상태 머신 역할 플래그
    int minSpinCount = 5;                   // 연출 시 룰렛을 회전 시킬 최저 횟수
    int maxSpinCount = 10;                  // 연출 시 룰렛을 회전 시킬 최대 횟수
    // 룰렛의 다양한 회전 연출 (Dotween 라이브러리 활용)
    enum SpinPattern
    {
        //Smooth, Back, Elastic, Bounce,
        Pass, NotPass, Smooth,      // 각각 과제 경우의 수 
        Count
    }
    float vAngle;       // 룰렛의 한 영역이 차지하는 각도

    // 25.12.23. 추가
    [Header("Pin Settings")]
    [SerializeField] GameObject pinPrefab;      // 핀을 룰렛의 시작 지점을 참조하여 생성
    [SerializeField] float pinOffset = 100f;           // 핀이 룰렛 중심으로부터 생성될 거리 계산
    List<GameObject> pins = new List<GameObject>(); // 핀 추적을 용이하게 하기 위해 리스트 생성

    // 추가 연출용 변수
    int lastItemIdx = -1;                   // 바늘이 마지막으로 가리킨 인덱스값
    [SerializeField] RectTransform pointer; // 바늘
    [SerializeField] float bounceAngle = 20f;   // 바늘이 핀에 튕기는 각도
    [SerializeField] float returnSpeed = 0.1f;  // 바늘이 제자리로 돌아오는 속도

    float direction = -1;       // -1 = 시계 방향 : 코드 의도를 알기 어려워져서 추가

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
        float z = roulletteParent.localRotation.eulerAngles.z;
        UpdatePointer(z);
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
        rateSum = 0;

        //roulletteParent.position = roulletteCenter.position;

        foreach (var item in itemDatas)
            rateSum += item.rate;

        int itemCount = itemDatas.Length;           // 동적으로 생성할 룰렛 항목의 수
        vAngle = 360f / itemCount;       // 한 칸이 차지하는 공간의 각도 계산
        float imgFill = 1f / itemCount;

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

        // 25.12.23. 추가
        for (int i = 0; i < itemCount; i++)     // 생성 순서를 후순위로 미루기 위해 별도의 for문 사용
        {
            if (pinPrefab != null)
            {
                GameObject pin = Instantiate(pinPrefab, roulletteParent);

                // 핀의 각도 결정 -> 룰렛의 경계면에 위치하도록 Item.startAngle 참조
                float angle = -vAngle * i;

                // 좌표 계산
                float rad = angle * Mathf.Deg2Rad;
                float x = Mathf.Sin(rad) * pinOffset;
                float y = Mathf.Cos(rad) * pinOffset;

                pin.transform.localPosition = new Vector3(x, y, 0);
                pin.transform.localRotation = Quaternion.Euler(0, 0, angle);

                pins.Add(pin);
            }
        }

        // 초기 생성 시 핀의 위치와 바늘이 겹치는 경우 룰렛을 살짝 회전시켜 어색한 연출 회피
        if (Mathf.Approximately(180f % vAngle, 0f)) // 부동소수 오차 방지
                                                    // vAngle이 나누어 떨어지는 경우 핀 위치가 겹침 
        {
            roulletteParent.localRotation = Quaternion.Euler(0, 0, vAngle / 2);
        }

        // 25.12.23. 추가

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
        SpinPattern pattern = (SpinPattern)Random.Range(0, (int)SpinPattern.Count);
        
        // 룰렛의 회전 수 설정
        int ranSpinCount = Random.Range(minSpinCount, maxSpinCount + 1);
        
        // 생성된 결과 범위 내에 도착 지점 offset 설정
        // 기본 도착 지점 startAngle에 0 ~ 영역 내 최대 각도를 더함
        float offSet = Random.Range(5f, vAngle - 5f);       // 핀과 겹치지 않는 위치로 조정

        // 목표 각도 계산     // 25.12.23. 시계방향으로 회전하게 수정
        float curZ = roulletteParent.localRotation.eulerAngles.z;
        float targetZ = (((curZ - (curZ % 360f))               // 수정 현재 각도 정렬
            + (360f * ranSpinCount)                        // 연출용 회전 수 적용
            + resultItem.startAngle + offSet)) * direction;// 시계 방향으로 회전하도록 *-1
        
        //// 시퀀스 분리 지점 설정 : 목적지의 20%를 남기고 연출 시작
        //float stopZ = targetZ - vAngle * 0.4f;
        // 총 연출 시간 계산
        float directTime = spinTime * ranSpinCount;
        float mainDurTime = directTime * 0.8f;
        float lastDurTime = directTime * 0.2f;

        // 시퀀스 1 - 메인 회전 : 연출 지점 전까지 감속하며 회전
        float mainStopZ = targetZ - 10f * direction;// 시퀀스 1 지점. 도착 10도 전 * 방향값

        Sequence seq = DOTween.Sequence();

        seq.Append(roulletteParent
            .DORotate(new Vector3(0, 0, mainStopZ),     // 목표 지점
            directTime, RotateMode.FastBeyond360)   // FastBeyond360 : 360도를 초과해 여러 바퀴 회전 시 사용
            .SetEase(Ease.OutCubic));     // 감속


        // 시퀀스 오버랩 지점 설정
        //float overlapTime = directTime - 0.1f;

        // 시퀀스 2 - 정지 연출
        Debug.Log(pattern);
        switch (pattern)
        {
            #region 수정 전 사항
            //// 정직하게 감속
            //case SpinPattern.Smooth:
            //    seq.Append (roulletteParent
            //        .DORotate(new Vector3(0, 0, targetZ),
            //        1.2f, RotateMode.Fast)      // 0.6f : 연출 시간
            //                                    // Fast : -180~180 범위 내의 가까운 방향으로 회전
            //        .SetEase(Ease.OutCubic)     // OutCubic : 반동 없이 일정하게 감속
            //        );
            //    break;
            //
            //// 목표 지점을 조금 초과한 뒤 돌아오는 연출
            //case SpinPattern.Back:
            //    float backOffset = vAngle * Random.Range(0.7f, 1.1f);
            //
            //    seq.Append(roulletteParent
            //        .DORotate(new Vector3(0, 0, targetZ + backOffset),
            //        0.4f, RotateMode.Fast)     // 0.25f : 연출 시간
            //        .SetEase(Ease.OutQuad)      // OutQuad : 목표 속도에 빠르게 도달 후 감속
            //        );
            //
            //    seq.Append(roulletteParent
            //        .DORotate(new Vector3(0, 0, targetZ),
            //        0.5f, RotateMode.Fast)     // 0.25f : 연출 시간
            //        .SetEase(Ease.InOutCubic)    // InOutQuad : 시작/종료 시 InOutCubic보다 완만하게 가/감속
            //        );
            //
            //    break;
            //
            //case SpinPattern.Elastic:
            //    // 튕기는 듯한 연출
            //    seq.Append(roulletteParent
            //        .DORotate(new Vector3(0, 0, targetZ),
            //        1.4f, RotateMode.Fast)       //  0.6f : 연출 시간       
            //        .SetEase(Ease.OutElastic, 0.6f, 0.3f)  // OutElastic : 0.6f의 진폭을 0.3f 간격으로 진동 효과 발생
            //        );
            //    break;
            //
            //case SpinPattern.Bounce:
            //    seq.Append(roulletteParent
            //         .DORotate(new Vector3(0, 0, targetZ),
            //         0.8f, RotateMode.Fast)     // 0.5f : 연출 시간
            //         .SetEase(Ease.OutBounce)   // OutBounce : 통통 튕기는 감속 반동 발생
            //         );
            //    break;
            #endregion
            // 넘길듯 말듯 넘어가는 연출
            case SpinPattern.Pass:
                seq.Append(roulletteParent
                    .DORotate(new Vector3(0, 0, targetZ), directTime * 0.2f)
                    .SetEase(Ease.OutQuart));
                break;

            // 넘길듯 말듯 안 넘어가는 연출
            case SpinPattern.NotPass:
                seq.Append(roulletteParent
                    .DORotate(new Vector3(0, 0, targetZ), directTime * 0.2f)
                    .SetEase(Ease.OutQuart));
                break;

            // 힘없이 확정
            case SpinPattern.Smooth:
                seq.Append(roulletteParent
                    .DORotate(new Vector3(0, 0, targetZ), directTime * 0.2f)
                    .SetEase(Ease.OutQuart));
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

        // 바늘 위치 최종 보정
        pointer.DORotate(Vector3.zero, 1f).SetEase(Ease.OutBack);

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
        if (itemDatas == null || itemDatas.Length == 0)
            return;

        float normalizedAngle = Mathf.Repeat(spinZ, 360f);  // 0~360도 범위로 정규화

        // 바늘 연출 => 25.12.23. 수정 : 바늘의 각도를 핀에 구속시켜 물리적으로 움직이는 것처럼 연출

        // 가장 가까운 핀과 거리 계산
        float dist = Mathf.Repeat(normalizedAngle + 180f, vAngle);      // + 180f: 바늘이 있는 지점
        float validRange = 5f;     // 핀이 바늘을 밀어내기 시작하는 거리 설정

        if(dist < validRange)
        {
            pointer.DOKill();
            float pushPower = 1f - (dist / validRange);
            float targetAngle = bounceAngle * pushPower;

            pointer.localRotation = Quaternion.Euler(0,0,targetAngle);
        }
        else
        {
            if (pointer.localRotation.eulerAngles.z > 0.1f && !DOTween.IsTweening(pointer))
                pointer.DORotate(Vector3.zero, returnSpeed)
                    .SetEase(Ease.OutElastic);
        }

        float result = Mathf.Repeat(normalizedAngle + 180f, 360f);
        int itemIdx = Mathf.FloorToInt(result / vAngle);
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
