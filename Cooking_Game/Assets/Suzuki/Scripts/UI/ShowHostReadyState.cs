using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShowHostReadyState : ReadyStateSyncBase
{
    [Header("それぞれのプレイヤーの準備状況"), SerializeField] ShowClientReadyState[] playerReadyStates;

    // プレイヤーの親オブジェクト名
    const string PlayerParentName = "Players";

    // 有効化されていないオブジェクトも対象に含めるか
    const bool IncludeInactive = true;

    protected override void Reset()
    {
        // 継承元のReset
        base.Reset();

        // プレイヤーの準備状況を表示するクラスたちを取得する（有効化状態でないものも全て含む）
        // プレイヤーの親を取得
        Transform playerParent = transform.Find(PlayerParentName);

        // nullチェック
        if (playerParent == null) return;

        // ヒエラルキーの順序通りに取得
        playerReadyStates = playerParent.GetComponentsInChildren<ShowClientReadyState>(IncludeInactive).OrderBy(player => player.transform.GetSiblingIndex()).ToArray();
    }

    void Update()
    {
        // nullチェック
        if (udpMulti == null) return;

        // プレイヤーごとに準備完了状況アイコンを更新
        foreach (ShowClientReadyState player in playerReadyStates) player.UpdateReadyStateIcon(udpMulti.GetTargetClient(player.Color));
    }
}
