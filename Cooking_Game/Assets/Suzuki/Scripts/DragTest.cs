using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DragTest : MonoBehaviour
{
    [Header("移動させるオブジェクト"), SerializeField] Transform trackObject;
    [Header("離してからの猶予"), SerializeField] float dragWaitTime = 0.1f;
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

    // ドラッグをしているかどうか
    void Touch()
    {
        if (Input.GetMouseButton(0))// 押されている間
        {
            // 特定の場所を押したらドラッグするときにはこちらを使う
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

            // ドラッグできる場所ならどこからでもドラッグを開始できる
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
