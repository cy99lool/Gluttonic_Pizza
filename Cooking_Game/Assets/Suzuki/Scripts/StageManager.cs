using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [System.Serializable]
    class InfoForReflect
    {
        public class FoodReflectInfo
        {
            Rigidbody rb;
            public Rigidbody Rigidbody => rb;

            float reflectRate;
            public float ReflectRate => reflectRate;

            public FoodReflectInfo(Rigidbody rb, float reflectRate)
            {
                this.rb = rb;
                this.reflectRate = reflectRate;
            }
        }

        FoodReflectInfo first;
        public FoodReflectInfo First => first;

        FoodReflectInfo second;
        public FoodReflectInfo Second => second;

        public InfoForReflect(Rigidbody firstRb, float firstReflectRate, Rigidbody secondRb, float secondReflectRate)
        {
            first = new FoodReflectInfo(firstRb, firstReflectRate);
            second = new FoodReflectInfo(secondRb, secondReflectRate);
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
        const float Magnification = 2f;// 係数
        const float BowAngleYCorrection = 90f;// 弓のY軸回転の修正値

        static readonly Vector3 DirectionArrowAngles = (90f, 0f, 0f);
        static readonly Vector3 DefaultDirectionArrowScales = (1f, 1f, 1f);

        [Header("移動させるオブジェクト"), SerializeField] Transform trackObject;
        [Header("基準点"), SerializeField] Transform pivot;
        public Vector3 PivotPos => pivot.position;

        [Header("弓"), SerializeField] Transform bow;
        [Header("弦と矢のコントローラー"), SerializeField] BowControler bowStringController;
        public BowControler BowStringController => bowStringController;

        [Header("方向を示す矢"), SerializeField] Transform directionArrow;
        [Header("矢の太さ(最小)"), SerializeField] float minArrowWidth = 0.5f;
        [Header("引っ張った距離に応じてサイズにかける倍率"), SerializeField] Vector2 pullMangification = new Vector2(0.01f, 0.15f);
        [Header("伸ばせる最大距離"), SerializeField] float maxDistance = 7f;
        [SerializeField] float basePower = 20f;

        Vector3 startPos;
        Vector3 lastPos;
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

            // ドラッグしているときの処理
            if (IsDragging)
            {
                // 引き始め
                if (!directionArrow.gameObject.activeSelf)
                {
                    directionArrow.gameObject.SetActive(true);// 方向を示す矢を有効化
                    bowStringController.StartAim();// 弦を引っ張り始める
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

    void Start()
    {
        for (int i = 0; i < trackObjects.Count; i++)
        {
            trackObjects[i].SetStartPos();
            trackObjects[i].SetCursorInfo();
        }
    }

    void FixedUpdate()
    {
        for (int i = 0; i < trackObjects.Count; i++)
        {
            // 指が離されて、発射されるとき
            if (trackObjects[i].Released)
            {
                // 具材を生成して発射
                SummonAndShotFood(trackObjects[i].FoodPrefab, trackObjects[i].TrackPosition + Vector3.up * 0.5f, trackObjects[i].ShotDirection, trackObjects[i].PivotPos, trackObjects[i].Power);

                // 弦の引き絞りを終了
                trackObjects[i].BowStringController.EndAim(trackObjects[i].TrackPosition);

                // 発射可能状況の制御
                trackObjects[i].Cursor.OnShoot();
            }
            if (trackObjects[i].IsMoving)
            {
                // 動かしているときのエフェクトを入れる予定
                Debug.Log("moving");
            }
            // ドラッグ中の矢の表示
            trackObjects[i].UpdateArrow();

            // ドラッグ位置の履歴を更新
            trackObjects[i].UpdateLastPosition();
        }

        // 反射リストにあったら反射する
        if (reflectList.Count > 0)
        {
            for (int i = reflectList.Count - 1; i >= 0; i--)
            {
                Reflect(reflectList[i]);
                reflectList.RemoveAt(i);// リストから削除
            }
        }
    }

    public void SummonAndShotFood(FoodMove foodPrefab, Vector3 summonPosition, Vector3 shotDirection, Vector3 pivotPos, float power)
    {
        // 具材の生成
        GameObject food = Instantiate(foodPrefab.gameObject, summonPosition, Quaternion.identity);
        FoodMove move = food.GetComponent<FoodMove>();

        // 発射する方を向かせる
        food.transform.LookAt(pivotPos);

        // 発射
        move.AddForce(shotDirection, power);
    }

    public void AddReflectList(FoodMove self, FoodMove opponent)
    {
        // リストに何も無ければ追加
        if (reflectList.Count == 0)
        {
            reflectList.Add(new InfoForReflect(self.Rigidbody, self.ReflectRate, opponent.Rigidbody, opponent.ReflectRate));
            return;
        }
        // リストにすでに入っているとき
        foreach (InfoForReflect reflect in reflectList)
        {
            // リストに含まれているものでなければ追加
            if (!reflect.IsSame(self.Rigidbody, opponent.Rigidbody))
            {
                reflectList.Add(new InfoForReflect(self.Rigidbody, self.ReflectRate, opponent.Rigidbody, opponent.ReflectRate));// 追加
                return;
            }
        }
    }

    /// <summary>
    /// 衝突時の反射
    /// </summary>
    void Reflect(InfoForReflect reflectInfo)
    {
        Rigidbody baseRb = reflectInfo.First.Rigidbody.velocity.magnitude >= reflectInfo.Second.Rigidbody.velocity.magnitude ?
            reflectInfo.First.Rigidbody : reflectInfo.Second.Rigidbody;

        Vector3 baseVelocity = reflectInfo.First.Rigidbody.velocity + reflectInfo.Second.Rigidbody.velocity;// お互いの勢いを足す

        // 勢いを計算しやすいように変換
        baseVelocity /= BaseKeepReflectSpeedRate;// 後で食材ごとに%を変換しないで済むようにしている
        baseVelocity.y = 0f;// y方向の力は必要ないので無くしておく

        Vector3 firstVelocity = Vector3.zero;
        Vector3 secondVelocity = Vector3.zero;

        // それぞれの勢いの設定
        if (baseRb == reflectInfo.First.Rigidbody)
        {
            firstVelocity = baseVelocity * -reflectInfo.First.ReflectRate;
            secondVelocity = baseVelocity * reflectInfo.Second.ReflectRate;
        }
        else
        {
            firstVelocity = baseVelocity * reflectInfo.First.ReflectRate;
            secondVelocity = baseVelocity * -reflectInfo.Second.ReflectRate;
        }

        // 速度を加算
        reflectInfo.First.Rigidbody.velocity += firstVelocity;
        reflectInfo.Second.Rigidbody.velocity += secondVelocity;
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
            if (trackObject.Cursor.Team == acquirer.Team)// 取得したチームを見つける
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
