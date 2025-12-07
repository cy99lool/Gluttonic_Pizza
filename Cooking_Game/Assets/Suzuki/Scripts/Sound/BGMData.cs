using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGMごとのクラス
/// </summary>
[System.Serializable]
public class BGMEntry
{
    [Header("再生する状況の種類"), SerializeField] BGMType soundType;
    public BGMType SoundType => soundType;

    [Header("音の名前"), SerializeField] string soundName;
    public string SoundName => soundName;

    [Header("再生するクリップ"), SerializeField] AudioClip soundClip;
    public AudioClip SoundClip => soundClip;

    [Header("音量"), Range(0f, 1f), SerializeField] float volume;
    public float Volume => volume;
}

public enum BGMType
{
    Title,
    ConnectLobby,
    InGame,
    Result,
}

[CreateAssetMenu(fileName = "BGMData", menuName = "Sound/BGM Data")]
public class BGMData : ScriptableObject
{
    [Header("再生する音一覧"), SerializeField] List<BGMEntry> bgmEntries;
    public List<BGMEntry> BGMEntries => bgmEntries;
}
