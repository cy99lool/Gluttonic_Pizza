using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadyStateSyncBase : MonoBehaviour
{
    [Header("UDP通信に利用しているクラス"), SerializeField] protected UDPMulti udpMulti;

    [Header("--- アイコン設定 ---")]
    [Header("エントリーを促す画像"), SerializeField] protected Sprite notReadyImage;
    [Header("準備完了状態の画像"), SerializeField] protected Sprite readyImage;

    // 有効化されていないオブジェクトも対象に含めるか
    const bool IncludeInactive = true;

    // Resetや最初にオブジェクトにアタッチしたときに呼ばれる（override可能）
    protected virtual void Reset()
    {
        // nullでなければ再設定しない
        if (udpMulti != null) return;

        // 取得できなかった場合、親を一つづつ遡って調べる
        Transform current = transform;
        while (current != null)
        {
            //Debug.Log("階層：" + current.name);

            // その階層にUDPMultiがあるか調べる
            UDPMulti foundComponent = current.GetComponentInChildren<UDPMulti>(IncludeInactive);

            // あるならそれを設定
            if (foundComponent != null)
            {
                udpMulti = foundComponent;
                break;
            }

            // 一つ上の親へ（親がなければnullとなる）
            current = current.parent;
        }

        if (udpMulti == null) Debug.LogWarning("親をすべて調べても見つかりませんでした。");
    }
}