using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.Linq;

public class StageManager : MonoBehaviour
{
    [System.Serializable]
    class InfoForReflect
    {
        public class FoodReflectInfo
        {
            Rigidbody rb;
            public Rigidbody Rigidbody => rb;

            FoodMove food;
            public FoodMove Food => food;

            Vector3 velocity;
            public Vector3 Velocity => velocity;

            public FoodReflectInfo( FoodMove foodMove)
            {
                this.rb = foodMove.Rigidbody;
                this.food = foodMove;
                this.velocity = rb.velocity;
            }
        }

        FoodReflectInfo first;
        public FoodReflectInfo First => first;

        FoodReflectInfo second;
        public FoodReflectInfo Second => second;

        public InfoForReflect(FoodMove firstFood, FoodMove secondFood)
        {
            first = new FoodReflectInfo(firstFood);
            second = new FoodReflectInfo(secondFood);
        }

        /// <summary>
        /// 同じ情報かを確かめる（順番が違うだけのものも同じとする）
        /// </summary>
        public bool IsSame(Rigidbody firstRb, Rigidbody secondRb)
        {
            if (firstRb == this.first.Rigidbody || firstRb == this.second.Rigidbody) return true;
            if (secondRb == this.first.Rigidbody || secondRb == this.second.Rigidbody) return true;

            return false;
        }
    }

    [System.Serializable]
    class TrackObject
    {
        const float Magnification = 3f;// 係数
        const float BowAngleYCorrection = 90f;// 弓のY軸回転の修正値

        static readonly Vector3 DirectionArrowAngles = new Vector3(90f, 0f, 0f);
        static readonly Vector3 DefaultDirectionArrowScales = new Vector3(1f, 1f, 1f);

        [Header("移動させるオブジェクト"), SerializeField] Transform trackObject;
        [Header("弓に表示する演出用の食べ物"), SerializeField] FoodBeforeShoot foodBeforeShoot;
        public FoodBeforeShoot FoodBeforeShoot => foodBeforeShoot;

        [Header("基準点"), SerializeField] Transform pivot;
        public Vector3 PivotPos => pivot.position;

        [Header("演出用食べ物の出現位置"), SerializeField] Transform foodSpawnPoint;

        [Header("弓"), SerializeField] Transform bow;
        [Header("弦と矢のコントローラー"), SerializeField] BowControler bowStringController;
        public BowControler BowStringController => bowStringController;

        [Header("方向を示す矢"), SerializeField] Transform directionArrow;
        [Header("矢の太さ(最小)"), SerializeField] float minArrowWidth = 0.5f;
        [Header("捕食モードになるまでの時間（秒）"), SerializeField] float eatModeChargeSeconds = 2.5f;
        [Header("引っ張った距離に応じてサイズにかける倍率"), SerializeField] Vector2 pullMangification = new Vector2(0.01f, 0.15f);
        [Header("伸ばせる最大距離"), SerializeField] float maxDistance = 7f;
        [SerializeField] float basePower = 20f;

        Vector3 startPos;
        Vector3 lastPos;
        float pullTimer = GameConstants.FirstTimerValue;
        bool eatMode = false;// 捕食モードかどうか
        public bool EatMode => eatMode;

        bool onEatModeChanged = false;// 捕食モードに切り替わった瞬間か
        public bool OnEatModeChanged => onEatModeChanged;
        public bool SetOnEatModeFalse() => onEatModeChanged = false;

        CursorInfo cursorInfo;
        public CursorInfo Cursor => cursorInfo;
        public FoodMove FoodPrefab => cursorInfo.Food;

        // 具材を飛ばす力
        public float Power => basePower * Magnification * calcRate(new Vector2(TrackPosition.x, TrackPosition.z));

        // 指が離れてからの発射されるまでの猶予を設けつつ、離した位置を基準に生成や発射を行いたいため
        public Vector3 TrackPosition => lastPos;
        public Vector3 ShotVector => pivot.position - TrackPosition;
        public Vector3 ShotDirection
        {
            get
            {
                Vector3 direction = ShotVector.normalized;
                direction.y = 0f;
                return direction;
            }
        }
        // ドラッグされているか
        public bool IsDragging => TrackPosition != startPos;

        // 動いているか
        public bool IsMoving => trackObject.transform.position != lastPos;

        // 最後の位置は動いた先で、オブジェクトが初期位置に戻ったときに真になる
        public bool Released => lastPos != startPos && trackObject.transform.position == startPos;
        public void SetStartPos()
        {
            startPos = trackObject.position;
            lastPos = startPos;
        }
        public void SetCursorInfo()
        {
            cursorInfo = trackObject.GetComponent<CursorInfo>();// 取得
        }
        public void UpdateLastPosition()
        {
            lastPos = trackObject.position;
        }
        /// <summary>
        /// 食べ物の出現時
        /// </summary>
        public void OnFoodSpawn()
        {
            // 演出用食べ物の位置設定
            foodBeforeShoot.transform.position = foodSpawnPoint.transform.position;

            // 有効化
            foodBeforeShoot.gameObject.SetActive(true);

            // タイマーをリセット
            pullTimer = GameConstants.FirstTimerValue;

            // 捕食モード解除
            eatMode = false;

            // 食べ物の牙を消す
            BowStringController.CurrentArrow.DisableEatMode();

            // エフェクト発生処理を以下に追加

        }

        /// <summary>
        /// 矢の更新
        /// </summary>
        public void UpdateArrow()
        {
            // nullチェック
            if (directionArrow == null) return;

            // ドラッグされていないときは表示しない
            if (!IsDragging && directionArrow.gameObject.activeSelf)
            {
                directionArrow.gameObject.SetActive(false);// 方向を示す矢を無効化
                return;
            }
            // 発射可能でないときは反応させない
            if (!cursorInfo.Team.Shootable) return;

            // ドラッグしているときの処理
            if (IsDragging)
            {
                // 引き始め
                if (!directionArrow.gameObject.activeSelf)
                {
                    directionArrow.gameObject.SetActive(true);// 方向を示す矢を有効化
                    bowStringController.StartAim(foodBeforeShoot);// 弦を引っ張り始める
                    pullTimer = GameConstants.FirstTimerValue;// タイマーをリセット
                    eatMode = false;
                    onEatModeChanged = false;
                }

                // 引張時間を経過
                pullTimer += Time.deltaTime;
                // 一定時間を超えたら捕食を有効化する
                if(pullTimer >= eatModeChargeSeconds)
                {
                    // チャージ完了した瞬間だけのフラグを設定
                    if(!onEatModeChanged && eatMode) onEatModeChanged = true;
                    else onEatModeChanged = false;

                    eatMode = true;
                }

                Vector3 pivotPosition = pivot.position;
                pivotPosition.y = TrackPosition.y;

                // 移動させるオブジェクトと基準点との位置関係を計算し、距離によって矢の大きさを変化させる
                directionArrow.position = (TrackPosition + pivotPosition) / 2f;
                directionArrow.LookAt(pivot.position);
                directionArrow.eulerAngles = new Vector3(DirectionArrowAngles.x, directionArrow.eulerAngles.y, DirectionArrowAngles.z);

                // 引っ張られた距離に応じてサイズを変える
                float distance = ShotVector.magnitude;
                // 横幅を距離の二乗で急激に大きくする（強く引っ張っているイメージ）
                directionArrow.localScale = new Vector3(minArrowWidth + distance * distance * pullMangification.x, distance * pullMangification.y, DefaultDirectionArrowScales.z);

                // 弓の回転(現在は360度回転できる、気になるようなら方向を示す矢の回転の段階で角度を制限)
                Vector3 eulerAngles = directionArrow.eulerAngles;
                eulerAngles.y += BowAngleYCorrection;
                bow.eulerAngles = eulerAngles;

                // 弦の更新(離されていないとき)
                if (!Released) bowStringController.Aim(TrackPosition);
            }
        }

        const float MaxRate = 1f;
        float calcRate(Vector2 target)
        {
            Vector2 distanceVector = new Vector2(pivot.position.x - target.x, pivot.position.z - target.y);
            float squaredDistance = distanceVector.x * distanceVector.x + distanceVector.y * distanceVector.y;// 距離の二乗(-をなくすため)
            float rate = squaredDistance / (maxDistance * maxDistance);
            if (rate > MaxRate) rate = MaxRate;
            return rate;
        }
    }

    const float BaseKeepReflectSpeedRate = 100f;// 反射時の勢いにかける数、普段は100%までの勢い保持率でいいはず

    [SerializeField] List<TrackObject> trackObjects = new List<TrackObject>();
    [SerializeField] List<InfoForReflect> reflectList = new List<InfoForReflect>();
    List<InfoForReflect> mergeEventList = new List<InfoForReflect>();
    List<InfoForReflect> eatEventList = new List<InfoForReflect>();

    [Header("振動のマネージャー"), SerializeField] VibrateManager vibrateManager;
    [Header("サウンドのマネージャー"), SerializeField] SoundManager soundManager;

    void Start()
    {
        for (int i = 0; i < trackObjects.Count; i++)
        {
            trackObjects[i].SetStartPos();
            trackObjects[i].SetCursorInfo();
        }

        // Androidのみ振動を有効化
        if (vibrateManager.IsAndroid) vibrateManager.EnableVibrate();
    }

    void FixedUpdate()
    {
        for (int i = 0; i < trackObjects.Count; i++)
        {
            // 指が離されて、発射されるとき
            if (trackObjects[i].Released)
            {
                if (trackObjects[i].Cursor.Shootable)
                {
                    // 具材を生成して発射
                    SummonAndShotFood(trackObjects[i].FoodPrefab, trackObjects[i].EatMode, trackObjects[i].TrackPosition + Vector3.up * 0.5f, trackObjects[i].ShotDirection, trackObjects[i].PivotPos, trackObjects[i].Power);
                }

                // 弦の引き絞りを終了
                trackObjects[i].BowStringController.EndAim(trackObjects[i].TrackPosition);

                // 弓の演出用食べ物を非表示
                if (trackObjects[i].FoodBeforeShoot.gameObject.activeSelf) trackObjects[i].FoodBeforeShoot.gameObject.SetActive(false);

                // 発射可能状況の制御
                trackObjects[i].Cursor.OnShoot();
            }
            // 発射クールタイム終了時
            if (trackObjects[i].Cursor.Team.Shootable && !trackObjects[i].FoodBeforeShoot.gameObject.activeSelf) trackObjects[i].OnFoodSpawn();
            // ドラッグ中
            if (trackObjects[i].IsMoving)
            {
                // 動かしているときのエフェクトを入れる予定
                //Debug.Log("moving");
            }
            // ドラッグ中の矢の表示
            trackObjects[i].UpdateArrow();

            // 捕食可能になったときの処理
            if (trackObjects[i].OnEatModeChanged) OnEatableChanged(trackObjects[i]);

            // 捕食モードの処理
            if (trackObjects[i].EatMode) OnEatMode(trackObjects[i]);

            // ドラッグ位置の履歴を更新
            trackObjects[i].UpdateLastPosition();
        }

        // 反射リストにあったら反射する
        if (reflectList.Count > 0)
        {
            for (int i = reflectList.Count - 1; i >= 0; i--)
            {
                Reflect(reflectList[i]);
            }
            reflectList.Clear();// リストをクリア
        }
        // くっつける
        if (mergeEventList.Count > 0)
        {
            for(int i = mergeEventList.Count - 1;i >= 0;i--)
            {
                mergeEventList[i].First.Food.OnMerge(mergeEventList[i].Second.Food);
                // 結合SE再生
                soundManager.PlaySE(PlayerSoundType.Merge, mergeEventList[i].First.Food.transform);
                Debug.Log("[MERGE]");
            }
            mergeEventList.Clear();// リストをクリア
        }
        // 食べる
        if(eatEventList.Count > 0)
        {
            for(int i =  eatEventList.Count - 1; i>=0;i--)
            {
                eatEventList[i].First.Food.OnEat(eatEventList[i].Second.Food, eatEventList[i].First.Velocity);
                // 捕食SE再生
                soundManager.PlaySE(PlayerSoundType.Eat, mergeEventList[i].First.Food.transform);
                Debug.Log("[EAT]");
            }
            eatEventList.Clear();// リストをクリア
        }
    }

    /// <summary>
    /// 捕食可能になったときの演出・処理
    /// </summary>
    void OnEatableChanged(TrackObject trackObject)
    {
        // エフェクト表示

        //// 捕食可能状態の切り替わりフラグの無効化
        //trackObject.SetOnEatModeFalse();
    }

    void OnEatMode(TrackObject trackObject)
    {
        // 振動
        vibrateManager.Vibrate(VibrationSituations.FullyCharged);

        // 弓についている食べ物にも牙を出す
        trackObject.BowStringController.CurrentArrow.EnableEatMode();
    }

    public void SummonAndShotFood(FoodMove foodPrefab,bool eatMode, Vector3 summonPosition, Vector3 shotDirection, Vector3 pivotPos, float power)
    {
        // 具材の生成
        GameObject food = Instantiate(foodPrefab.gameObject, summonPosition, Quaternion.identity);
        FoodMove foodMove = food.GetComponent<FoodMove>();

        // 捕食可能モードの設定
        if(eatMode) foodMove.EnableEatMode();

        // 発射する方を向かせる
        food.transform.LookAt(pivotPos);

        // 発射
        foodMove.AddForce(shotDirection, power);
    }

    public void AddReflectList(FoodMove self, FoodMove opponent)
    {
        AddInfoForReflectList(reflectList, self, opponent);
    }

    void AddInfoForReflectList(List<InfoForReflect> list, FoodMove self, FoodMove target)
    {
        // リストに何も無ければ追加
        if (list.Count == 0)
        {
            list.Add(new InfoForReflect(self, target));
            return;
        }
        // リストにすでに入っているときは追加しない
        if (HasPair(list, self, target)) return;

        list.Add(new InfoForReflect(self, target));// 追加
    }

    /// <summary>
    /// リスト内に同じペアを含んでいるかどうか
    /// </summary>
    /// <param name="list">調べるリスト</param>
    /// <param name="first">1つめ</param>
    /// <param name="second">2つめ</param>
    /// <returns>同じペアを含んでいるかどうか</returns>
    bool HasPair(List<InfoForReflect> list, FoodMove first, FoodMove second)
    {
        return list.Any(e => (e.First.Food == first &&  e.Second.Food == second) || (e.First.Food == second && e.Second.Food == first));
    }

    const float MaxReflectScale = 1f;
    //const float ResolvePenetrationThreshold = 0.5f;
    //const float ResolvePenetrationFactor = 10f;
    /// <summary>
    /// 衝突時の反射
    /// </summary>
    void Reflect(InfoForReflect reflectInfo)
    {
        if (reflectInfo.First.Rigidbody == null || reflectInfo.Second.Rigidbody == null) return;

        // RigidBodyのIsKinemanicも加味する予定
        Rigidbody baseRb = reflectInfo.First.Rigidbody.velocity.magnitude >= reflectInfo.Second.Rigidbody.velocity.magnitude ?
            reflectInfo.First.Rigidbody : reflectInfo.Second.Rigidbody;

        Vector3 baseVelocity = reflectInfo.First.Rigidbody.velocity + reflectInfo.Second.Rigidbody.velocity;// お互いの勢いを足す

        //Vector3 direction = (reflectInfo.Second.Rigidbody.transform.position - reflectInfo.First.Rigidbody.position);
        //float distance = direction.magnitude;
        //// 重なりの防止
        //if (distance <= ResolvePenetrationThreshold)
        //{

        //    // 力を加える方向
        //    direction.Normalize();

        //    float penetrationDepth = ResolvePenetrationThreshold - distance;
        //    baseVelocity = direction * penetrationDepth * ResolvePenetrationFactor;

        //    //baseVelocity = baseRb == reflectInfo.First.Rigidbody ? -distance : distance;
        //}

        // 勢いを計算しやすいように変換
        baseVelocity /= BaseKeepReflectSpeedRate;// 後で食材ごとに%を変換しないで済むようにしている
        baseVelocity.y = 0f;// y方向の力は必要ないので無くしておく

        Vector3 firstVelocity = Vector3.zero;
        Vector3 secondVelocity = Vector3.zero;

        // それぞれの勢いの設定
        if (baseRb == reflectInfo.First.Rigidbody)
        {
            firstVelocity = baseVelocity * -reflectInfo.First.Food.ReflectRate;
            secondVelocity = baseVelocity * reflectInfo.Second.Food.ReflectRate;
        }
        else
        {
            firstVelocity = baseVelocity * reflectInfo.First.Food.ReflectRate;
            secondVelocity = baseVelocity * -reflectInfo.Second.Food.ReflectRate;
        }

        // 速度を加算（つながってる数に応じて勢いを減らす）
        reflectInfo.First.Food.Root.Rigidbody.velocity += firstVelocity * (MaxReflectScale / (reflectInfo.First.Food.GetConnectedCount()));
        reflectInfo.Second.Food.Root.Rigidbody.velocity += secondVelocity * (MaxReflectScale / (reflectInfo.Second.Food.GetConnectedCount()));
    }

    public void AddMergeEventList(FoodMove self, FoodMove target)
    {
        AddInfoForReflectList(mergeEventList, self, target);
    }

    public void AddEatEventList(FoodMove self, FoodMove target)
    {
        AddInfoForReflectList(eatEventList, self, target);
    }

    /// <summary>
    /// アイテム獲得時、獲得したチームがパワーアップ可能に
    /// </summary>
    /// <param name="item">取得されたアイテム</param>
    /// <param name="acquirer">獲得者</param>
    public void OnAcquireItem(FieldItem item, FoodMove acquirer)
    {
        foreach (TrackObject trackObject in trackObjects)
        {
            if (trackObject.Cursor.Team.Color == acquirer.Team)// 取得したチームを見つける
            {
                trackObject.Cursor.SetModeFlag(item.Mode);// 移行可能なモードに追加
                Destroy(item.gameObject);// フィールドからアイテムを削除
            }
        }
    }

    public void FoodGrow(CursorInfo cursor)
    {
        // 大きさ強化切り替え（溜まっていたら）
        if (!cursor.CanBig) return;

        // 巨大化以外のとき（大きくする）
        if (cursor.FoodMode != CursorInfo.Mode.Big) ChangeBigFood(cursor);

        // 巨大化時（元に戻す）
        else if (cursor.FoodMode == CursorInfo.Mode.Big) RevertToNormal(cursor);
    }


    public void FoodChangeBomb(CursorInfo cursor)
    {
        // 爆弾強化切り替え（溜まっていたら）
        if (!cursor.CanBomb) return;

        // 爆弾以外のとき（爆弾に変える）
        if (cursor.FoodMode != CursorInfo.Mode.Bomb) ChangeBomb(cursor);

        // 爆弾時（元に戻す）
        else if (cursor.FoodMode == CursorInfo.Mode.Bomb) RevertToNormal(cursor);
    }

    /// <summary>
    /// 巨大化させる
    /// </summary>
    void ChangeBigFood(CursorInfo cursor)
    {
        Debug.Log("巨大化");
        cursor.SetMode(CursorInfo.Mode.Big);
    }

    /// <summary>
    /// 通常時の状態に戻す
    /// </summary>
    void RevertToNormal(CursorInfo cursor)
    {
        Debug.Log("通常に戻る");
        cursor.SetMode(CursorInfo.Mode.Normal);
    }

    void ChangeBomb(CursorInfo cursor)
    {
        Debug.Log("爆弾化");
        cursor.SetMode(CursorInfo.Mode.Bomb);
    }
}
