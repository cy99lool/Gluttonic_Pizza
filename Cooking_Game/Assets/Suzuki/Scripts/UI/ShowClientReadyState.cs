using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]// Imageが必須（なければ自動で作られる）
public class ShowClientReadyState : ReadyStateSyncBase
{
    [Header("色、ホストの準備状況確認でのみ設定"), SerializeField] TeamColor color;
    public TeamColor Color => color;

    [Header("準備状況を表示するImage"), SerializeField] Image stateImage;
    public Image StateImage => stateImage;

    ReadyState lastState = ReadyState.NotReady;
    // Resetや最初にオブジェクトにアタッチしたときに呼ばれる（継承元をoverride）
    protected override void Reset()
    {
        // 継承元のReset処理
        base.Reset();

        // アイコンのImageを取得
        if (stateImage == null)
        {
            stateImage = GetComponent<Image>();
        }

        lastState = ReadyState.NotReady;
    }

    void Update()
    {
        // nullチェック
        if (udpMulti == null) return;

        // 準備状況のアイコンを更新
        UpdateReadyStateIcon(udpMulti.Myinfo);
    }

    /// <summary>
    /// 準備状況のアイコンを更新
    /// </summary>
    public void UpdateReadyStateIcon(UDPMulti.ClientInfo clientInfo)
    {
        // 現在の準備状況を取得
        ReadyState currentState = clientInfo.ReadyState;

        // 自身の準備状況に応じてアイコンを切り替える（状況が切り替わったときだけ）
        ChangeReadyStateIcon(currentState);
    }

    /// <summary>
    /// 準備状況が切り替わったときだけ、アイコンを切り替える
    /// </summary>
    /// <param name="currentState"></param>
    void ChangeReadyStateIcon(ReadyState currentState)
    {
        // 準備状況が同じなら切り替えない
        if (currentState == lastState) return;

        // アイコン切り替え
        stateImage.sprite = currentState == ReadyState.Ready ? readyImage : notReadyImage;

        // 状況を更新
        lastState = currentState;
    }
}
