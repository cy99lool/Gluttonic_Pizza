using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimedRotateShake : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float timeLimit = 60f;
    private float elapsed = 0f;
    private bool triggered = false;
    private bool warningSoundPlayed = false;

    [Header("Warning Settings")]
    [SerializeField] private float warningTime = 10f; // 残り◯秒で音＋シェイク

    [Header("UI")]
    [SerializeField] private Image timerCircle;

    [Header("Shake Target")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeAngle = 15f;
    [SerializeField] private float shakeSpeed = 30f;

    private Quaternion originalRotation;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip triggerClip;   // 時間切れ時の音
    [SerializeField] private AudioClip warningClip;   // 残り◯秒の警告音

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

        // ⚠️ 残り warningTime 秒で音＋シェイク
        if (!warningSoundPlayed && remaining <= warningTime)
        {
            warningSoundPlayed = true;

            if (audioSource != null && warningClip != null)
                audioSource.PlayOneShot(warningClip);

            if (targetRect != null)
                StartCoroutine(RotateShakeCoroutine());
        }

        // ⏰ 制限時間を超えたら実行
        if (elapsed >= timeLimit)
        {
            TriggerEffect();
        }
    }

    private void TriggerEffect()
    {
        triggered = true;

        if (audioSource != null && triggerClip != null)
            audioSource.PlayOneShot(triggerClip);

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
        targetRect.localRotation = originalRotation;
    }
}

