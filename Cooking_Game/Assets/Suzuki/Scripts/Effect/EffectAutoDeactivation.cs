using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectAutoDeactivation : MonoBehaviour
{
    [Header("再生対象（自身の子）のエフェクトたち"), SerializeField] List<ParticleSystem> particleSystems;

    void Awake()
    {
        if(particleSystems == null)
        {
            // 初期化
            particleSystems = new List<ParticleSystem>();

            // パーティクルを全て取得
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();

            // 追加
            particleSystems.AddRange(particles);
        }
    }

    void OnEnable()
    {
        if(particleSystems != null) StartCoroutine(DeactivateAfterDuration());
    }

    /// <summary>
    /// 一定時間後に自身を無効化する
    /// </summary>
    IEnumerator DeactivateAfterDuration()
    {
        // エフェクトの最長効果時間を取得
        float maxParticleTime = GameConstants.Zero;
        foreach(ParticleSystem particle in particleSystems) if(particle.main.duration > maxParticleTime) maxParticleTime = particle.main.duration;

        // エフェクトの最長効果時間分待機
        yield return new WaitForSeconds(maxParticleTime);

        // 自身を無効化
        this.gameObject.SetActive(false);
    }
}
