using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager inst;     // 싱글톤

    [Header("Roullette Data")]
    public Item[] itemDatas;
    float rateSum;          // Item 내 rate의 총합

    [Header("UI")]
    public RectTransform roulletteParent;       // 룰렛 조각의 부모 오브젝트
    public GameObject partPrefab;           // 룰렛 조각 프리펩
                                            //    * Image > fill 옵션 활성화 
    public Text resultText;                 // 당첨 영역 라벨 표시
    public Text informationText;            // 메세지 영역 라벨 표시

    public Button pushStart_Btn;

    [Header("Result Table")]
    public Text curRoundText;
    public Text curLabelText;
    public Text curValueText;
    public Text curRateText;
    int curRound = 0;

    [Header("Direction")]
    public float spinTime = 5f;             // 회전 시간
    bool isSpinning = false;                // 회전 중 인지 체크 > 상태 머신 역할 플래그
    public int spinCount = 5;               // 연출 시 룰렛을 회전 시킬 횟수

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
        if (pushStart_Btn != null)
            pushStart_Btn.onClick.AddListener(ClickStartButton);

        curRoundText.text = 0.ToString();
        curLabelText.text = 0.ToString();
        curValueText.text = 0.ToString();
        curRateText.text = 0.ToString();
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
        float visualAngle = 360f / itemCount;       // 한 칸이 차지하는 공간의 각도 계산
        float imgFill = 1f / itemCount;

        for(int i = 0; i < itemCount; i++)
        {
            // 실제 당첨 확률 계산
            itemDatas[i].probability = (float)itemDatas[i].rate / rateSum;

            // 각도 값 저장
            itemDatas[i].startAngle = visualAngle * i;
            itemDatas[i].endAngle = visualAngle * (i + 1);

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
            part.transform.localRotation = Quaternion.Euler(0, 0, -visualAngle * i);

            // 텍스트 설정
            Text partLabel = part.GetComponentInChildren<Text>();
            if(partLabel != null)
            {
                // 텍스트 수정 및 부채꼴 중앙으로 위치하도록 회전
                partLabel.text = itemDatas[i].label + "\n\n\n\n\n\n\n";     // 임시 
                partLabel.transform.localRotation = Quaternion.Euler(0,0, -visualAngle / 2f);
            }
        }
    }

    public void ClickStartButton()
    {
        if (isSpinning) return;

        StartCoroutine(StartSpin());
    }

    IEnumerator StartSpin()
    {
        curRound++;
        isSpinning = true;
        informationText.text = "Waiting...";

        // 결과 생성
        Item resultItem = GetResult();

        // 목표 각도 계산
        float itemCenterAngle = (resultItem.startAngle + resultItem.endAngle) / 2;
        float targetRot = 360f * spinCount * itemCenterAngle;

        float elapsed = 0f;
        float startRot = roulletteParent.localRotation.eulerAngles.z;

        // 회전
        while (elapsed < spinTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spinTime;

            t = t * (2f - t);
            float curZ = Mathf.Lerp(0, targetRot, t);
            roulletteParent.localRotation = Quaternion.Euler(0, 0, curZ);

            yield return null;
        }

        // 
        isSpinning = false;
        informationText.text = "결과 확인";
        resultText.text = resultItem.label;
        ShowResult(resultItem);
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
        curRateText.text = targetItem.probability.ToString("F2");   // 저장해둔 실제 값 표시
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
