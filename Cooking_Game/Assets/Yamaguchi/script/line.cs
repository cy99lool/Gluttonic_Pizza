using UnityEngine;

public class LineRendererExample : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public float minZ = -45f; // 回転できる最小角度（左）
    public float maxZ = 45f;  // 回転できる最大角度（右）
    
    void Start()
    {
        // LineRendererコンポーネントを取得
        lineRenderer = GetComponent<LineRenderer>();

        // 頂点数を指定
        int vertexCount = 3;
        lineRenderer.positionCount = vertexCount;

        // 各頂点の位置を設定
        for (int i = 0; i < vertexCount; i++)
        {
            float x = i * 1.0f; // X座標
            float y = Mathf.Sin(i); // Y座標 (例: サイン波)
            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}
