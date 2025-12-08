using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodExplosion : MonoBehaviour
{
    [Header("消去されるまでの時間"), SerializeField] float destroySeconds = 1f;
    [Header("--- 爆発設定 ---")]
    [Header("ふき飛ばす食べ物"), SerializeField] List<FoodMove> foods;
    [Header("吹き飛ばす威力（横方向）"), SerializeField] float horizontalBaseFactor = 1f;
    [Header("吹き飛ばす威力（縦方向）"), SerializeField] float verticalBaseFactor = 5f;
    void Start()
    {
        AddVelocityAllFoods();

        StartCoroutine(DestroyAfterSeconds(destroySeconds));
    }

    void AddVelocityAllFoods()
    {
        foreach(FoodMove food in foods)
        {
            // 射出方向と勢いの決定
            Vector3 velocity = (food.transform.position - transform.position).normalized;
            velocity *= horizontalBaseFactor;
            velocity.y += verticalBaseFactor;


            food.Rigidbody.AddForce(velocity, ForceMode.Impulse);
        }
    }

    IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Destroy(gameObject);
    }
}
