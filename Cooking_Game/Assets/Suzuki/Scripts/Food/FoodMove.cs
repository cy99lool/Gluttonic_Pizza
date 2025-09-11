using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodMove : MonoBehaviour
{
    const float BreakThreshold = 0.01f;
    const float OverlapSphereRadius = 0f;// Rayの始点が当たり判定の内部にあるかを調べるためのものなので、点にしている

    [SerializeField] Rigidbody myRb;
    public Rigidbody Rigidbody => myRb;

    [Header("重力"), SerializeField] float gravity = 9.8f;
    [Header("一秒あたりの減速率"), SerializeField] float brakeRate = 1.8f;
    [Header("地面についている判定の距離"), SerializeField] float onGroundDistance = 0.15f;
    [Header("消えるまでの時間"), SerializeField] float eraseLimit = 5f;
    [Header("ブレーキをかけるレイヤー"), SerializeField] LayerMask brakeMask;
    [Header("落下しないレイヤー"), SerializeField] LayerMask groundMask;
    [Header("ぶつかるレイヤー"), SerializeField] LayerMask hitMask;
    [Header("ぶつかったときの反射率（%）"), Range(0f, 100f), SerializeField] float myReflectRate = 90f;
    public float ReflectRate => myReflectRate;

    // ステータスとして他のクラスにまとめるかも（ポイントの倍率等を設定する場合もあるかも）
    [Header("チーム"), SerializeField] TeamColor team;
    [Header("入手されるときのポイント"), SerializeField] int point = 10;
    public int ScorePoint => point;

    StageManager stageManager;
    float eraseTimer = GameConstants.FirstTimerValue;

    float BrakePower => 1f - brakeRate * Time.deltaTime;

    bool isGround = false;
    protected bool IsGround => isGround;

    bool isFalling = false;

    public TeamColor Team => team;

    // Start is called before the first frame update
    protected void Start()
    {
        stageManager = FindAnyObjectByType<StageManager>();
        isGround = false;
    }

    protected void FixedUpdate()
    {
        FallUpdate();

    }

    void FallUpdate()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * transform.lossyScale.y * GameConstants.HalfMultiplyer, Vector3.down);

        // ========================================================================================================
        // 接地点の取得
        Collider groundHit = null;

        // 自身に重なっている床を調べる
        Collider[] overlapColliders = Physics.OverlapSphere(groundRay.origin, OverlapSphereRadius, groundMask);

        // 床が重なっていた場合
        if (overlapColliders.Length > 0)
        {
            for (int i = 0; i < overlapColliders.Length; i++)
            {
                // 減速させるレイヤー（例：ピザのレイヤー）があった場合、そちらを優先して適用する
                if (CompareLayer(brakeMask, overlapColliders[i].gameObject.layer))
                {
                    groundHit = overlapColliders[i];
                    break;
                }

                // 減速させるレイヤーがなかった場合、落下を停止するだけのレイヤーのものを適用する
                groundHit = overlapColliders[i];
            }
        }

        // 床が重なっていなかった場合
        else
        {
            RaycastHit[] hits = Physics.SphereCastAll(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask);

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    // 減速させるレイヤー（例：ピザのレイヤー）があった場合、そちらを優先して適用する
                    if (CompareLayer(brakeMask, hits[i].collider.gameObject.layer))
                    {
                        groundHit = hits[i].collider;
                        break;
                    }
                    // 減速させるレイヤーがなかった場合、落下を停止するだけのレイヤーのものを適用する
                    groundHit = hits[i].collider;
                }
            }
        }


        //if (Physics.Raycast(groundRay, out RaycastHit groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask))// 地面についているとき
        // 地面についているとき    
        if (groundHit != null)
        {
            if (isFalling)
            {
                // 着地点の設定
                Vector3 hitPos = groundHit.ClosestPoint(transform.position);
                hitPos.y += transform.localScale.y * GameConstants.HalfMultiplyer;// 貫通対策

                // 着地点に位置を設定
                transform.position = hitPos;

                StopFalling();

                EraseCheck();// 一定時間以上浮いていたら消す
            }
        }

        //if (Physics.Raycast(groundRay, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), brakeMask))// ブレーキをかけるレイヤーのとき
        //if (Physics.SphereCast(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, out groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), brakeMask))// ブレーキをかけるレイヤーのとき

        // ブレーキをかける床の上のとき
        if (groundHit != null && CompareLayer(brakeMask, groundHit.gameObject.layer))
        {
            // ピザの子にする
            if (transform.parent == null || transform.parent != groundHit.transform)
            {
                transform.parent = groundHit.transform;
            }

            Brake();// ブレーキ
            if(!isGround) isGround = true;// 接地開始


            if (eraseTimer > GameConstants.FirstTimerValue) eraseTimer = GameConstants.FirstTimerValue;// 消えないようにする
        }
        
        // 浮いているとき
        if(groundHit == null)
        {
            // 親がピザなら親子づけを外す
            if (transform.parent != null && CompareLayer(brakeMask, transform.parent.gameObject.layer))
            {
                transform.parent = null;
            }

            Fall();
            if (isGround) isGround = false;// 接地中断

            EraseCheck();// 一定時間以上浮いていたら消す
        }
    }

    void EraseCheck()
    {
        eraseTimer += Time.deltaTime;
        if (eraseTimer >= eraseLimit) Destroy(gameObject);
    }

    public virtual void AddForce(Vector3 direction, float power)
    {
        myRb.velocity += direction * power;
        Debug.Log($"direction:{direction}, power:{power}, velocity:{myRb.velocity}");
    }

    public void SetVelocity(Vector3 velocity)
    {
        myRb.velocity = velocity;
    }

    void Fall()
    {
        if (myRb.velocity.y == -gravity) return;
        Vector3 velocity = myRb.velocity;
        velocity.y = velocity.y > 0f ? velocity.y - gravity : -gravity;
        myRb.velocity = velocity;

        if (!isFalling) isFalling = true;
    }

    /// <summary>
    /// 落下を停止
    /// </summary>
    void StopFalling()
    {
        Vector3 velocity = myRb.velocity;
        velocity.y = 0f;
        myRb.velocity = velocity;

        if (isFalling) isFalling = false;
    }

    /// <summary>
    /// 時間による減速を行う
    /// </summary>
    void Brake()
    {
        Vector3 velocity = myRb.velocity;

        velocity.x *= BrakePower;
        velocity.z *= BrakePower;

        if (velocity.x * velocity.x <= BreakThreshold && velocity.z * velocity.z <= BreakThreshold) velocity = Vector3.zero;// 停止

        myRb.velocity = velocity;
    }

    void OnTriggerEnter(Collider other)
    {
        if (CompareLayer(hitMask, other.gameObject.layer))
        {
            // 衝突時の処理（エフェクトの再生等、マネージャーに衝突を知らせるだけにする予定（お互いで衝突処理が呼び出されて異常な速度でふっとばし合うため））
            if (other.gameObject.TryGetComponent<FoodMove>(out FoodMove opponentFood))// 相手が食べ物なら
            {
                //Reflect(myRb, oppoentRb);
                stageManager.AddReflectList(this, opponentFood);
            }
        }
    }

    /// <summary>
    /// レイヤーマスクにレイヤーが含まれているかどうか確認する
    /// </summary>
    bool CompareLayer(LayerMask layerMask, int layer)
    {
        return ((1 << layer) & layerMask) != 0;
    }

    protected void SetReflectRate(float rate)
    {
        myReflectRate = rate;
    }

    public enum TeamColor
    {
        Red = 0,
        Blue,
        Green,
        Yellow,
        AllSize
    }
}
