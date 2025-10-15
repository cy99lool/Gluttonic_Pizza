using UnityEngine;

public class BreakIngredients : MonoBehaviour
{
    [Header("壊れる設定")]
    [SerializeField] private ParticleSystem particlePrefab; // 壊れるときのパーティクル
    [SerializeField] private string targetTag = "Break";    // 当たる対象のタグ

    [Header("噛みつき演出設定")]
    [SerializeField] private BiteEffect biteEffectPrefab;   // 既存の BiteEffect プレハブ
    [SerializeField] private float biteEffectDuration = 1.5f; // BiteEffectを消すまでの時間
    [SerializeField] private Transform mouthOrigin;         // 歯や口の基準位置（プレイヤーなど）

    private bool isBroken = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isBroken) return;
        if (!other.CompareTag(targetTag)) return;

        isBroken = true;

        Vector3 hitPos = transform.position;

        // ===== 噛みつき方向（2D用） =====
        Vector3 dir = Vector3.zero;
        if (mouthOrigin != null)
        {
            dir = (hitPos - mouthOrigin.position);
            dir.z = 0; // Z軸を無視して2D方向だけで回転を作る
            dir.Normalize();
        }

        // 回転方向を計算（Y軸を基準に2D的な回転）
        Quaternion lookRot = Quaternion.identity;
        if (dir != Vector3.zero)
        {
            // 2D的な向き（右を基準にするなら Vector3.right）
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            lookRot = Quaternion.Euler(0, 0, angle);
        }

        // ===== ① BiteEffectの生成 =====
        if (biteEffectPrefab != null)
        {
            // 歯を少しプレイヤー側から出す（演出しやすくする）
            Vector3 bitePos = hitPos - dir * -3f;

            BiteEffect biteInstance = Instantiate(biteEffectPrefab, bitePos, lookRot);
            biteInstance.Bite(); // 既存の BiteEffect の噛みつきアニメを呼び出し
            Destroy(biteInstance.gameObject, biteEffectDuration);
        }

        // ===== ② パーティクルを生成 =====
        if (particlePrefab != null)
        {
            ParticleSystem ps = Instantiate(particlePrefab, hitPos, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.startLifetime.constantMax);
        }

        // ===== ③ 自分を削除 =====
        Destroy(gameObject);
    }
}
