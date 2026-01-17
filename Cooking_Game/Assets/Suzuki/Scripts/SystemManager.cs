using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.Timeline;
using System.Linq;

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

        [Header("--- UI設定 ---")]
        [Header("ゲームスタート時に非表示にするUI"), SerializeField] List<UIGroupSwitcher> connectCanvases;

        [Header("チームの情報UIテキスト"), SerializeField] TMPro.TextMeshProUGUI scoreText;
        public TMPro.TextMeshProUGUI ScoreText => scoreText;

        [Header("取られるまでの時間のテキスト"), SerializeField] TMPro.TextMeshProUGUI pickTimeText;
        public TMPro.TextMeshProUGUI PickTimeText => pickTimeText;

        [Header("--- リザルト表示 ---")]

        [Header("タブレット画面")]
        [Header("タブレット画面のリザルトのスクリプト"), SerializeField] Result tabletResult;
        public Result TabletResult => tabletResult;
        [Header("タブレット画面のスコア表示オブジェクト"), SerializeField] GameObject tabletResuiltUI;
        public GameObject TabletResuiltUI => tabletResuiltUI;

        [Header("〃のスコアバー"), SerializeField] RectTransform tabletScoreBar;
        public RectTransform TabletScoreBar => tabletScoreBar;

        float shootableTimer = GameConstants.FirstTimerValue;
        public float ShootableTimer => shootableTimer;

        public void SetUnshootable() => shootable = false;
        public void SetShootable() => shootable = true;

        [SerializeField] GamePhase phase;// ゲームの進行状況
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
            // 接続UIを非表示化
            foreach (UIGroupSwitcher groupSwitcher in connectCanvases) groupSwitcher.ChangeUIGroup();

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
    [Header("ゲームスタート時に非表示にするUI"), SerializeField] List<UIGroupSwitcher> connectCanvases;
    [Header("サウンドマネージャー"), SerializeField] SoundManager soundManager;

    [Header("--- フェーズ時間 ---")]
    [Header("食材の発射フェーズの時間"), SerializeField] float shootPhaseTime;
    [Header("ハーフタイムの時間"), SerializeField] float breakPhaseTime;
    [Header("ピザが取られるフェーズの時間"), SerializeField] float pickPhaseTime;

    [Header("--- タイムラインの設定 ---")]
    [Header("ピザのカットを行うDirector"), SerializeField] PlayableDirector pizzaCutDirector;
    [Header("ピザの取得を行うDirector"), SerializeField] PlayableDirector pizzaStealDirector;
    [Header("ピザの取得シグナルトラックの名前"), SerializeField] string pizzaStealSignalTrackName;
    [Header("ピザの取得でスライスを動かすAnimationTrack"), SerializeField] string pizzaStealAnimationTrackName;

    [Header("--- スクリプトでのアニメーション設定 ---")]
    [Header("時計の中身"), SerializeField] List<Image> clockFillers;
    [Header("時計を満たすアニメーションの時間（秒）"), SerializeField] float clockFillTime;

    [SerializeField] List<Team> teams;
    public List<Team> Teams => teams;

    bool isStarted;
    public bool IsStarted => isStarted;

    PizzaManager pizzaManager;
    GamePhase currentPhase;
    public GamePhase CurrentPhase => currentPhase;

    public void SetCurrentPhase(GamePhase phase) => currentPhase = phase;

    void Start()
    {
        pizzaManager = FindObjectOfType<PizzaManager>();

        isStarted = false;

        // フェーズの初期化（接続待ちフェーズに）
        currentPhase = GamePhase.ConnectPhase;

        SetAllPlayerShootable(teams);

        // 接続画面のBGMを再生（Windowsのみ）
        PlayBGM_Windows(BGMType.ConnectLobby);

        // デバッグ用
        //StartCoroutine(Main());
        //PlaySE_Windows(PlayerSoundType.Eat, transform);
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

    public void OnStartReady()
    {
        // 準備画面を非表示
        foreach (UIGroupSwitcher groupSwitcher in connectCanvases) groupSwitcher.ChangeUIGroup();

        // ゲーム開始
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

        // デバッグ用
        //currentPhase = GamePhase.InGame;
        //SetAllPlayerShootable(teams);
        //while(true)
        //{
        //    yield return null;
        //}

        while (counter < PhaseCount)
        {
            // 発射準備フェーズ
            //yield return StartCoroutine(PizzaSelectPhase(RouletteTime, pickNum));

            // 食材発射フェーズ
            yield return StartCoroutine(ShootFoodPhase(shootPhaseTime));

            // ピザ取得待機フェーズ
            yield return StartCoroutine(PreparePickPizzaPhase(breakPhaseTime));

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
            EnablePizzaHighlight();


            yield return null;
        }
    }

    // 取られるピザのハイライト
    void EnablePizzaHighlight()
    {
        foreach (int index in pickIndexes)
        {
            pizzaManager.PickableSlices[index].EnableHighlightObject();
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
        // 選択個数がピザ切れの総数より多かった場合は、ピザ切れの総数にする
        if (pickCount > pizzaManager.PickableSlices.Count) pickCount = (uint)pizzaManager.PickableSlices.Count;

        // 選択個数が0個なら初期化されたものを返す
        if (pickCount == GameConstants.Zero) return new List<int>();

        //int pickIndex = Random.Range(0, pizzaManager.PizzaSlices.Count);
        List<int> pickIndexes = new List<int>();
        List<PizzaSlice> pickableSlices = new List<PizzaSlice>();
        pickableSlices.AddRange(pizzaManager.PickableSlices);

        // 取る個数分取る場所を指定
        for (int i = 0; i < pickCount; i++)
        {
            if (i > pickableSlices.Count) break;

            int index = Random.Range(GameConstants.Zero, pickableSlices.Count);
            pickIndexes.Add(index);
            Debug.Log($"{pickableSlices[index]}が選ばれた");

            // ハイライト
            pickableSlices[index].EnableHighlightObject();

            pickableSlices.Remove(pickableSlices[index]);
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

        // ピザの上の食べ物をすべて消去
        pizzaManager.ClearAllFood();

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

        // ハイライトを消去
        foreach(PizzaSlice slice in pizzaManager.PizzaSlices)
        {
            // ハイライトの消去
            slice.DisableHighlightObject();
        }

        // 元の位置に戻す
        pizzaManager.SetAllPizzaStartPosition();

        // 時計のFillAmountをMaxにするアニメーション
        yield return FillClock(clockFillTime);

        // 回転開始
        pizzaManager.StartSpin();

        // プレイヤーは発射できるように
        SetAllPlayerShootable(teams);

        float timer = GameConstants.FirstTimerValue;
        while (timer < shootTime)
        {
            // UI更新
            //UpdatePickTimeUI(shootTime - timer);
            UpdateClockUI(timer, shootTime);

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

            // 時間経過
            timer += Time.deltaTime;
            // 全員が食材を発射し終えたら途中でも次のフェーズへ
            //if (IsAllPlayerUnShootable()) yield break;

            yield return null;
        }
    }

    // 時計の見た目を更新
    void UpdateClockUI(float time, float maxTime)
    {
        float ratio = time / maxTime;

        foreach(Image filler in clockFillers)
        {
            filler.fillAmount = Mathf.Lerp(GameConstants.One, GameConstants.Zero, ratio);
        }
    }

    /// <summary>
    /// 時計の時間を満たす
    /// </summary>
    /// <param name="fillTime">満たすまでの時間</param>
    IEnumerator FillClock(float fillTime)
    {
        float timer = GameConstants.FirstTimerValue;

        while(timer < fillTime)
        {
            // 時計の中身を増加させる
            foreach(Image filler in clockFillers)
            {
                filler.fillAmount = Mathf.Lerp(GameConstants.Zero, GameConstants.One, timer / fillTime);
            }

            yield return null;
            timer += Time.deltaTime;
        }

        // 確実に全て埋める
        foreach(Image filler in clockFillers)
        {
            filler.fillAmount = GameConstants.One;
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

        // カット演出
        // 有効化
        if (!pizzaCutDirector.gameObject.activeSelf) pizzaCutDirector.gameObject.SetActive(true);
        // 再生
        pizzaCutDirector.Play();

        // 再生完了まで待機（状態で判定）
        yield return new WaitUntil(() => pizzaCutDirector.state != PlayState.Playing);
        //// 再生完了まで待機（再生時間で判定、Hold用）
        //yield return new WaitUntil(() => pizzaCutDirector.time >= pizzaCutDirector.duration);

        // 無効化
        pizzaCutDirector.gameObject.SetActive(false);
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
        //// 取得
        //pizzaManager.TakeAllPizza();
        
        // 次に取るピザを決定
        pickIndexes = SelectPizzaSlices();

        // 時計を再度埋める
        yield return FillClock(clockFillTime);

        // 回転開始
        pizzaManager.StartSpin();

        // プレイヤーが発射可能に
        SetAllPlayerShootable(teams);

        // 取得ペースを計算
        float pickPace = pickPhaseTime / pizzaManager.PizzaSlices.Count;

        float pickTimer = GameConstants.FirstTimerValue;
        float phaseTimer = GameConstants.FirstTimerValue; 

        while (pizzaManager.PickableSlices.Count != GameConstants.Zero)
        {
            if(pickTimer >= pickPace)
            {
                //pizzaManager.TakePizzaSlice(pickIndexes);
                // スライスの親子付け解除
                pizzaManager.PizzaSlices[pickIndexes[GameConstants.FirstIndex]].transform.SetParent(null);

                // ピザの取得timeline再生
                PlayPickTimeline(pizzaManager.PizzaSlices[pickIndexes[GameConstants.FirstIndex]].gameObject);

                // 次に取るピザを決定
                pickIndexes = SelectPizzaSlices();

                // ピックタイミングを待つ（タイマーリセット）
                pickTimer = GameConstants.FirstTimerValue;
            }
            // タイマーのUI更新
            UpdateClockUI(phaseTimer, pickPhaseTime);

            // タイマー増加
            phaseTimer += Time.deltaTime;
            pickTimer += Time.deltaTime;
            yield return null;
        }
    }

    void PlayPickTimeline(GameObject target)
    {
        // 指定した名前のSignalTrackを探す
        BindToTrack<SignalTrack>(pizzaStealSignalTrackName, target);

        // 指定した名前のAnimationTrackを探す
        BindToTrack<AnimationTrack>(pizzaStealAnimationTrackName, target);

        // ピック準備位置へと移動

        // スライスの方を向く

        // 再生
        pizzaStealDirector.Play();
    }

    //
    void BindToTrack<T>(string trackName,  GameObject target) where T : TrackAsset
    {
        // Timelineアセットを取得
        TimelineAsset timelineAsset = pizzaStealDirector.playableAsset as TimelineAsset;

        // T型のTrackAssetで指定した名前のトラックを探す
        TrackAsset track = timelineAsset.GetOutputTracks().OfType<T>().FirstOrDefault(track => track.name == trackName);

        if (track != null)
        {
            // オブジェクトをバインドする（このトラック上のSignalEmitterがtargetのSignalEmitterを発火させるようにする）
            pizzaStealDirector.SetGenericBinding(track, target);
        }
    }

    IEnumerator EndPhase(int phaseCounter)
    {
        int nextPhase = phaseCounter;// 次のフェーズを取得

        // ピザの復活処理（アニメーションの再生）

        pizzaManager.ActivatePizzaSlices();
        pizzaManager.FillAllPickableSlices();

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