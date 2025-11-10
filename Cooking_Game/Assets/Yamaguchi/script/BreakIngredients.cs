using UnityEngine;

public class BreakIngredients : MonoBehaviour
{
    [Header("壊れる設定")]
    [SerializeField] private ParticleSystem particlePrefab; // 壊れるときのパーティクル
    [SerializeField] private string targetTag = "Break";    // 当たる対象のタグ

    [Header("噛みつき演出設定")]
    [SerializeField] private BiteEffect biteEffectPrefab;   // 既存の BiteEffect プレハブ
    [SerializeField] private float biteEffectDuration = 0.3f; // BiteEffectを消すまでの時間
    [SerializeField] private Transform mouthOrigin;         // 歯や口の基準位置（プレイヤーなど）

    private bool isBroken = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isBroken) return;
        if (!other.CompareTag(targetTag)) return;

        isBroken = true;

        Vector3 hitPos = transform.position;

        // ===== ① BiteEffectの生成 =====
        if (biteEffectPrefab != null && mouthOrigin != null)
        {
            // 「口の位置」→「ヒット位置」への方向
            Vector3 dir = (hitPos - mouthOrigin.position);
            dir.z = 0; // 2D視点なのでZは固定

            if (dir.sqrMagnitude > 0.0001f)
            {
                // 方向に基づいてZ角度を算出
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // スプライトの正面が右(+X)の場合 → そのまま
                // 正面が上(+Y)なら angle + 90f に変更してOK
                Quaternion lookRot = Quaternion.Euler(0f, 0f, angle + 180f);

                // 少し手前から噛む
                Vector3 bitePos = hitPos - dir.normalized * 0.5f;

                // BiteEffect生成
                BiteEffect biteInstance = Instantiate(biteEffectPrefab, bitePos, lookRot);
                biteInstance.Bite();

                Destroy(biteInstance.gameObject, biteEffectDuration);
            }
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
