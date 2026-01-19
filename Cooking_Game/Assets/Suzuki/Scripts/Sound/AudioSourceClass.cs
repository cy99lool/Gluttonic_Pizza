using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioSourceClass : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    public AudioSource AudioSource => audioSource;

    // 追尾するTransform
    Transform chaseTransform = null;
    /// <summary>
    /// 追尾するTransformを設定
    /// </summary>
    /// <param name="transform"></param>
    public void SetChaseTransform(Transform transform) => chaseTransform = transform;

    bool isReserved = false;
    void Reserve() => isReserved = true;

    // 再生中かどうか
    public bool IsPlaying => audioSource != null && audioSource.isPlaying && !isReserved;

    /// <summary>
    /// 再生可能か調べる
    /// </summary>
    public bool Playable => audioSource != null && !audioSource.isPlaying;// AudioSourceがアタッチされていてかつ再生中でないものならtrue、それ以外はfalse

    void Update()
    {
        if (chaseTransform != null) Chase();
    }

    /// <summary>
    /// 追尾
    /// </summary>
    void Chase()
    {
        transform.position = chaseTransform.position;
    }

    public void OnPlay()
    {
        // 予約
        Reserve();
        StartCoroutine(ResetReserved());
    }

    IEnumerator ResetReserved()
    {
        // 1フレーム待つ
        yield return null;

        // 予約解除
        isReserved = false;
    }
}
