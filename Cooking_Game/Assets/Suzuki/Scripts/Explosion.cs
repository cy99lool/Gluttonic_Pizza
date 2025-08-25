using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    const float UpwardsModifier = 0f;// 爆風で持ち上がっているように見せる強さ

    //[Header("吹き飛ばせるレイヤー"), SerializeField] LayerMask hitLayer;
    [Header("爆発の威力"), SerializeField] float power;
    //[Header("爆発範囲"), SerializeField] float radius = 5f;
    [Header("爆発判定の持続時間"), SerializeField] float explodeTime = 0.01f;
    [Header("消去するまでの時間"), SerializeField] float destroyTime = 3f;

    Collider explodeCollider;
    List<FoodMove> blewAwayFoodList = new List<FoodMove>();
    RaycastHit[] hits;
    void Start()
    {
        explodeCollider = GetComponent<Collider>();
        StartCoroutine(ExplodeBehavior());
    }

    //void FixedUpdate()
    //{
    //    hits = (Physics.SphereCastAll(transform.position, radius, transform.forward, hitLayer));
    //    foreach(RaycastHit hit in hits)
    //    {
    //        if (!TryGetComponent(out FoodMove food)) return;// 食べ物以外は処理しない

    //        for (int i = 0; i < blewAwayFoodList.Count; i++)
    //        {
    //            if (blewAwayFoodList[i] == food) return;// すでに吹き飛ばしているなら処理しない
    //        }

    //        Rigidbody rb;
    //        if (hit.transform.TryGetComponent<Rigidbody>(out rb))
    //        {
    //            rb.AddExplosionForce(power, transform.position, transform.localScale.x);// 爆風で吹き飛ばす
    //        }
    //    }
    //}

    IEnumerator ExplodeBehavior()
    {
        float timer = GameConstants.FirstTimerValue;
        while (timer <= destroyTime)
        {
            if (timer >= explodeTime && explodeCollider.enabled) explodeCollider.enabled = false;// 当たり判定を消去
            timer += Time.deltaTime;// 時間を経過
            yield return null;
        }
        Destroy(gameObject);// 自身を消去
    }

    void OnTriggerStay(Collider other)
    {
        FoodMove food;
        if (!other.TryGetComponent(out food)) return;// 食べ物以外は処理しない

        for (int i = 0; i < blewAwayFoodList.Count; i++)
        {
            if (blewAwayFoodList[i] == food) return;// すでに吹き飛ばしているなら処理しない
        }

        Rigidbody rb;
        if (other.TryGetComponent<Rigidbody>(out rb))
        {
            rb.AddExplosionForce(power, transform.position, transform.localScale.x, UpwardsModifier, ForceMode.Impulse);// 爆風で吹き飛ばす
            blewAwayFoodList.Add(food);// すでに吹き飛ばした対象に追加
        }
    }
}
