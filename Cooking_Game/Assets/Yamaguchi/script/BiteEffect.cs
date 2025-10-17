using UnityEngine;
using System.Collections;

public class BiteEffect : MonoBehaviour
{
    [SerializeField] private Transform upperRoot;     // 上の歯
    [SerializeField] private Transform lowerRoot;     // 下の歯
    [SerializeField] private Transform effectPoint;   // 噛み合わせ中心
    [SerializeField] private ParticleSystem hitEffect; // パーティクル
    [SerializeField] private float moveDistance = 0.2f; // 上下移動距離
    [SerializeField] private float biteSpeed = 8f;      // 噛み速度
    [SerializeField] private float holdTime = 0.1f;     // 閉じたまま時間
    [SerializeField] private float moveOffset = 0.3f;   // 出現位置補正（距離）
    [SerializeField] private float lifeTime = 2f;       // 自動削除時間

    private Vector3 upperStartPos;
    private Vector3 lowerStartPos;
    private bool isBiting = false;
    private Transform target;

    void Start()
    {
        upperStartPos = upperRoot.localPosition;
        lowerStartPos = lowerRoot.localPosition;
    }

    /// <summary>
    /// 対象を設定して噛みつく
    /// </summary>
    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        Bite();
    }

    public void Bite()
    {
        if (!isBiting)
            StartCoroutine(BiteAnim());
    }

    private IEnumerator BiteAnim()
    {
        isBiting = true;

        // --- 方向計算（2D用、Z軸固定） ---
        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            dir.z = 0;

            // 口の位置を対象方向に少しだけ近づける
            transform.position += dir * moveOffset;
        }

        // --- 歯を閉じる ---
        Vector3 upperDown = upperStartPos + Vector3.down * moveDistance;
        Vector3 lowerUp = lowerStartPos + Vector3.up * moveDistance;

        while (Vector3.Distance(upperRoot.localPosition, upperDown) > 0.001f)
        {
            upperRoot.localPosition = Vector3.MoveTowards(upperRoot.localPosition, upperDown, biteSpeed * Time.deltaTime);
            lowerRoot.localPosition = Vector3.MoveTowards(lowerRoot.localPosition, lowerUp, biteSpeed * Time.deltaTime);
            yield return null;
        }

        // --- パーティクル再生 ---
        if (hitEffect != null && effectPoint != null)
        {
            hitEffect.transform.position = effectPoint.position;
            hitEffect.Play();
        }

        yield return new WaitForSeconds(holdTime);

        // --- 元に戻す ---
        while (Vector3.Distance(upperRoot.localPosition, upperStartPos) > 0.001f)
        {
            upperRoot.localPosition = Vector3.MoveTowards(upperRoot.localPosition, upperStartPos, biteSpeed * Time.deltaTime);
            lowerRoot.localPosition = Vector3.MoveTowards(lowerRoot.localPosition, lowerStartPos, biteSpeed * Time.deltaTime);
            yield return null;
        }

        isBiting = false;
    }
}
