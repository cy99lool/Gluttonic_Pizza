using UnityEngine;

public class BloodDrop1 : MonoBehaviour
{
    public float stopTime = 0.15f;   // 止まる時間
    public float fallSpeed = 200f;   // 落下速度(px/sec)
    public float lifeTime = 2f;      // 消えるまでの時間

    private RectTransform rt;
    private float timer = 0f;
    private bool isFalling = false;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        Destroy(gameObject, lifeTime); // 時間で自動削除
    }

    void Update()
    {
        if (rt == null) return;

        timer += Time.deltaTime;

        // 停止時間が過ぎたら落下開始
        if (!isFalling && timer >= stopTime)
        {
            isFalling = true;
        }

        // 落下フェーズ
        if (isFalling)
        {
            rt.anchoredPosition += Vector2.up * fallSpeed * Time.deltaTime;
        }
    }
}
