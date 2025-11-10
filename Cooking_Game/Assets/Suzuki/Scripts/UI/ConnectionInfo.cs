using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConnectionInfo : MonoBehaviour
{
    [Header("--- アイコン設定 ---")]
    [Header("接続待ちアイコン"), SerializeField] Image connectWaitIcon;
    [Header("接続済みアイコン"), SerializeField] Image connectedIcon;
    [Header("--- テキスト設定 ---")]
    [Header("接続状況テキスト"), SerializeField] TextMeshProUGUI statusText;
    [Header("接続待ちテキストの「」▶「...」1ループの長さ"), SerializeField] float waitTextAnimationLength;

    bool connect = false;
    string baseText = "接続待ち";
    string animationText;
    float textTimer = GameConstants.FirstTimerValue;
    float nextTextAnimationTime;
    int animationCount = GameConstants.Zero;// 現在のコマ

    const int TextAnimationAmount = 4;// テキストのアニメーションのコマ数
    void Start()
    {
        // アニメーションのコマごとの時間を設定
        nextTextAnimationTime = waitTextAnimationLength / TextAnimationAmount;

        animationCount = GameConstants.Zero;
    }

    void Update()
    {
        // 接続中はアニメーションを行わないためreturn
        if (connect) return;

        // テキストのアニメーション
        animationText = UpdateTextAnimation();

        // テキストに反映
        statusText.text = baseText + animationText;
    }

    string UpdateTextAnimation()
    {
        textTimer += Time.deltaTime;

        // アニメーション変更
        if (textTimer >= nextTextAnimationTime)
        {
            animationCount++;

            // アニメーションのループ
            if (animationCount == TextAnimationAmount)
            {
                animationCount = GameConstants.Zero;
            }

            // タイマーの初期化
            textTimer = GameConstants.FirstTimerValue;
        }

        // アニメーションをテキストに反映
        string returnText = "";

        for(int i = 0; i < animationCount; i++)
        {
            returnText += ".";
        }

        return returnText;
    }

    /// <summary>
    /// 接続時
    /// </summary>
    public void OnConnect()
    {
        connect = true;

        // 接続済みテキストへ変更
        baseText = "接続済み";

        // 接続済みアイコンへ変化
        connectWaitIcon.gameObject.SetActive(false);
        connectedIcon.gameObject.SetActive(true);
    }

    /// <summary>
    /// 切断時
    /// </summary>
    public void OnDisconnect()
    {
        connect = false;

        // 接続待ちテキストへ変更
        baseText = "接続待ち";
        textTimer = GameConstants.FirstTimerValue;

        // 接続待ちアイコンへ変化
        connectWaitIcon.gameObject.SetActive(true);
        connectedIcon.gameObject.SetActive(false);
    }
}
