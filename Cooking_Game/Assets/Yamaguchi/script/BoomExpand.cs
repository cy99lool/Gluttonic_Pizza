using UnityEngine;

public class BoomExpand : MonoBehaviour
{
    public float expandSpeed = 1.0f;   // 拡大速度
    public float fadeDuration = 1.5f;  // 消えるまでの時間

    private SpriteRenderer sr;
    private Color color;
    private float timer = 0f;

    private bool active = false;   // スペースを押すまで動かない

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        color = sr.color;

        // 最初は完全非表示
        sr.color = new Color(color.r, color.g, color.b, 0f);
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        // スペースを押したら表示して発動開始
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sr.color = new Color(color.r, color.g, color.b, 1f);  // 表示
            transform.localScale = Vector3.one;                   // 元サイズに戻す
            timer = 0f;                                           // タイマー初期化
            active = true;
        }

        if (!active) return;

        // 拡大
        transform.localScale += Vector3.one * expandSpeed * Time.deltaTime;

        // フェードアウト
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        sr.color = new Color(color.r, color.g, color.b, alpha);

        // 完全に透明で削除
        if (alpha <= 0f)
        {
            active = false;  
            // 非表示に戻す（何回でもスペースで発生できる）
            sr.color = new Color(color.r, color.g, color.b, 0f);
            transform.localScale = Vector3.zero;
        }
    }
}
