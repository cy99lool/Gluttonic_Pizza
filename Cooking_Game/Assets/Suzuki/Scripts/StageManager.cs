using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [System.Serializable]
    class InfoForReflect
    {
        Rigidbody firstRb;
        Rigidbody secondRb;
        float reflectRate;

        public Rigidbody FirstRb => firstRb;
        public Rigidbody SecondRb => secondRb;
        public float ReflectRate => reflectRate;

        public InfoForReflect(Rigidbody firstRb, Rigidbody secondRb, float reflectRate)
        {
            this.firstRb = firstRb;
            this.secondRb = secondRb;
            this.reflectRate = reflectRate;
        }
        /// <summary>
        /// 同じ情報かを確かめる（順番が違うだけのものも同じとする）
        /// </summary>
        public bool IsSame(Rigidbody firstRb, Rigidbody secondRb)
        {
            if (firstRb == this.firstRb || firstRb == this.secondRb) return true;
            if (secondRb == this.firstRb || secondRb == this.secondRb) return true;

            return false;
        }
        // 反射レートを設定（現状大小を比較して反射率の小さい方に設定している）
        public void SetReflectRate(float rate)
        {
            reflectRate = reflectRate >= rate ? rate : reflectRate;
        }
    }

    [System.Serializable]
    class TrackObject
    {
        [Header("移動させるオブジェクト"), SerializeField] Transform trackObject;
        [Header("基準点"), SerializeField] Transform pivot;
        [Header("食べ物"), SerializeField] FoodMove foodPrefab;
        [Header("伸ばせる最大距離"), SerializeField] float maxDistance = 7f;
        [SerializeField] float basePower = 20f;

        Vector3 startPos;
        Vector3 lastPos;
        float magnification = 2f;// 係数
        public FoodMove FoodPrefab => foodPrefab;

        // 具材を飛ばす力
        public float Power => basePower * magnification * calcRate(new Vector2(TrackPosition.x, TrackPosition.z));

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
        public bool IsDragging => trackObject.transform.position != startPos;

        // 動いているか
        public bool IsMoving => trackObject.transform.position != lastPos;

        // 最後の位置は動いた先で、オブジェクトが初期位置に戻ったときに真になる
        public bool Released => lastPos != startPos && trackObject.transform.position == startPos;
        public void SetStartPos()
        {
            startPos = trackObject.position;
            lastPos = startPos;
        }
        public void UpdateLastPosition()
        {
            lastPos = trackObject.position;
        }

        float calcRate(Vector2 target)
        {
            Vector2 distanceVector = new Vector2(pivot.position.x - target.x, pivot.position.z - target.y);
            float squaredDistance = distanceVector.x * distanceVector.x + distanceVector.y * distanceVector.y;// 距離の二乗(-をなくすため)
            float rate = squaredDistance / (maxDistance * maxDistance);
            if (rate > 1f) rate = 1f;
            return rate;
        }
    }

    [SerializeField] List<TrackObject> trackObjects = new List<TrackObject>();
    [SerializeField] List<InfoForReflect> reflectList = new List<InfoForReflect>();

    void Start()
    {
        for(int i = 0; i < trackObjects.Count; i++)
        {
            trackObjects[i].SetStartPos();
        }
    }

    void FixedUpdate()
    {
        for(int i = 0; i < trackObjects.Count; i++)
        {
            // 指が離されて、発射されるとき
            if (trackObjects[i].Released)
            {
                // 具材を生成して発射
                SummonAndShotFood(trackObjects[i].FoodPrefab, trackObjects[i].TrackPosition + Vector3.up * 0.5f, trackObjects[i].ShotDirection, trackObjects[i].Power);
            }
            if(trackObjects[i].IsMoving)
            {
                // 動かしているときのエフェクトを入れる予定
                Debug.Log("moving");
            }
            // ドラッグ中
            if (trackObjects[i].IsDragging)
            {
                // 矢印の方向や距離のベクトルを求める（y軸方向は除く）
                Vector3 arrowDirection = trackObjects[i].ShotVector;
                arrowDirection.y = 0f;

                // 矢印を変形
                if(arrowDirection != Vector3.zero)
                {
                    
                }
            }

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

    public void SummonAndShotFood(FoodMove foodPrefab, Vector3 summonPosition, Vector3 shotDirection, float power)
    {
        // 具材の生成
        GameObject food = Instantiate(foodPrefab.gameObject, summonPosition, Quaternion.identity);
        FoodMove move = food.GetComponent<FoodMove>();

        // 発射
        move.AddForce(shotDirection, power);
    }

    public void AddReflectList(Rigidbody myRb, Rigidbody opponentRb, float reflectRate)
    {
        // リストに何も無ければ追加
        if (reflectList.Count == 0)
        {
            reflectList.Add(new InfoForReflect(myRb, opponentRb, reflectRate));
            return;
        }
        // リストにすでに入っているとき
        foreach (InfoForReflect reflect in reflectList)
        {
            // リストに含まれているものでなければ追加
            if (!reflect.IsSame(myRb, opponentRb))
            {
                reflectList.Add(new InfoForReflect(myRb, opponentRb, reflectRate));// 追加
                return;
            }
            if (reflect.IsSame(myRb, opponentRb) && reflect.ReflectRate != reflectRate)
            {
                reflect.SetReflectRate(reflectRate);// 現在反射レートの小さい方に設定される、高反発なものがあるなら計算方法を変える
                return;
            }
        }
    }

    /// <summary>
    /// 衝突時の反射
    /// </summary>
    void Reflect(InfoForReflect reflectInfo)
    {
        Rigidbody baseRb = reflectInfo.FirstRb.velocity.magnitude >= reflectInfo.SecondRb.velocity.magnitude ?
            reflectInfo.FirstRb : reflectInfo.SecondRb;
        Vector3 baseVelocity = baseRb.velocity;

        baseVelocity.y = 0f;// y方向の力は必要ないので無くしておく

        Vector3 firstVelocity = Vector3.zero;
        Vector3 secondVelocity = Vector3.zero;

        //それぞれの勢いの設定
        if (baseRb == reflectInfo.FirstRb)
        {
            firstVelocity = baseVelocity * -reflectInfo.ReflectRate;// 反射レートに応じた勢い
            secondVelocity = baseVelocity * (1 - reflectInfo.ReflectRate);// 反射した残りの勢い
        }
        else
        {
            firstVelocity = baseVelocity * (1 - reflectInfo.ReflectRate);// 反射レートに応じた勢い
            secondVelocity = baseVelocity * -reflectInfo.ReflectRate;// 反射した残りの勢い
        }

        // 速度をセット
        reflectInfo.FirstRb.velocity = firstVelocity;
        reflectInfo.SecondRb.velocity = secondVelocity;
    }
}
