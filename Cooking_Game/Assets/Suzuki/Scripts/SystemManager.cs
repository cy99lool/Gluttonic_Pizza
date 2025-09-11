using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    [System.Serializable]
    public class Team
    {
        [SerializeField] FoodMove.TeamColor color;
        public FoodMove.TeamColor Color => color;

        [SerializeField] int score;
        public int Score => score;
        [Header("チームの情報UIテキスト"), SerializeField] TMPro.TextMeshProUGUI scoreText;
        public TMPro.TextMeshProUGUI ScoreText => scoreText;
        [Header("取られるまでの時間のテキスト"), SerializeField] TMPro.TextMeshProUGUI pickTimeText;
        public TMPro.TextMeshProUGUI PickTimeText => pickTimeText;

        public void AddScore(int score)
        {
            this.score += score;
        }
    }

    [SerializeField] List<Team> teams;

    public List<Team> Teams => teams;

    PizzaManager pizzaManager;

    void Start()
    {
        pizzaManager = FindObjectOfType<PizzaManager>();

        //StartCoroutine(Main());
    }

    void Update()
    {
        // UIを更新
        UpdateScoreUI();
    }

    /// <summary>
    /// UIの更新
    /// </summary>
    void UpdateScoreUI()
    {
        if (teams.Count == 0) return;
        int redScore = 0, blueScore = 0, greenScore = 0, yellowScore = 0;
        for(int i = 0; i < teams.Count; i++)
        {
            // 色ごとのスコアを取得（リスト内の順番がバラバラでも問題ないように）
            if (teams[i].Color == FoodMove.TeamColor.Red) redScore = teams[i].Score;
            if (teams[i].Color == FoodMove.TeamColor.Blue) blueScore = teams[i].Score;
            if (teams[i].Color == FoodMove.TeamColor.Green) greenScore = teams[i].Score;
            if (teams[i].Color == FoodMove.TeamColor.Yellow) yellowScore = teams[i].Score;
        }

        for(int i = 0; i < teams.Count; i++)
        {
            // テキストの更新
            teams[i].ScoreText.text = $"赤:{redScore:D2}青:{blueScore:D2}\n緑:{greenScore:D2}黄:{yellowScore:D2}";
        }
    }

    // スライスを取るまでの時間を表示する
    void UpdatePickTimeUI(float time)
    {
        if (teams.Count == 0) return;
        for(var i = 0; i < teams.Count; i++)
        {
            teams[i].PickTimeText.text = $"取られるまで:\n{(int)time:D2}秒";
        }
    }

    IEnumerator Main()
    {
        while (pizzaManager.PizzaSlices.Count > 0)
        {
            // ピザの取得（デバッグ）
            yield return DebugPick(30f, 1);
        }
    }

    public void OneClickOneCycle()
    {
        StartCoroutine(DebugPick(30f, 1));
    }

    IEnumerator DebugPick(float pickTime, uint pickCount = 1)
    {
        pizzaManager.StartSpin();// 回転開始

        // 選択個数がピザ切れの総数より多かった場合は、ピザ切れの総数にする
        if (pickCount > pizzaManager.PizzaSlices.Count) pickCount = (uint)pizzaManager.PizzaSlices.Count;

        //int pickIndex = Random.Range(0, pizzaManager.PizzaSlices.Count);
        List<int> pickIndexes = new List<int>();
        List<PizzaSlice> pickableSlices = new List<PizzaSlice>();
        //pickableSlices = pizzaManager.PizzaSlices;
        for (int i = 0; i < pizzaManager.PizzaSlices.Count; i++)
        {
            pickableSlices.Add(pizzaManager.PizzaSlices[i]);
        }

        // 取る個数分取る場所を指定
        for (int i = 0; i < pickCount; i++)
        {
            if (i > pickableSlices.Count) break;

            int index = Random.Range(0, pickableSlices.Count);
            pickIndexes.Add(index);
            Debug.Log($"{pickableSlices[index]}が選ばれた");

            pickableSlices.RemoveAt(index);
        }

        float timer = 0f;
        while (timer < pickTime)
        {
            timer += Time.deltaTime;
            UpdatePickTimeUI(pickTime - timer);
            yield return null;
        }

        Debug.Log("取得");
        pizzaManager.TakePizzaSlice(pickIndexes);
        pizzaManager.StopSpin();// 回転停止
    }
}