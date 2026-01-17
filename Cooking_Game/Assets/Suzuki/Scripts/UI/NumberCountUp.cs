using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberCountUp
{
    // アニメーション中のテキスト数
    int currentActiveAnimationCount = GameConstants.Zero;

    public bool IsAllFinished => currentActiveAnimationCount == GameConstants.Zero;



    /// <summary>
    /// テキストの数字のカウントアップアニメーション（アニメーション時間で固定）
    /// </summary>
    /// <param name="targetText">数字のカウントアップを行うテキスト</param>
    /// <param name="targetNum">目的の数字</param>
    /// <param name="duration">アニメーションの長さ（秒）</param>
    /// <param name="onComplete">完了時に行う処理</param>
    /// <returns></returns>
    public IEnumerator CountUpNumByTime(TMPro.TextMeshProUGUI targetText, int targetNum, float duration, System.Action onComplete = null)
    {
        // アニメーション開始時
        OnStartAnimation();

        try
        {
            // 長さが0秒なら即終了（ゼロ除算を防ぐ）
            if (duration == GameConstants.Zero)
            {
                targetText.SetText("{0}", targetNum);
                yield break;
            }

            float timer = GameConstants.FirstTimerValue;

            while (timer < duration)
            {
                // 経過時間の割合から数字を求める
                int num = (int)Mathf.Lerp(GameConstants.Zero, targetNum, timer / duration);

                // 数字をテキストに適用
                targetText.SetText("{0}",num);

                timer += Time.deltaTime;
                yield return null;
            }

            // 確実に目的の値になるようにしておく
            targetText.SetText("{0}", targetNum);
        }
        finally
        {
            // アニメーション終了時
            OnEndAnimation(onComplete);
        }
    }

    /// <summary>
    /// テキストの数字のカウントアップアニメーション（カウントする速度が固定）
    /// </summary>
    /// <param name="targetText">数字のカウントアップを行うテキスト</param>
    /// <param name="targetNum">目的の数字</param>
    /// <param name="countSpeed">カウントの速さ（数値/秒）</param>
    /// <param name="onComplete">完了時に行う処理</param>
    /// <returns></returns>
    public IEnumerator CountUpNumBySpeed(TMPro.TextMeshProUGUI targetText, int targetNum, float countSpeed, System.Action onComplete = null)
    {
        // アニメーション開始時
        OnStartAnimation();

        try
        {
            // カウント速度が0なら即終了
            if (countSpeed == GameConstants.Zero)
            {
                targetText.SetText("{0}", targetNum);
                yield break;
            }

            int num = GameConstants.Zero;
            float timer = GameConstants.FirstTimerValue;

            while (num < targetNum)
            {
                // 数字のカウント
                num = (int)(timer * countSpeed);

                // 数字をテキストに適用
                targetText.SetText("{0}", num);

                timer += Time.deltaTime;
                yield return null;
            }

            // 確実に目的の値になるようにしておく
            targetText.SetText("{0}", targetNum);
        }
        finally
        {
            // アニメーション終了時
            OnEndAnimation(onComplete);
        }
    }

    /// <summary>
    /// アニメーション開始時、再生中アニメーション数を増やす
    /// </summary>
    void OnStartAnimation() => currentActiveAnimationCount++;

    /// <summary>
    /// アニメーション終了時、再生中アニメーション数を減らす(0以下にならないように)
    /// </summary>
    /// <param name="onComplete">完了時に行う処理</param>
    void OnEndAnimation(System.Action onComplete)
    {
        // カウントを減らす
        currentActiveAnimationCount = Mathf.Max(GameConstants.Zero, --currentActiveAnimationCount);

        // 渡された完了時の処理を行う
        onComplete?.Invoke();
    }
}
