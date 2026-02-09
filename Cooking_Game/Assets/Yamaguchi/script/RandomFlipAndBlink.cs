using UnityEngine;
using System.Collections;

public class RandomFlipAndBlink : MonoBehaviour
{
    public float blinkInterval = 0.5f;

    SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // 向きをランダムに決める
        bool upsideDown = Random.value > 0.5f;

        if (upsideDown)
        {
            // 上下逆さま（2D）
            transform.rotation = Quaternion.Euler(0f, 0f, 180f);

            // もしくはこっちでもOK
            // sr.flipY = true;
        }

        // 点滅開始
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (true)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
