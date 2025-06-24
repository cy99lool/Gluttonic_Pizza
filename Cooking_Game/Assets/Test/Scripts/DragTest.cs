using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragTest : MonoBehaviour
{
    [Header("移動させるオブジェクト"), SerializeField] Transform trackObject;
    [Header("離してからの猶予"), SerializeField] float dragWaitTime = 0.1f;
    //[Header("基準点"), SerializeField] Transform pivot;
    //[Header("伸ばせる最大距離"), SerializeField] float maxDistance = 7f;
    //[SerializeField] float basePower = 5f;
    /*[Header("食べ物"), SerializeField] FoodMove foodPrefab;*/// 将来的に他から食べ物のプレハブを指定するように変更するかも
    [Header("自分の視点"), SerializeField] Camera view;
    //[Header("掴むレイヤー"), SerializeField] LayerMask grabLayerMask;
    [Header("動かせる場所レイヤー"), SerializeField] LayerMask movableLayerMask;
    
    StageManager stageManager;
    float timer = 0f;
    bool downHolding = false;
    bool released = false;
    Vector3 startPos;
    Vector3 movePos;

    void Start()
    {
        startPos = trackObject.position;
        stageManager = FindObjectOfType<StageManager>();
    }
    void Update()
    {
        Touch();
        if (downHolding)
        {
            trackObject.position = movePos;
            //Debug.Log("rate:" + calcRate(new Vector2(trackObject.position.x, trackObject.position.z)));
        }

        if (downHolding && released)
        {
            if (timer >= dragWaitTime)
            {
                downHolding = false;
                //Shot();// 発射
                trackObject.position = startPos;// 位置を戻している
                timer = 0f;
                //Debug.Log("発射！");
            }

            timer += Time.deltaTime;
        }
    }

    //public void OnTouch(InputAction.CallbackContext context)// 継続して位置を取れない
    //{
    //    if (context.phase == InputActionPhase.Performed)
    //    {
    //        RaycastHit hit;
    //        if (Physics.Raycast(view.ScreenPointToRay(context.ReadValue<Vector2>()), out hit, 100))// カメラ上の触れた位置
    //        {
    //            trackObject.transform.position = hit.point;
    //            Debug.Log(hit.point);
    //        }

    //        if (!downHolding) downHolding = true;
    //        released = false;
    //        Debug.Log("Performed");
    //    }
    //    if (context.phase == InputActionPhase.Canceled)
    //    {
    //        if (!released) released = true;
    //    }
    //}

    void Shot()
    {
        float magnification = 2f;// 係数
        //float rate = calcRate(new Vector2(trackObject.position.x, trackObject.position.z));// 伸び具合
        //float power = basePower * rate * magnification;

        // オブジェクトの生成、発射
        //GameObject food = Instantiate(foodPrefab.gameObject, trackObject.position + Vector3.up * 0.5f, Quaternion.identity);
        //FoodMove move = food.GetComponent<FoodMove>();

        //Vector3 shotDirection = (pivot.position - trackObject.position).normalized;// 方向を設定
        //move.AddForce(shotDirection, power);// 発射

        //Vector3 shotDirection = (pivot.position - trackObject.position).normalized;// 方向を設定
        //shotDirection.y = 0f;
        //stageManager.SummonAndShotFood(foodPrefab, trackObject.position/* + Vector3.up * 0.5f*/, shotDirection, power);

        // デバッグ用
        //Debug.Log($"power:{power},direction:{shotDirection},final:{shotDirection * power}");
    }

    // 割合を計算
    //float calcRate(Vector2 target)
    //{
    //    Vector2 distanceVector = new Vector2(pivot.position.x - target.x, pivot.position.z - target.y);
    //    float squaredDistance = distanceVector.x * distanceVector.x + distanceVector.y * distanceVector.y;// 距離の二乗(-をなくすため)
    //    float rate = squaredDistance / (maxDistance * maxDistance);
    //    if (rate > 1f) rate = 1f;
    //    return rate;
    //}

    // ドラッグをしているかどうか
    void Touch()
    {
        if (Input.GetMouseButton(0))// 押されている間
        {
            //// 掴みはじめ
            //if (!downHolding)
            //{
            //    RaycastHit grabHit;
            //    if (Physics.Raycast(view.ScreenPointToRay(Input.mousePosition), out grabHit, 100, grabLayerMask))// Rayが衝突したら（LayerMaskを追加する）
            //    {
            //        if (grabHit.transform.gameObject == trackObject.gameObject)
            //        {
            //            downHolding = true;
            //            released = false;
            //        }
            //    }
            //}
            // 掴んでいる間
            //else
            //{
            RaycastHit moveHit;
            if (Physics.Raycast(view.ScreenPointToRay(Input.mousePosition), out moveHit, 100, movableLayerMask))// 動かす先を見つけたら
            {
                movePos = moveHit.point;// 移動先を設定
                if (timer > 0f) timer = 0f;// 猶予をリセット
                if (!downHolding)// 掴み始め
                {
                    downHolding = true;
                    released = false;
                }
            }
            //}
        }
        if (Input.GetMouseButtonUp(0))// 離したとき
        {
            if (!released) released = true;
        }
    }
}
