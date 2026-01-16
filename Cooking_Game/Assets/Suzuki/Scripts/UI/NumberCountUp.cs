using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberCountUp
{
    /// <summary>
    /// テキストの数字のカウントアップアニメーション（アニメーション時間で固定）
    /// </summary>
    /// <param name="targetText">数字のカウントアップを行うテキスト</param>
    /// <param name="targetNum">目的の数字</param>
    /// <param name="duration">アニメーションの長さ（秒）</param>
    /// <returns></returns>
    public static IEnumerator CountUpNumByTime(TMPro.TextMeshProUGUI targetText, int targetNum, float duration)
    {
        float timer = GameConstants.FirstTimerValue;

        while(timer < duration)
        {
            // 経過時間の割合から数字を求める
            int num = (int)Mathf.Lerp(GameConstants.Zero, targetNum, timer / duration);

            // 数字をテキストに適用
            targetText.text = num.ToString();

            timer += Time.deltaTime;
            yield return null;
        }

        // 確実に目的の値になるようにしておく
        targetText.text = targetNum.ToString();
    }

    /// <summary>
    /// テキストの数字のカウントアップアニメーション（カウントする速度が固定）
    /// </summary>
    /// <param name="targetText">数字のカウントアップを行うテキスト</param>
    /// <param name="targetNum">目的の数字</param>
    /// <param name="countSpeed">カウントの速さ（数値/秒）</param>
    /// <returns></returns>
    public static IEnumerator CountUpNumBySpeed(TMPro.TextMeshProUGUI targetText, int targetNum, float countSpeed)
    {
        int num = GameConstants.Zero;
        float timer  = GameConstants.FirstTimerValue;

        while (num < targetNum)
        {
            // 数字のカウント
            num = (int)(timer * countSpeed);

            // 数字をテキストに適用
            targetText.text = num.ToString();

            timer += Time.deltaTime;
            yield return null;
        }

        // 確実に目的の値になるようにしておく
        targetText.text = targetNum.ToString();
    }
}
