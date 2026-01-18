using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// キャラクターの音ごとのクラス
/// </summary>
[System.Serializable]
public class CharacterSoundEntry
{
    [Header("再生する状況の種類"), SerializeField] PlayerSoundType soundType;
    public PlayerSoundType SoundType => soundType;

    [Header("音の名前"), SerializeField] string soundName;
    public string SoundName => soundName;

    [Header("再生するクリップ"), SerializeField] AudioClip soundClip;
    public AudioClip SoundClip => soundClip;

    [Header("音量"), Range(0f,1f),SerializeField] float volume;
    public float Volume => volume;

    [Header("ピッチの幅（+-）"), SerializeField] float pitchVarietion;
    public float PitchVarietion => pitchVarietion;
}

public enum PlayerSoundType
{
    Charge,
    Eat,
    Merge,
    Bomb,
    BeforeBomb,
    ScoreCountUpStart,
    ScoreCountUpEnd,
}

[CreateAssetMenu(fileName = "CharacterSoundData", menuName = "Sound/Character Sound Data")]
public class CharacterSoundData : ScriptableObject
{
    [Header("再生する音一覧"), SerializeField] List<CharacterSoundEntry> soundEntries;
    public List<CharacterSoundEntry> SoundEntries => soundEntries;

    public void PlaySE(PlayerSoundType soundType)
    {
        foreach(CharacterSoundEntry characterSoundEntry in soundEntries)
        {
            // 登録されたタイプと合致するものを再生（マネージャーのキューに追加？）
            //if(characterSoundEntry.SoundType == soundType) 
        }
    }
}
