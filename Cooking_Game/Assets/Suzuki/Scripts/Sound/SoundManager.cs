using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("BGMの再生設定"), SerializeField] BGMPlayer bgmPlayer;

    /// <summary>
    /// BGMの再生（フェードイン）
    /// </summary>
    /// <param name="type">再生するBGM</param>
    public void PlayBGM(BGMType type) => bgmPlayer.Play(this, type);

    /// <summary>
    /// BGMの停止（フェードアウト）
    /// </summary>
    public void StopBGM() => bgmPlayer.Stop(this);
}