using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [Header("被捕食時のエフェクト"), SerializeField] List<EffectAutoDeactivation> eatenEffects;

    /// <summary>
    /// 捕食のエフェクトを再生
    /// </summary>
    /// <param name="position">再生地点</param>
    public void PlayEatenEffect(Vector3 position)
    {
        // 再生できるエフェクトオブジェクトを取得
        EffectAutoDeactivation effect = GetPooledObject(eatenEffects);

        // 再生できるものがなければ再生しない
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
        foreach(EffectAutoDeactivation effect in effectPool)
        {
            // 再生可能なもの(無効化されている)があれば返す
            if(!effect.gameObject.activeSelf) return effect;
        }
        return null;
    }
}
