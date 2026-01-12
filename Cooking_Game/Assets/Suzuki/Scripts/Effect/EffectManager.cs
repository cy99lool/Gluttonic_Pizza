using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EffectManager : MonoBehaviour
{
    [Header("--- エフェクトごとの設定 ---")]
    [Header("捕食時のエフェクト"), SerializeField] EffectSetting eatenEffects;
    [Header("結合時のエフェクト"), SerializeField] EffectSetting mergedEffects;
    [Header("チャージ完了時のエフェクト"), SerializeField] EffectSetting chargedEffects;

    void Awake()
    {
        // 起動時にあらかじめ生成しておく
        Prewarm(eatenEffects);
        Prewarm(mergedEffects);
        Prewarm(chargedEffects);
    }

    /// <summary>
    /// エフェクトプールの準備を行う
    /// </summary>
    /// <param name="effectSetting">エフェクトプールの設定</param>
    void Prewarm(EffectSetting effectSetting)
    {
        // プレハブが設定されていなければ生成しない
        if (effectSetting.Prefab == null) return;

        // エフェクトプールの準備
        for (int i = 0; i < effectSetting.PoolSize; i++)
        {
            // エフェクト用オブジェクトを生成（親が設定されていたら親子付けも行う）
            EffectAutoDeactivation effect = effectSetting.Parent != null ? Instantiate(effectSetting.Prefab, effectSetting.Parent) : Instantiate(effectSetting.Prefab);

            // 生成したら非有効化（勝手に再生されないように）
            effect.gameObject.SetActive(false);

            // プールのリストに追加
            effectSetting.Pool.Add(effect);
        }
    }

    /// <summary>
    /// 捕食エフェクトを再生
    /// </summary>
    /// <param name="position">再生位置</param>
    public void PlayEatenEffect(Vector3 position) => PlayEffect(eatenEffects, position);

    /// <summary>
    /// 結合エフェクトを再生
    /// </summary>
    /// <param name="position">再生位置</param>
    public void PlayMergedEffect(Vector3 position) => PlayEffect(mergedEffects, position);

    /// <summary>
    /// チャージ完了エフェクトを再生
    /// </summary>
    /// <param name="position">再生位置</param>
    public void PlayChargedEffect(Vector3 position) => PlayEffect(chargedEffects, position);

    void PlayEffect(EffectSetting setting, Vector3 position)
    {
        // プール内の再生可能なオブジェクトを取得
        EffectAutoDeactivation effect = GetPooledObject(setting.Pool);

        // 再生可能なオブジェクトがなければ再生しない
        if (effect == null) return;

        // 位置を設定
        effect.transform.position = position;

        // 再生
        effect.gameObject.SetActive(true);
    }

    /// <summary>
    /// プール内の再生可能なエフェクトオブジェクトを取得
    /// </summary>
    /// <param name="effectPool">エフェクトのプール</param>
    /// <returns>再生可能なエフェクトオブジェクト(無ければnull)</returns>
    EffectAutoDeactivation GetPooledObject(List<EffectAutoDeactivation> effectPool)
    {
        foreach (EffectAutoDeactivation effect in effectPool)
        {
            // 再生可能なもの(無効化されている)があれば返す
            if (!effect.gameObject.activeSelf) return effect;
        }
        // なければnull
        return null;
    }
}

/// <summary>
/// エフェクトの設定
/// </summary>
[System.Serializable]
public class EffectSetting
{
    [Header("親オブジェクト（グループ分けに使用）"), SerializeField] Transform parent;
    public Transform Parent => parent;

    [Header("プレハブ"), SerializeField] EffectAutoDeactivation prefab;
    public EffectAutoDeactivation Prefab => prefab;

    [Header("プールに生成する数"), SerializeField] uint poolSize;
    public uint PoolSize => poolSize;

    List<EffectAutoDeactivation> pool = new List<EffectAutoDeactivation>();
    public List<EffectAutoDeactivation> Pool => pool;
}
