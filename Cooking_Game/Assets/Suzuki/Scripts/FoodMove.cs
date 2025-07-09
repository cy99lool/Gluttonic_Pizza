using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodMove : MonoBehaviour
{
    [SerializeField]Rigidbody myRb;
    [Header("重力"), SerializeField] float gravity = 0.98f;
    [Header("一秒あたりの減速率"), SerializeField] float brakeRate = 0.4f;
    [Header("地面についている判定の距離"), SerializeField] float onGroundDistance = 0.2f;
    [Header("消えるまでの時間"), SerializeField] float eraseLimit = 5f;
    [Header("ブレーキをかけるレイヤー"), SerializeField] LayerMask brakeMask;
    [Header("落下しないレイヤー"), SerializeField] LayerMask groundMask;
    [Header("ぶつかるレイヤー"), SerializeField] LayerMask hitMask;
    [Header("ぶつかったときの速度保持率"), Range(0f, 1f), SerializeField] float myReflectRate = 0.4f;
    [Header("チーム"), SerializeField] TeamColor team;

    StageManager stageManager;
    float eraseTimer = 0f;

    float BrakePower => 1f - brakeRate * Time.deltaTime;

    public TeamColor Team => team;

    // Start is called before the first frame update
    void Start()
    {
        stageManager = FindAnyObjectByType<StageManager>();
    }

    void FixedUpdate()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        float size = 0.5f;

        if(Physics.Raycast(groundRay, out RaycastHit groundHit, onGroundDistance + size, groundMask))// 地面についているとき
        {
            transform.position = groundHit.point;// 貫通対策
            
            StopFalling();

            EraseCheck();// 一定時間以上浮いていたら消す
        }

        if (Physics.Raycast(groundRay, onGroundDistance + size, brakeMask))// ブレーキをかけるレイヤーのとき
        {
            // ピザの子にする
            if (transform.parent == null || transform.parent != groundHit.transform)
            {
                transform.parent = groundHit.transform;
            }

            Brake();// ブレーキ

            if (eraseTimer > 0f) eraseTimer = 0f;// 消えないようにする
        }

        else// 浮いているとき
        {
            // 親がピザなら親子づけを外す
            if(CompareLayer(brakeMask, transform.parent.gameObject.layer))
            {
                transform.parent = null;
            }

            Fall();

            EraseCheck();// 一定時間以上浮いていたら消す
        }

    }

    void EraseCheck()
    {
        eraseTimer += Time.deltaTime;
        if (eraseTimer >= eraseLimit) Destroy(gameObject);
    }

    public void AddForce(Vector3 direction, float power)
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

        if (velocity.x <= 0.01f && velocity.z <= 0.01f) velocity = Vector3.zero;// 停止

        myRb.velocity = velocity;
    }

    void OnTriggerEnter(Collider other)
    {
        if (CompareLayer(hitMask, other.gameObject.layer))
        {
            // 衝突時の処理（エフェクトの再生等、マネージャーに衝突を知らせるだけにする予定（お互いで衝突処理が呼び出されて異常な速度でふっとばし合うため））
            if (other.gameObject.TryGetComponent<Rigidbody>(out Rigidbody opponentRb))// 相手にもRigidBodyがあるなら
            {
                //Reflect(myRb, oppoentRb);
                stageManager.AddReflectList(myRb, opponentRb, myReflectRate);
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

    public enum TeamColor
    {
        Red = 0,
        Blue,
        Green,
        Yellow,
        AllSize
    }
}
