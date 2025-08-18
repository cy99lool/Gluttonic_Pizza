using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodMove : MonoBehaviour
{
    const float BreakThreshold = 0.01f;

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

    public TeamColor Team => team;

    // Start is called before the first frame update
    protected void Start()
    {
        stageManager = FindAnyObjectByType<StageManager>();
        isGround = false;
    }

    protected void FixedUpdate()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * transform.lossyScale.y * GameConstants.HalfMultiplyer, Vector3.down);

        if (Physics.Raycast(groundRay, out RaycastHit groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask))// 地面についているとき
        //if (Physics.SphereCast(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, out RaycastHit groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask))// 地面についているとき

        {
            transform.position = groundHit.point;// 貫通対策

            StopFalling();

            EraseCheck();// 一定時間以上浮いていたら消す
        }

        if (Physics.Raycast(groundRay, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), brakeMask))// ブレーキをかけるレイヤーのとき
        //if (Physics.SphereCast(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, out groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), brakeMask))// ブレーキをかけるレイヤーのとき
        {
            // ピザの子にする
            if (transform.parent == null || transform.parent != groundHit.transform)
            {
                transform.parent = groundHit.transform;
            }

            Brake();// ブレーキ
            if (!isGround) isGround = true;// 接地開始

            if (eraseTimer > GameConstants.FirstTimerValue) eraseTimer = GameConstants.FirstTimerValue;// 消えないようにする
        }

        else// 浮いているとき
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
    }

    /// <summary>
    /// 落下を停止
    /// </summary>
    void StopFalling()
    {
        Vector3 velocity = myRb.velocity;
        velocity.y = 0f;
        myRb.velocity = velocity;
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
