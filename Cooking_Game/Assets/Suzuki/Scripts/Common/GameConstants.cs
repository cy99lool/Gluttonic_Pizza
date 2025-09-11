using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 複数のスクリプトで共通で使う定数やメソッド等をまとめておく
/// </summary>
public static class GameConstants
{
    public const float FirstTimerValue = 0f;// タイマーの初期化に使う
    public const float HalfMultiplyer = 0.5f;// 半分にする際に使う（TransformのSizeから半径を取るときなど）
    public const float OneSecond = 1f;// 一秒あたりのレートを計算するとき等に使う

    /// <summary>
    /// Vector3の割り算
    /// </summary>
    /// <param name="numerator">分子</param>
    /// <param name="denominator">分母</param>
    /// <returns></returns>
    public static Vector3 DivineVector3(Vector3 numerator, Vector3 denominator)
    {
        return new Vector3(numerator.x / denominator.x, numerator.y / denominator.y, numerator.z / denominator.z);
    }

    public static Vector3 MultiplyVector3(Vector3 first, Vector3 second)
    {
        return new Vector3(first.x * second.x, first.y * second.y, first.z * second.z);
    }
}
