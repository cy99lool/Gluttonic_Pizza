using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    [System.Serializable]
    public class Team
    {
        [SerializeField] TeamColor color;
        public TeamColor Color => color;

        [SerializeField] int score;
        public int Score => score;

        [Header("チームの情報UIテキスト"), SerializeField] TMPro.TextMeshProUGUI scoreText;
        public TMPro.TextMeshProUGUI ScoreText => scoreText;

        [Header("取られるまでの時間のテキスト"), SerializeField] TMPro.TextMeshProUGUI pickTimeText;
        public TMPro.TextMeshProUGUI PickTimeText => pickTimeText;

        // ----- リザルト表示 -----
        [Header("リザルト表示\nメイン画面のリザルトのスクリプト"), SerializeField] Result result;
        public Result Result => result;

        [Header("メイン画面のスコア表示オブジェクト"), SerializeField] GameObject mainResultUI;
        public GameObject MainResultUI => mainResultUI;

        [Header("〃のスコアバー"), SerializeField] RectTransform mainScoreBar;
        public RectTransform MainScoreBar => mainScoreBar;

        [Header("\nタブレット画面のスコア表示オブジェクト"), SerializeField] GameObject tabletResuiltUI;
        public GameObject TabletResuiltUI => tabletResuiltUI;

        [Header("〃のスコアバー"), SerializeField] RectTransform tabletScoreBar;
        public RectTransform TabletScoreBar => tabletScoreBar;

        // ----- リザルト表示ここまで -----

        const int DefaultBulletCountValue = 0;// 初期化するときの弾数
        const int DefaultBulletSubValue = 1;

        [SerializeField]int bulletCount;// 発射可能弾数
        public int BulletCount => bulletCount;
        public bool Shootable => bulletCount > 0;// 発射できるかどうか、後々演出で一時停止を実装するなら条件を増やす

        public void SetBulletCount(int count)// 発射可能弾数の設定
        {
            bulletCount = count;
        }
        public void ResetBulletCount()
        {
            bulletCount = DefaultBulletCountValue;
        }
        public void SubtractBullet(int value = DefaultBulletSubValue)
        {
            // 弾数を減らす
            bulletCount -= value;
            if(bulletCount < 0) bulletCount = DefaultBulletCountValue;
        }

        public void AddScore(int score)
        {
            this.score += score;
        }
    }
    [Header("メイン画面のリザルトのスクリプト"), SerializeField] Result mainResult;

    [SerializeField] List<Team> teams;
    public List<Team> Teams => teams;

    [Header("食材の発射可能弾数"), SerializeField] int reloadBulletValue = 5;

    bool isStarted;
    public bool IsStarted => isStarted;

    PizzaManager pizzaManager;

    void Start()
    {
        pizzaManager = FindObjectOfType<PizzaManager>();

        isStarted = false;

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
        for (int i = 0; i < teams.Count; i++)
        {
            // 色ごとのスコアを取得（リスト内の順番がバラバラでも問題ないように）
            if (teams[i].Color == TeamColor.Red) redScore = teams[i].Score;
            if (teams[i].Color == TeamColor.Blue) blueScore = teams[i].Score;
            if (teams[i].Color == TeamColor.Green) greenScore = teams[i].Score;
            if (teams[i].Color == TeamColor.Yellow) yellowScore = teams[i].Score;
        }

        for (int i = 0; i < teams.Count; i++)
        {
            // テキストの更新
            teams[i].ScoreText.text = $"赤:{redScore:D2}青:{blueScore:D2}\n緑:{greenScore:D2}黄:{yellowScore:D2}";
        }
    }

    // スライスを取るまでの時間を表示する
    void UpdatePickTimeUI(float time)
    {
        if (teams.Count == 0) return;
        for (var i = 0; i < teams.Count; i++)
        {
            teams[i].PickTimeText.text = $"取られるまで:\n{(int)time:D2}秒";
        }
    }

    public void OneClickOneCycle()
    {
        StartCoroutine(Main());
    }

    List<int> pickIndexes = new List<int>();

    const float RouletteTime = 1f;// ルーレット演出の長さ
    const int pickNum = 1;
    const float ShootableTime = 45f;
    const float PreparePizzaTime = 2f;// ピザ取得準備の時間
    const int PhaseCount = 3;// フェーズの数
    const int PinePhase = 2;
    const int LastPhase = 3;
    IEnumerator Main()
    {
        if (!isStarted) isStarted = true;
        int counter = GameConstants.One;

        //while (pizzaManager.PizzaSlices.Count > 0)
        while(counter <= PhaseCount)
        {
            // 発射準備フェーズ
            //yield return StartCoroutine(PizzaSelectPhase(RouletteTime, pickNum));

            // 食材発射フェーズ（デバッグ、後で名前変える）
            yield return StartCoroutine(DebugPick(ShootableTime));

            // ピザ取得待機フェーズ
            yield return StartCoroutine(PreparePickPizzaPhase(PreparePizzaTime));

            // ピザ取得フェーズ
            //yield return StartCoroutine(PickPizzaPhase());

            // フェーズ終了時処理
            yield return StartCoroutine(EndPhase(counter));

            counter++;
        }

        // リザルトフェーズ
        yield return StartCoroutine(ResultPhase());
    }

    IEnumerator PizzaSelectPhase(float rouletteTime, uint pickCount = 1)
    {
        // 確実に誰も発射できないように
        foreach (Team team in teams)
        {
            team.ResetBulletCount();
        }

        // 取得するピザの番号を取得
        pickIndexes = SelectPizzaSlices(pickCount);

        // 選ばれたピザの演出
        float timer = GameConstants.FirstTimerValue;
        while (timer < rouletteTime)
        {
            timer += Time.deltaTime;

            if (pickIndexes.Count == GameConstants.Zero) yield return null;// 取得するピザがなければ演出カット
            
            // ルーレット演出をいれる
            foreach(int index in pickIndexes)
            {
                pizzaManager.PizzaSlices[index].EnableHighlightObject();
            }

            
            yield return null;
        }
    }

    /// <summary>
    /// 取得するピザの番号を返す
    /// </summary>
    /// <param name="pickCount">取る枚数</param>
    /// <returns>取得するピザの番号リスト</returns>
    List<int> SelectPizzaSlices(uint pickCount = 1)
    {
        // 選択個数が0個なら初期化されたものを返す
        if (pickCount == GameConstants.Zero) return new List<int>();

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

            int index = Random.Range(GameConstants.Zero, pickableSlices.Count);
            pickIndexes.Add(index);
            Debug.Log($"{pickableSlices[index]}が選ばれた");

            pickableSlices.RemoveAt(index);
        }

        return pickIndexes;// 取得するピザの番号を返す
    }

    /// <summary>
    /// 食材発射フェーズ
    /// </summary>
    /// <param name="shootTime">発射可能時間</param>
    /// <returns></returns>
    IEnumerator DebugPick(float shootTime)
    {
        if (pickIndexes.Count > 0)
        {
            // 念の為再度ハイライト
            foreach (int index in pickIndexes)
            {
                pizzaManager.PizzaSlices[index].EnableHighlightObject();
            }
        }

        pizzaManager.StartSpin();// 回転開始

        // プレイヤーは発射できるように（残弾数の補充）
        foreach(Team team in teams)
        {
            team.SetBulletCount(reloadBulletValue);
        }

        float timer = GameConstants.FirstTimerValue;
        while (timer < shootTime)
        {
            timer += Time.deltaTime;
            UpdatePickTimeUI(shootTime - timer);

            // パインの召喚処理

            // ピザを取られるフェーズの処理


            // 全員が食材を発射し終えたら途中でも次のフェーズへ
            if(IsAllPlayerUnShootable()) yield break;

            yield return null;
        }
    }

    /// <summary>
    /// すべてのプレイヤーが発射不可能な状態か調べる
    /// </summary>
    /// <returns>すべてのプレイヤーが発射不可能な状態であるか</returns>
    bool IsAllPlayerUnShootable()
    {
        foreach(Team team in teams)
        {
            // 一人でも撃てるなら偽
            if (team.Shootable) return false;
        }

        return true;
    }

    IEnumerator PreparePickPizzaPhase(float preparePizzaTime)
    {
        pizzaManager.StopSpin();// 回転停止

        // 取得待機演出
        yield return StartCoroutine(pizzaManager.PrepareTakePizza(preparePizzaTime));
    }

    IEnumerator PickPizzaPhase()
    {
        if (pickIndexes.Count < 1) yield return null;

        // 取得
        Debug.Log("取得");
        pizzaManager.TakePizzaSlice(pickIndexes);
        
        yield return null;
    }

    IEnumerator EndPhase(int phaseCounter)
    {
        int nextPhase = phaseCounter++;// 次のフェーズを取得

        // パイン開始フラグを設定予定
        //if (nextPhase == PinePhase) 

        // 魔王のピック開始フラグを設定予定
        //if(nextPhase == LastPhase)

        yield return null;
    }

    IEnumerator ResultPhase()
    {
        for(int i = 0; i < teams.Count; i++)
        {
            //break;// デバッグ用
            if (teams[i].Result == null) continue;

            if (!teams[i].Result.gameObject.activeInHierarchy) teams[i].Result.gameObject.SetActive(true);
            if (!teams[i].Result.gameObject.activeInHierarchy) continue;

            yield return teams[i].Result.ShowResult();
        }

        if (mainResult.gameObject.activeInHierarchy)
        {
            yield return StartCoroutine(mainResult.ShowResult());
        }
    }
}