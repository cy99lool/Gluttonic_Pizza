using UnityEngine;

public class BoomExpand : MonoBehaviour
{
    public float expandSpeed = 1.0f;   // 拡大速度
    public float fadeDuration = 1.5f;  // 消えるまでの時間

    private SpriteRenderer sr;
    private Color color;
    private float timer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        color = sr.color;
    }

    void Update()
    {
        // 拡大
        transform.localScale += Vector3.one * expandSpeed * Time.deltaTime;

        // フェードアウト
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        sr.color = new Color(color.r, color.g, color.b, alpha);

        // 完全に透明になったら削除
        if (alpha <= 0f) Destroy(gameObject);
    }
}

