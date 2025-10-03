using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimedRotateShake : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float timeLimit = 10f;
    private float elapsed = 0f;
    private bool triggered = false;

    [Header("UI")]
    [SerializeField] private Image timerCircle;

    [Header("Shake Target")]
    [SerializeField] private RectTransform targetRect;   // 回転させたいUI
    [SerializeField] private float shakeDuration = 0.5f; // 続ける時間
    [SerializeField] private float shakeAngle = 15f;     // 左右の角度
    [SerializeField] private float shakeSpeed = 30f;     // 速さ（振動回数/秒）

    private Quaternion originalRotation;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource; // AudioSource をアタッチ
    [SerializeField] private AudioClip triggerClip;   // 鳴らしたい効果音

    void Start()
    {
        if (targetRect != null)
            originalRotation = targetRect.localRotation;
        if (timerCircle != null)
            timerCircle.fillAmount = 1f;
    }

    void Update()
    {
        if (triggered) return;

        elapsed += Time.deltaTime;
        float remaining = Mathf.Max(0f, timeLimit - elapsed);

        if (timerCircle != null)
            timerCircle.fillAmount = remaining / timeLimit;

        if (elapsed >= timeLimit)
        {
            TriggerEffect();
        }
    }

    private void TriggerEffect()
    {
        triggered = true;

        // 音を鳴らす
        if (audioSource != null && triggerClip != null)
        {
            audioSource.PlayOneShot(triggerClip);
        }

        // 回転シェイク開始
        if (targetRect != null)
            StartCoroutine(RotateShakeCoroutine());
    }

    private IEnumerator RotateShakeCoroutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            float angle = Mathf.Sin(Time.time * shakeSpeed) * shakeAngle;
            targetRect.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
            t += Time.deltaTime;
            yield return null;
        }
        targetRect.localRotation = originalRotation; // 元に戻す
    }

   
}
