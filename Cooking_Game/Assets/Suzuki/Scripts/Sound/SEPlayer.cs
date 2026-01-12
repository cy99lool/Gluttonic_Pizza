using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SEPlayer
{
    [Header("プレイヤーSEのデータ"), SerializeField] CharacterSoundData characterSoundData;
    [Header("プレイヤーSEのAudioSourceたち"), SerializeField] List<AudioSourceClass> audioSources;

    public void Play(SoundManager runner, PlayerSoundType soundType, Transform playTransform)
    {

        // 再生するAudioSourceを指定
        AudioSourceClass audioSource = null;
        {
            foreach (AudioSourceClass audioSourceClass in audioSources)
            {
                // 再生可能なAudioSourceを割り当てる
                if (audioSourceClass.Playable)
                {
                    audioSource = audioSourceClass;
                    break;
                }
            }
        }
        // 再生できるAudioSourceがなければ再生しない
        if (audioSource == null) return;

        // 指定された種類のBGMがあるか検索
        CharacterSoundEntry targetEntry = null;

        foreach (CharacterSoundEntry entry in characterSoundData.SoundEntries)
        {
            // 種類が合致したら適用
            if (entry.SoundType == soundType)
            {
                targetEntry = entry;
                break;
            }
        }
        // 指定された種類のBGMがなければ再生しない
        if (targetEntry == null) return;

        // 再生準備
        audioSource.AudioSource.clip = targetEntry.SoundClip;
        audioSource.AudioSource.volume = targetEntry.Volume;

        // ピッチをランダムで変更
        float pitch = Random.Range(-targetEntry.PitchVarietion, targetEntry.PitchVarietion);
        float targetPitch = GameConstants.One;
        targetPitch += pitch;
        audioSource.AudioSource.pitch = targetPitch;

        // 追尾の設定
        audioSource.SetChaseTransform(playTransform);

        // 再生
        audioSource.AudioSource.Play();
    }
}
