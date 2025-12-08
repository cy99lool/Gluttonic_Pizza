using UnityEngine;
using UnityEngine.UI;

public class CircleTimerWithNeedle : MonoBehaviour
{
    [SerializeField] private float timeLimit = 60f; // 制限時間（秒）
    private float timer;

    [Header("UI Elements")]
    [SerializeField] private Image timerCircle;       // 円形ゲージ
    [SerializeField] private RectTransform needle;    // 針

    void Start()
    {
        timer = timeLimit;
        timerCircle.fillAmount = 1f; // 満タンからスタート
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            float progress = timer / timeLimit; // 1.0 → 0.0 に減る

            // 🟡 円形ゲージを更新
            timerCircle.fillAmount = progress;

            // 🔴 針を回す（360度回転）
            float angle = 360f * progress; 
            needle.localEulerAngles = new Vector3(0, 0, angle); 
            // マイナスで時計回り、プラスで反時計回り
        }
        else
        {
            timer = 0;
            timerCircle.fillAmount = 0f;
            needle.localEulerAngles = Vector3.zero; // 終了時は0度に固定
        }
    }
}
