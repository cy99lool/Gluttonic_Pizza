using UnityEngine;
using UnityEngine.UI;

public class CircleTimer : MonoBehaviour
{
    [SerializeField] private float timeLimit = 60f; // 制限時間（秒）
    private float timer;

    [SerializeField] private Image timerCircle; // 円形Imageをアタッチ

    void Start()
    {
        timer = timeLimit;
        timerCircle.fillAmount = 1f; // スタート時は満タン
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;

            // fillAmountに割合を代入（1→0へ）
            float progress = timer / timeLimit;
            timerCircle.fillAmount = progress;
        }
        else
        {
            timer = 0;
            timerCircle.fillAmount = 0f; // ゲージが空になる
        }
    }
}
