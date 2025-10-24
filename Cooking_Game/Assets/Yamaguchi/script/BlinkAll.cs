using UnityEngine;
using System.Collections;

public class BlinkRenderer : MonoBehaviour
{
    private Renderer rend;
    public float interval = 0.3f; // 点滅間隔（秒）

    void Start()
    {
        rend = GetComponent<Renderer>();
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (true)
        {
            rend.enabled = !rend.enabled; // 表示/非表示を切り替え
            yield return new WaitForSeconds(interval);
        }
    }
}
