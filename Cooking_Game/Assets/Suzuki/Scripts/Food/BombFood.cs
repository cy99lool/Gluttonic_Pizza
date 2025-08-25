using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombFood : FoodMove
{
    [Header("発射してから爆発するまでの時間"), SerializeField] float triggerTime = 3f;
    [Header("爆発してからの無敵時間"), SerializeField] float immuneTime = 0.1f;
    [Header("爆風オブジェクト"), SerializeField] Explosion explosion;

    Rigidbody rb;
    new void Start()
    {
        base.Start();// 親クラスのStartを実行

        rb = GetComponent<Rigidbody>();
        StartCoroutine(BombBehavior());
    }

    /// <summary>
    /// 爆弾の振る舞い
    /// </summary>
    /// <returns></returns>
    IEnumerator BombBehavior()
    {
        // 指定時間待機
        yield return new WaitForSeconds(triggerTime);

        yield return Explode();// 爆発
    }

    /// <summary>
    /// 爆発
    /// </summary>
    /// <returns></returns>
    IEnumerator Explode()
    {
        // 先に吹き飛ばされないようにしておく
        rb.isKinematic = true;

        // 爆風オブジェクトを生成
        Instantiate(explosion, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(immuneTime);// 一定時間無敵
        rb.isKinematic = false;// 再び吹き飛ばされるように
    }
}
