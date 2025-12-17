using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager inst;     // 싱글톤

    [Header("Roullette Data")]
    public Item[] itemDatas;
    float rateSum;          // Item 내 rate의 총합

    private void Awake()
    {
        if (inst == null)
            inst = this;
        else
            Destroy(gameObject);

        LoadData();
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

            // 확률 계산 함수 실행
            CalculateProbability();
        }
        else
        {
            Debug.Log($"json 파일 로드 실패 : {filePath} ");
        }
    }

    void CalculateProbability()
    {
        rateSum = 0;

        foreach (var item in itemDatas)
            rateSum += item.rate;

        float curAngle = 0;

        foreach (var item in itemDatas)
        {
            // 각 항목의 실제 확률값 초기화
            item.probability = item.rate / rateSum;

            // 룰렛에 
            float angleSize = item.probability * 360f;

            
        }
    }

    public void ShowJson()
    {
        string filePath = Path.Combine(Application.dataPath, "Table", "rate.json");
        System.Diagnostics.Process.Start("notePad.exe", filePath);
    }
}
