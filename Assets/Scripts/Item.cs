using System;


[Serializable]
public class Item
{
    public string label;        // 룰렛 항목의 이름
    public int value;           // 당첨 시 적용되는 배율
    public int rate;            // 당첨 확률 값 (json 파일 내 모든 항목을 더한 후 계산 시 활용)

    [NonSerialized] public float probability;   // 최종 계산된 확률 저장

    // 룰렛의 시작과 끝 각도를 저장
    [NonSerialized] public float startAngle;
    [NonSerialized] public float endAngle;
}

// json 파싱 후 배열 저장을 위한 래퍼
public class ItemWrapper
{
    public Item[] items;
}