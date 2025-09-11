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
    //[SerializeField] Transform pivot;
    //[Header("掴むレイヤー"), SerializeField] LayerMask grabLayerMask;
    [Header("動かせる場所レイヤー"), SerializeField] LayerMask movableLayerMask;

    StageManager stageManager;
    float timer = 0f;
    bool downHolding = false;
    bool released = false;
    Vector3 startPos;
    Vector3 movePos;
    CursorInfo cursorInfo;

    void Start()
    {
        startPos = trackObject.position;
        stageManager = FindObjectOfType<StageManager>();
        cursorInfo = trackObject.GetComponent<CursorInfo>();
        //if(pivot != null) bowSetting.SetBowPos(pivot.position);
    }
    void Update()
    {
        // ドラッグしているかどうか
        Touch();

        // 長押し中
        if (downHolding)
        {
            trackObject.position = movePos;
            //Debug.Log("rate:" + calcRate(new Vector2(trackObject.position.x, trackObject.position.z)));
        }

        // 離したとき
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
            Ray moveRay = view.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(moveRay, out moveHit, 100, movableLayerMask))// 動かす先を見つけたら
            {
                if (!downHolding)// 掴み始め
                {
                    // UIに被っているときはドラッグを開始しない
                    // マウス
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }
                    // スマホ
                    if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    {
                        return;
                    }

                    downHolding = true;
                    released = false;
                }
                movePos = moveHit.point;// 移動先を設定

                if (timer > 0f) timer = 0f;// 猶予をリセット

            }
            //}
        }
        if (Input.GetMouseButtonUp(0))// 離したとき
        {
            if (!released)
            {
                released = true;
            }
        }
    }

    /// <summary>
    /// 巨大化ボタンを押したとき
    /// </summary>
    public void OnFoodGrow()
    {
        stageManager.FoodGrow(cursorInfo);
    }

    /// <summary>
    /// 爆弾化ボタンを押したとき
    /// </summary>
    public void OnFoobBomb()
    {
        stageManager.FoodChangeBomb(cursorInfo);
    }
}
