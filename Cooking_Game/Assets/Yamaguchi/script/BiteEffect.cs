using UnityEngine;
using System.Collections;

public class BiteEffect : MonoBehaviour
{
    [SerializeField] private Transform upperRoot;
    [SerializeField] private Transform lowerRoot;
    [SerializeField] private Transform effectPoint;   // 口の中央
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private float biteSpeed = 8f;
    [SerializeField] private float biteAngle = 30f;
    [SerializeField] private float holdTime = 0.1f;

    private Quaternion upperStartRot;
    private Quaternion lowerStartRot;
    private bool isBiting = false;

    void Start()
    {
        upperStartRot = upperRoot.localRotation;
        lowerStartRot = lowerRoot.localRotation;
    }

    void Update()
    {
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

        Quaternion upperOpen = Quaternion.Euler(0, 0, biteAngle);
        Quaternion lowerOpen = Quaternion.Euler(0, 0, -biteAngle);

        // 歯を開く
        while (Quaternion.Angle(upperRoot.localRotation, upperOpen) > 0.1f)
        {
            upperRoot.localRotation = Quaternion.RotateTowards(upperRoot.localRotation, upperOpen, biteSpeed * Time.deltaTime * 60);
            lowerRoot.localRotation = Quaternion.RotateTowards(lowerRoot.localRotation, lowerOpen, biteSpeed * Time.deltaTime * 60);
            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        // パーティクル再生
        if (hitEffect != null && effectPoint != null)
        {
            hitEffect.transform.position = effectPoint.position; // 口の中央
            hitEffect.Play();
        }

        // 歯を閉じる
        while (Quaternion.Angle(upperRoot.localRotation, upperStartRot) > 0.1f)
        {
            upperRoot.localRotation = Quaternion.RotateTowards(upperRoot.localRotation, upperStartRot, biteSpeed * Time.deltaTime * 60);
            lowerRoot.localRotation = Quaternion.RotateTowards(lowerRoot.localRotation, lowerStartRot, biteSpeed * Time.deltaTime * 60);
            yield return null;
        }

        isBiting = false;
    }

    // 🔹 ここから追加：ターゲット方向に向かせるメソッド
    public void SetDirection(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.z = 0;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        transform.position += dir.normalized * 0.2f; // 噛む位置調整
    }

    // Renderer を強制ON
    private void SetRenderersEnabled(Transform t, bool enabled)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = enabled;
    }
}
