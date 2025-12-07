using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("BGMの再生設定"), SerializeField] BGMPlayer bgmPlayer;
    [Header("SEの再生設定"), SerializeField] SEPlayer sePlayer;

    /// <summary>
    /// BGMの再生（フェードイン）
    /// </summary>
    /// <param name="type">再生するBGM</param>
    public void PlayBGM(BGMType type)
    {
        // 再生中のBGMを停止
        StopBGM();

        // 再生
        bgmPlayer.Play(this, type);
    }

    /// <summary>
    /// BGMの停止（フェードアウト）
    /// </summary>
    public void StopBGM() => bgmPlayer.Stop(this);

    /// <summary>
    /// SEの再生
    /// </summary>
    /// <param name="type">再生するSE</param>
    /// <param name="playTransform">再生者（追跡対象）</param>
    public void PlaySE(PlayerSoundType type, Transform playTransform) => sePlayer.Play(this, type, playTransform);
}