using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldItem : MonoBehaviour
{
    [Header("親になるレイヤー"), SerializeField] LayerMask groundMask;
    [Header("床についた判定までの距離"), SerializeField] float onGroundDistance = 3f;
    [Header("アイテムを獲得した人が使用可能になるパワーアップ"), SerializeField] CursorInfo.Mode mode;
    public CursorInfo.Mode Mode => mode;

    StageManager stageManager;

    void Start()
    {
        stageManager = FindObjectOfType<StageManager>();
    }

    void FixedUpdate()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * GameConstants.HalfMultiplyer, Vector3.down);

        // 地面についているとき
        if (Physics.SphereCast(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, out RaycastHit groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask))
        {
            transform.position = groundHit.point + Vector3.up * transform.lossyScale.y * GameConstants.HalfMultiplyer;// 地面につける
            transform.parent = groundHit.transform;// 親に設定
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<FoodMove>(out FoodMove food))
        {
            stageManager.OnAcquireItem(this, food);// アイテム獲得
        }
    }
}
