using UnityEngine;

public class SpawnBlood : MonoBehaviour
{
    public GameObject bloodPrefab;
    public Canvas canvas;
    public Camera targetCamera;

    [Header("Inspectorで調整するスクリーン座標")]
    public Vector2 screenPosition = new Vector2(960, 540);
    public float depth = 1f;

    [Header("UIの表示位置オフセット")]
    public Vector2 offset = Vector2.zero;

    [Header("スペースキー押してから出現するまでの遅延秒数")]
    public float delaySeconds = 2f;   // ← ここで設定できる

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 worldPos = targetCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, depth)
            );

            // ← Space を押したら一定時間後に実行
            StartCoroutine(SpawnWithDelay(worldPos));
        }
    }

    // 遅延処理用コルーチン
    private System.Collections.IEnumerator SpawnWithDelay(Vector3 pos)
    {
        yield return new WaitForSeconds(delaySeconds);
        Spawn(pos);
    }

    public void Spawn(Vector3 worldPos)
    {
        if (bloodPrefab == null || canvas == null || targetCamera == null)
            return;

        Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCamera,
            out localPos
        );

        GameObject obj = Instantiate(bloodPrefab, canvas.transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchoredPosition = localPos + offset;

        if (obj.GetComponent<BloodDrop>() == null)
        {
            obj.AddComponent<BloodDrop>();
        }
    }
}
