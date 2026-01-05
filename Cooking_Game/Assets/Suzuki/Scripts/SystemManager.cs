using System.Collections;
using System.Collections.Generic;
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

        [SerializeField] int explosionScore;
        public int ExplosionScore => explosionScore;

        [Header("発射後のクールタイム"), SerializeField] float shootCT;

        [Header("チームの情報UIテキスト"), SerializeField] TMPro.TextMeshProUGUI scoreText;
        public TMPro.TextMeshProUGUI ScoreText => scoreText;

        [Header("取られるまでの時間のテキスト"), SerializeField] TMPro.TextMeshProUGUI pickTimeText;
        public TMPro.TextMeshProUGUI PickTimeText => pickTimeText;

        // ----- リザルト表示 -----
        [Header("--- リザルト表示 ---")]
        //[Header("メイン画面")]
        //[Header("メイン画面のリザルトのスクリプト"), SerializeField] Result mainResult;
        //public Result MainResult => mainResult;

        //[Header("メイン画面のスコア表示オブジェクト"), SerializeField] GameObject mainResultUI;
        //public GameObject MainResultUI => mainResultUI;

        //[Header("〃のスコアバー"), SerializeField] RectTransform mainScoreBar;
        //public RectTransform MainScoreBar => mainScoreBar;

        [Header("タブレット画面")]
        [Header("タブレット画面のリザルトのスクリプト"), SerializeField] Result tabletResult;
        public Result TabletResult => tabletResult;
        [Header("タブレット画面のスコア表示オブジェクト"), SerializeField] GameObject tabletResuiltUI;
        public GameObject TabletResuiltUI => tabletResuiltUI;

        [Header("〃のスコアバー"), SerializeField] RectTransform tabletScoreBar;
        public RectTransform TabletScoreBar => tabletScoreBar;

        // ----- リザルト表示ここまで -----

        float shootableTimer = GameConstants.FirstTimerValue;
        public float ShootableTimer => shootableTimer;

        public void SetUnshootable() => shootable = false;
        public void SetShootable() => shootable = true;

        GamePhase phase;// ゲームの進行状況
        public GamePhase Phase => phase;

        /// <summary>
        /// 発射CTを設定
        /// </summary>
        public void SetShootCT()
        {
            shootableTimer = shootCT;
        }

        public void ResetShootCT()
        {
            shootableTimer = GameConstants.Zero;
        }

        public void SubstractCT(float daltaTime)
        {
            shootableTimer -= daltaTime;

            if (Shootable) ResetShootCT();
        }
        bool shootable;
        public bool Shootable => shootable && shootableTimer <= GameConstants.Zero;// 発射できるかどうか、後々演出で一時停止を実装するなら条件を増やす

        public void AddScore(int score)
        {
            this.score += score;
        }

        public void AddExplosionScore(int score) => this.explosionScore += score;

        /// <summary>
        /// フェーズ開始処理
        /// </summary>
        /// <param name="phase">開始するフェーズ</param>
        public void StartPhase(GamePhase phase)
        {
            // フェーズを更新
            this.phase = phase;

            switch (phase)
            {
                case GamePhase.ConnectPhase:
                    break;
                case GamePhase.GameStart:
                    break;
                case GamePhase.InGame:
                    StartInGamePhase();
                    break;
                case GamePhase.PickPizza:
                    StartPickPizzaPhase();
                    break;
                case GamePhase.Result:
                    StartResultPhase();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// インゲームのフェーズを開始
        /// </summary>
        void StartInGamePhase()
        {
            // 発射可能にする
            SetShootable();
            ResetShootCT();
        }
        /// <summary>
        /// ピザを取るフェーズを開始
        /// </summary>
        void StartPickPizzaPhase()
        {
            // 発射不可能にする
            SetUnshootable();
        }
        /// <summary>
        /// リザルトフェーズを開始
        /// </summary>
        void StartResultPhase()
        {
            // 発射不可能にする
            SetUnshootable();

            if (TabletResult == null) return;

            // 有効化
            if (!TabletResult.gameObject.activeInHierarchy) TabletResult.gameObject.SetActive(true);
            if (!TabletResult.gameObject.activeInHierarchy) return;

            // リザルト表示
            TabletResult.StartCoroutine(TabletResult.ShowResult());
        }
    }
    [Header("メイン画面のリザルトのスクリプト"), SerializeField] Result mainResult;
    [Header("サウンドマネージャー"), SerializeField] SoundManager soundManager;

    [SerializeField] List<Team> teams;
    public List<Team> Teams => teams;

    bool isStarted;
    public bool IsStarted => isStarted;

    PizzaManager pizzaManager;
    GamePhase currentPhase;
    public GamePhase CurrentPhase => currentPhase;

    void Start()
    {
        pizzaManager = FindObjectOfType<PizzaManager>();

        isStarted = false;

        // フェーズの初期化（接続待ちフェーズに）
        currentPhase = GamePhase.ConnectPhase;

        SetAllPlayerShootable(teams);

        // 接続画面のBGMを再生（Windowsのみ）
        PlayBGM_Windows(BGMType.ConnectLobby);

        //StartCoroutine(Main());
    }

    /// <summary>
    /// Windows環境のみBGMを再生
    /// </summary>
    /// <param name="bgmType">再生する種類</param>
    void PlayBGM_Windows(BGMType bgmType)
    {
        // BGMを再生
        if (soundManager != null && (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)) soundManager.PlayBGM(bgmType);
    }

    /// <summary>
    /// Windows環境のみSEを再生
    /// </summary>
    /// <param name="soundType">再生する種類</param>
    /// <param name="playTransform">再生位置</param>
    public void PlaySE_Windows(PlayerSoundType soundType, Transform playTransform)
    {
        // BGMを再生
        if (soundManager != null && (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)) soundManager.PlaySE(soundType, playTransform);
    }

    /// <summary>
    /// Android環境のみSEを再生
    /// </summary>
    /// <param name="soundType">再生する種類</param>
    /// <param name="playTransform">再生位置</param>
    public void PlaySE_Android(PlayerSoundType soundType, Transform playTransform)
    {
#if UNITY_ANDROID
        // BGMを再生
        if (soundManager != null) soundManager.PlaySE(soundType, playTransform);
#endif
    }

    void Update()
    {
        // UIを更新
        UpdateScoreUI();
    }

    void FixedUpdate()
    {
        UpdateShootCT();
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

    void UpdateShootCT()
    {
        foreach (Team team in teams)
        {
            if (!team.Shootable) team.SubstractCT(Time.deltaTime);
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

    /// <summary>
    /// ゲームの進行状況、同期に使用する
    /// </summary>
    [System.Serializable]
    public enum GamePhase
    {
        ConnectPhase = 0,
        GameStart = 1,
        InGame = 2,
        PickPizza = 3,
        Result = 5
    }

    /// <summary>
    /// ゲームの進行状況を同期する
    /// </summary>
    /// <param name="phase">同期する進行状況</param>
    public void SyncGamePhase(Team team, GamePhase phase)
    {
        // すでに同期されていたらなにもしない
        if (team.Phase == phase) return;

        // 同期してフェーズ開始
        team.StartPhase(phase);
    }

    List<int> pickIndexes = new List<int>();

    const float RouletteTime = 1f;// ルーレット演出の長さ
    const int pickNum = 1;
    const float ShootableTime = 45f;
    const float PreparePizzaTime = 2f;// ピザ取得準備の時間
    const int PhaseCount = 2;// フェーズの総数
    const int PinePhase = 2;
    const int LastPhase = 3;
    IEnumerator Main()
    {
        if (!isStarted)
        {
            isStarted = true;
            // インゲームBGMを再生(Windowsのみ)
            PlayBGM_Windows(BGMType.InGame);
        }
        int counter = GameConstants.Zero;

        //while (pizzaManager.PizzaSlices.Count > 0)
        while (counter < PhaseCount)
        {
            // 発射準備フェーズ
            //yield return StartCoroutine(PizzaSelectPhase(RouletteTime, pickNum));

            // 食材発射フェーズ
            yield return StartCoroutine(ShootFoodPhase(ShootableTime));

            // ピザ取得待機フェーズ
            //yield return StartCoroutine(PreparePickPizzaPhase(PreparePizzaTime));

            // ピザ取得フェーズ
            //yield return StartCoroutine(PickPizzaPhase());
            yield return StartCoroutine(PickAllPizzaPhase());

            // フェーズ経過数を加算
            counter++;

            // フェーズ終了時処理
            yield return StartCoroutine(EndPhase(counter));
        }

        // リザルトフェーズ
        yield return StartCoroutine(ResultPhase());
    }

    IEnumerator PizzaSelectPhase(float rouletteTime, uint pickCount = 1)
    {
        // フェーズ設定
        currentPhase = GamePhase.GameStart;

        // 確実に誰も発射できないように
        SetAllPlayerUnshootable(teams);

        // 取得するピザの番号を取得
        pickIndexes = SelectPizzaSlices(pickCount);

        // 選ばれたピザの演出
        float timer = GameConstants.FirstTimerValue;
        while (timer < rouletteTime)
        {
            timer += Time.deltaTime;

            if (pickIndexes.Count == GameConstants.Zero) yield return null;// 取得するピザがなければ演出カット

            // ルーレット演出をいれる
            foreach (int index in pickIndexes)
            {
                pizzaManager.PizzaSlices[index].EnableHighlightObject();
            }


            yield return null;
        }
    }

    // すべてのプレイヤーを発射不可にする
    void SetAllPlayerUnshootable(List<Team> teams)
    {
        foreach (Team team in teams)
        {
            //team.ResetBulletCount();
            team.SetShootCT();
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

    const string OpacityPropertyName = "_OPACITY";

    /// <summary>
    /// 食材発射フェーズ
    /// </summary>
    /// <param name="shootTime">発射可能時間</param>
    /// <returns></returns>
    IEnumerator ShootFoodPhase(float shootTime)
    {
        // フェーズを設定
        currentPhase = GamePhase.InGame;

        // 完全に焼けるテクスチャになるまでの時間
        float cookTime = shootTime;

        // ピザの焼けるマテリアルたちを登録
        List<Material> cookedMaterials = new List<Material>();

        // ピザの焼けるマテリアルの設定
        float startOpacity = GameConstants.Zero;
        float endOpacity = GameConstants.One;

        // マテリアルを登録
        foreach (PizzaSlice slice in pizzaManager.PizzaSlices)
        {
            // 無効化されていたら有効化
            if (!slice.CookedRenderer.gameObject.activeSelf) slice.CookedRenderer.gameObject.SetActive(true);

            // リストに追加
            cookedMaterials.Add(slice.CookedRenderer.material);
        }

        if (pickIndexes.Count > 0)
        {
            // 念の為再度ハイライト
            foreach (int index in pickIndexes)
            {
                pizzaManager.PizzaSlices[index].EnableHighlightObject();
            }
        }

        pizzaManager.StartSpin();// 回転開始

        // プレイヤーは発射できるように
        SetAllPlayerShootable(teams);

        float timer = GameConstants.FirstTimerValue;
        while (timer < shootTime)
        {
            // UI更新
            UpdatePickTimeUI(shootTime - timer);

            //ピザの焼けるマテリアルへと変えていく
            if (timer <= cookTime)
            {
                foreach (Material cookedMaterial in cookedMaterials)
                {
                    // 割合から透明度を計算
                    float currentOpacity = Mathf.Lerp(startOpacity, endOpacity, timer / cookTime);

                    // 透明度を適用
                    cookedMaterial.SetFloat(OpacityPropertyName, currentOpacity);
                }
            }

            // パインの召喚処理

            // 途中でピザを取られるフェーズの処理
            // 1.ピックするフェーズだったら、取得するピザを選ぶ（演出入り？） その後取得までの時間を現在タイマー+◯◯秒で設定、ピック中のフラグを立てる
            // 2.取得する時間になったらそのピザの取得演出を入れる、その後ピック中のフラグをオフに

            // 時間経過
            timer += Time.deltaTime;
            // 全員が食材を発射し終えたら途中でも次のフェーズへ
            //if (IsAllPlayerUnShootable()) yield break;

            yield return null;
        }
    }

    /// <summary>
    /// 全プレイヤーを発射可能な状態にする
    /// </summary>
    void SetAllPlayerShootable(List<Team> teams)
    {
        foreach (Team team in teams)
        {
            team.SetShootable();
            team.ResetShootCT();
        }

    }

    /// <summary>
    /// すべてのプレイヤーが発射不可能な状態か調べる
    /// </summary>
    /// <returns>すべてのプレイヤーが発射不可能な状態であるか</returns>
    bool IsAllPlayerUnShootable()
    {
        foreach (Team team in teams)
        {
            // 一人でも撃てるなら偽
            if (team.Shootable) return false;
        }

        return true;
    }

    IEnumerator PreparePickPizzaPhase(float preparePizzaTime)
    {
        // フェーズ設定
        currentPhase = GamePhase.PickPizza;

        pizzaManager.StopSpin();// 回転停止

        // プレイヤーは発射不可
        SetAllPlayerUnshootable(teams);

        // 取得待機演出
        yield return StartCoroutine(pizzaManager.PrepareTakePizza(preparePizzaTime));
    }

    IEnumerator PickPizzaPhase()
    {
        // フェーズ設定
        currentPhase = GamePhase.PickPizza;

        if (pickIndexes.Count < 1) yield return null;

        // 取得
        Debug.Log("取得");
        pizzaManager.TakePizzaSlice(pickIndexes);

        yield return null;
    }

    IEnumerator PickAllPizzaPhase()
    {
        // フェーズ設定
        currentPhase = GamePhase.PickPizza;
        // 取得
        pizzaManager.TakeAllPizza();

        yield return null;
    }

    IEnumerator EndPhase(int phaseCounter)
    {
        int nextPhase = phaseCounter;// 次のフェーズを取得

        // ピザの復活処理（アニメーションの再生）

        // パイン開始フラグを設定予定
        //if (nextPhase == PinePhase) 

        // 魔王のピック開始フラグを設定予定
        //if(nextPhase == LastPhase)

        yield return null;
    }

    IEnumerator ResultPhase()
    {
        // フェーズ設定
        currentPhase = GamePhase.Result;

        mainResult.gameObject.SetActive(true);

        //for(int i = 0; i < teams.Count; i++)
        //{
        //    //break;// デバッグ用
        //    if (teams[i].Result == null) continue;

        //    if (!teams[i].Result.gameObject.activeInHierarchy) teams[i].Result.gameObject.SetActive(true);
        //    if (!teams[i].Result.gameObject.activeInHierarchy) continue;

        //    yield return teams[i].Result.ShowResult();
        //}

        if (mainResult.gameObject.activeInHierarchy)
        {
            yield return StartCoroutine(mainResult.ShowResult());
        }
    }
}