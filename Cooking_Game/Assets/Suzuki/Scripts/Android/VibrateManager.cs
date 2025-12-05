using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrateManager : MonoBehaviour
{
    [Header("チャージ完了時の振動時間(ミリ秒)"), SerializeField] int chargedVibrateTime = 20;

    bool vibratable; // 振動可能かどうか

    /// <summary>
    /// 振動可能にする
    /// </summary>
    public void EnableVibrate()
    {
        vibratable = true;
    }


    public void Vibrate(VibrationSituations situation)
    {
        // 振動が無効化されていたら振動させない
        if (!vibratable) return;

        // 振動をサポートしていない機器は振動させない
        if (!SystemInfo.supportsVibration) return;

        // 振動
        switch (situation)
        {
            case VibrationSituations.FullyCharged:
                Vibrate(chargedVibrateTime);
                break;
            default:
                break;
        }
    }

    // Android設定
    AndroidJavaClass unityPlayer;
    AndroidJavaObject currentActivity;
    AndroidJavaObject vibrator = null;

#if UNITY_ANDROID && !UNITY_EDITOR
    // 初期化を行う
    void Awake()
    {
        try
        {
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        // 失敗時
        catch(System.Exception ex)
        {
            vibrator = null;
        }
    }

#endif

    /// <summary>
    /// 実際に振動を命令する
    /// </summary>
    /// <param name="milliseconds">振動させる時間（ミリ秒）</param>
    void Vibrate(long milliseconds)
    {
        // Androidの振動
        if (IsAndroid)
        {
            // きちんと取得できている場合
            if (vibrator != null) vibrator.Call("vibrate", milliseconds);

            // 初期化に失敗したフォールバック
            else Handheld.Vibrate();
        }
    }

    // Androidかどうか
    public bool IsAndroid
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif

        }
    }
}

/// <summary>
/// 振動を発生させる状況
/// </summary>
public enum VibrationSituations
{
    FullyCharged = 1
}