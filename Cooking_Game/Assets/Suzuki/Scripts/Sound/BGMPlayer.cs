using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BGMPlayer
{
    [Header("BGMのデータ"), SerializeField] BGMData data;
    [Header("BGMのAudioSource"), SerializeField] AudioSource audioSource;
    [Header("--- フェード設定 ---")]
    [Header("フェードイン時間"), SerializeField] float fadeInDuration;
    [Header("フェードアウト時間"), SerializeField] float fadeOutDuration;

    /// <summary>
    /// BGMを再生（フェードイン）
    /// </summary>
    /// /// <param name="runner">コルーチンの実行者</param>
    /// <param name="type">再生するBGMの種類</param>
    public void Play(SoundManager runner, BGMType type)
    {
        BGMEntry entry = null;
        // 指定された種類のBGMがあるか検索
        //foreach ()
        //{

        //}

        // 指定された種類のBGMがなければreturn
        if (entry == null) return;
        // フェードインして再生
        runner.StartCoroutine(StartWithFadeIn(entry, fadeInDuration));
    }

    /// <summary>
    /// フェードインして再生
    /// </summary>
    /// <param name="duration">フェードの長さ</param>
    /// <returns></returns>
    public IEnumerator StartWithFadeIn(BGMEntry entry, float duration)
    {
        float timer = GameConstants.FirstTimerValue;

        // 最大ボリュームを記録
        float maxVolume = entry.Volume;

        // 最低ボリューム
        float minVolume = GameConstants.Zero;

        // 再生するクリップ
        audioSource.clip = entry.SoundClip;

        // 再生開始
        audioSource.Play();

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // ボリュームを上げる（効果時間の割合で決める）
            audioSource.volume = Mathf.Lerp(minVolume, maxVolume, timer / duration);
            yield return null;
        }

        // 終了後に確実に最大ボリュームにしておく
        audioSource.volume = maxVolume;
    }

    /// <summary>
    /// BGMを停止（フェードアウト）
    /// </summary>
    /// <param name="runner">コルーチンの実行者</param>
    public void Stop(SoundManager runner)
    {
        // フェードアウトして停止
        runner.StartCoroutine(StopWithFadeOut(fadeOutDuration));
    }

    /// <summary>
    /// フェードアウトして停止
    /// </summary>
    /// <param name="duration">フェードの長さ</param>
    /// <returns></returns>
    public IEnumerator StopWithFadeOut(float duration)
    {
        float timer = GameConstants.FirstTimerValue;

        // 現在のボリュームを記録
        float maxVolume = audioSource.volume;

        // 最低ボリューム
        float minVolume = GameConstants.Zero;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // ボリュームを下げる（効果時間の割合で決める）
            audioSource.volume = Mathf.Lerp(maxVolume, minVolume, timer / duration);
            yield return null;
        }

        // 終了後に確実に最低ボリュームにしておく
        audioSource.volume = minVolume;

        // 停止
        audioSource.Stop();
    }
}
