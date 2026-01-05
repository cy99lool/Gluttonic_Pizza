using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 振動のデータ
/// </summary>
[System.Serializable]
public class VibrationData
{
    public enum VibrationType { OneShot, Waveform}

    [Header("振動の種類（単発/ループ）"), SerializeField] VibrationType type = VibrationType.OneShot;
    public VibrationType Type => type;

    [Header("振動の最小の強さ"), SerializeField, Range(0, 255)] int minPower = 0;
    public int MinPower => minPower;

    [Header("振動の最大の強さ"), SerializeField, Range(0, 255)] int maxPower = 150;
    public int MaxPower => maxPower;

    [Header("--- 単発の設定 ---")]
    [Header("持続時間(ms)"), SerializeField] long oneShotDurationMs = 20;
    public long OneShotDurationMs => oneShotDurationMs;

    [Header("--- ループの設定 ---")]
    [Header("振動ごとの待機時間(ms)"), SerializeField] long silentMs = 50;
    public long SlientMs => silentMs;

    [Header("振動ごとの持続時間(ms)"), SerializeField] long activeMs = 100;
    public long ActiveMs => activeMs;

    [Header("ループ時に戻る振動の再生箇所（-1でループしない）"), SerializeField] int repeatIndex = 0;
    public int RepeatIndex => repeatIndex;
}
