using UnityEngine;
using System.Collections;

public class biteeffect1 : MonoBehaviour
{
    [SerializeField] private Transform upperRoot;   // 上の歯
    [SerializeField] private Transform lowerRoot;   // 下の歯
    [SerializeField] private Transform effectPoint; // かみつき中心位置
    [SerializeField] private ParticleSystem hitEffect;

    [Header("動き設定")]
    [SerializeField] private float moveDistance = 0.3f; // 開く距離
    [SerializeField] private float biteSpeed = 8f;      // 開閉スピード
    [SerializeField] private float holdTime = 0.1f;     // 開いたままの時間

    private Vector3 upperStartPos;
    private Vector3 lowerStartPos;
    private bool isBiting = false;

    void Start()
    {
        // 初期位置を記録
        upperStartPos = upperRoot.localPosition;
        lowerStartPos = lowerRoot.localPosition;
    }

    void Update()
    {
        // スペースキーで噛みつく
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Bite();
        }
    }

    public void Bite()
    {
        if (!isBiting)
            StartCoroutine(BiteAnim());
    }

    private IEnumerator BiteAnim()
    {
        isBiting = true;

        Vector3 upperOpenPos = upperStartPos + Vector3.up * moveDistance;
        Vector3 lowerOpenPos = lowerStartPos + Vector3.down * moveDistance;

        // 歯を開く
        while (Vector3.Distance(upperRoot.localPosition, upperOpenPos) > 0.001f)
        {
            upperRoot.localPosition = Vector3.MoveTowards(upperRoot.localPosition, upperOpenPos, biteSpeed * Time.deltaTime);
            lowerRoot.localPosition = Vector3.MoveTowards(lowerRoot.localPosition, lowerOpenPos, biteSpeed * Time.deltaTime);
            yield return null;
        }

        // 開いたまま少し待つ
        yield return new WaitForSeconds(holdTime);

        // パーティクルを再生（噛みつく瞬間）
        if (hitEffect != null && effectPoint != null)
        {
            hitEffect.transform.position = effectPoint.position;
            hitEffect.Play();
        }

        // 歯を閉じる
        while (Vector3.Distance(upperRoot.localPosition, upperStartPos) > 0.001f)
        {
            upperRoot.localPosition = Vector3.MoveTowards(upperRoot.localPosition, upperStartPos, biteSpeed * Time.deltaTime);
            lowerRoot.localPosition = Vector3.MoveTowards(lowerRoot.localPosition, lowerStartPos, biteSpeed * Time.deltaTime);
            yield return null;
        }

        isBiting = false;
    }
}
