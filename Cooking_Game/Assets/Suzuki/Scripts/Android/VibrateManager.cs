using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VibrateManager : MonoBehaviour
{
    [Header("チャージ完了時の振動時間(ミリ秒)"), SerializeField] int chargedVibrateTime = 20;

    bool vibratable;// 振動可能かどうか
    bool isVibrating = false;// 振動している最中かどうか

    /// <summary>
    /// 振動可能にする
    /// </summary>
    public void EnableVibrate()
    {
        vibratable = true;
    }


    public void Vibrate(VibrationSituations situation)
    {
        // 振動が無効化されていたり、振動中は振動させない
        if (!vibratable || isVibrating) return;

        // 振動をサポートしていない機器は振動させない
        if (!SystemInfo.supportsVibration) return;

        // 振動
        switch (situation)
        {
            case VibrationSituations.FullyCharged:
                StartCoroutine(VibrateCorutine(chargedVibrateTime));
                break;
            default:
                break;
        }
    }

    // Android設定
    AndroidJavaClass unityPlayer;
    AndroidJavaObject currentActivity;
    AndroidJavaObject vibrator = null;
    int sdkInt = GameConstants.Zero;

#if UNITY_ANDROID && !UNITY_EDITOR
    // 初期化を行う
    void Awake()
    {
        try
        {
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // SDKバージョンを取得
            sdkInt = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
            // Android12以降
            if (sdkInt >= 31)
            {
                var vibratorManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");
                vibrator = vibratorManager.Call<AndroidJavaObject>("getDefaultVibrator");
            }
            // それ以前のバージョン
            else vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        // 失敗時
        catch (System.Exception ex)
        {
            vibrator = null;
        }

        // デバッグログ
        Debug.Log($"[Vibrate Debug] SDK Version: {sdkInt}");
        if (vibrator == null) Debug.LogError("[Vibrate Debug] Vibratorの取得に失敗しました。サービス名が間違っている可能性があります。");
        else Debug.Log("[Vibrate Debug] Vibratorの取得に成功しました！");
    }

#endif

    const int DefaultAmplitude = 255;

    /// <summary>
    /// 実際に振動を命令する
    /// </summary>
    /// <param name="milliseconds">振動させる時間（ミリ秒）</param>
    void Vibrate(long milliseconds)
    {
        // Androidの振動
        if (IsAndroid)
        {
#if UNITY_ANDROID
            // きちんと取得できている場合
            if (vibrator != null)
            {
                // Android8.0以上
                if(sdkInt >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        // 振動エフェクトを作成
                        AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, DefaultAmplitude);
                        
                        // 振動
                        vibrator.Call("vibrate", effect);
                    }
                }
                else vibrator.Call("vibrate", milliseconds);
            }

            // 初期化に失敗したフォールバック
            //else Handheld.Vibrate();
#endif
        }
    }

    IEnumerator VibrateCorutine(long milliseconds)
    {
        // 振動中にする
        isVibrating = true;

        Vibrate(milliseconds);

        // 振動時間中は待機
        yield return new WaitForSeconds(milliseconds / GameConstants.MillisecondPerSecond);

        // 振動中でなくする
        isVibrating = false;
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