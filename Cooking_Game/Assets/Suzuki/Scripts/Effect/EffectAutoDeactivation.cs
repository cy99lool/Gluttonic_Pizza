using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectAutoDeactivation : MonoBehaviour
{
    [SerializeField] new ParticleSystem particleSystem;

    void Awake()
    {
        if(particleSystem == null) particleSystem = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if(particleSystem != null) StartCoroutine(DeactivateAfterDuration());
    }

    /// <summary>
    /// 一定時間後に自身を無効化する
    /// </summary>
    IEnumerator DeactivateAfterDuration()
    {
        // エフェクトの効果時間分待機
        yield return new WaitForSeconds(particleSystem.main.duration);

        // 自身を無効化
        this.gameObject.SetActive(false);
    }
}
