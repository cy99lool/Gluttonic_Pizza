using UnityEngine;

public class smoke : MonoBehaviour
{
    public ParticleSystem effect;   // 再生したいエフェクト

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayEffect();
        }
    }

    void PlayEffect()
    {
        if (effect == null) return;

        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play();
    }
}
