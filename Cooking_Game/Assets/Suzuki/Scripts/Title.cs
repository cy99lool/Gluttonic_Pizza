using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Playables;

public class Title : MonoBehaviour
{
    // [Header("遷移先のシーン"), SerializeField] string nextScene;
    [Header("遷移アニメーションのディレクター"), SerializeField] PlayableDirector changeSceneDirector;
    [Header("--- 操作を促す文 ---")]
    [Header("文本体のオブジェクト"), SerializeField] TextMeshProUGUI titleText;
    [Header("ループ：フェードアウト時間（秒）"), SerializeField] float loopFadeOutTime;
    [Header("ループ：フェードイン時間（秒）"), SerializeField] float loopFadeInTime;
    [Header("ループ：1ループ後の待機時間（秒）"), SerializeField] float loopInterval;
    [Header("クリック後：フェードアウト時間（秒）"), SerializeField] float clickFadeOutTime;

    bool changing = false;
    bool isTouched = false;
    // Start is called before the first frame update
    void Start()
    {
        changing = false;

        // タイトル下の文字の明滅アニメーション
        if (titleText != null) StartCoroutine(AnimateTitleText());
    }

    // Update is called once per frame
    void Update()
    {
        isTouched = Input.touchCount > 0 && Input.GetTouch(GameConstants.Zero).phase == TouchPhase.Began;

        if ((isTouched || Input.anyKeyDown) && !changing) StartCoroutine(ChangeScene());
    }

    IEnumerator ChangeScene()
    {
        changing = true;
        Debug.Log("Change to Next Scene...");

        // シーン切り替えのアニメーション（タイムライン）を再生
        changeSceneDirector.Play();

        // タイムラインの再生が終了するまで待つ
        while (changeSceneDirector.state == PlayState.Playing)
        {
            yield return null;
        }
        
        Debug.Log("Loading...");
        SceneManager.LoadScene(GameConstants.MainSceneName);
    }

    IEnumerator AnimateTitleText()
    {
        float maxAlpha = titleText.alpha;

        // タッチされるまでループする
        while (!isTouched)
        {
            // フェードアウト（透明になる）
            yield return InterruptibleFadeText(titleText, GameConstants.Zero, loopFadeOutTime);

            // フェードイン（不透明になる）
            yield return InterruptibleFadeText(titleText, maxAlpha, loopFadeInTime);

            // ループごとの待機時間
            yield return new WaitForSeconds(loopInterval);
        }

        // フェードアウト（タッチで中断できない）
        yield return UnInterruptibleFadeText(titleText, GameConstants.Zero, clickFadeOutTime);
    }

    /// <summary>
    /// テキストのフェード（タッチで中断される）
    /// </summary>
    /// <param name="text">フェードさせるテキスト</param>
    /// <param name="targetAlpha">目標の透明度（1.0で完全に不透明）</param>
    /// <param name="fadeTime">フェードにかける時間</param>
    IEnumerator InterruptibleFadeText(TextMeshProUGUI text, float targetAlpha, float fadeTime)
    {
        // 開始時の透明度を記録
        float startAlpha = text.alpha;

        float timer = GameConstants.FirstTimerValue;

        // タッチされたら途中で終わる
        while (!isTouched && timer < fadeTime)
        {
            // フェード
            text.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);

            timer += Time.deltaTime;
            yield return null;
        }

        // フェードの完了時（タッチで途中終了していないとき）、透明度を目標値に設定
        if (timer >= fadeTime) text.alpha = targetAlpha;
    }

    /// <summary>
    /// テキストのフェード（タッチで中断されない）
    /// </summary>
    /// <param name="text">フェードさせるテキスト</param>
    /// <param name="targetAlpha">目標の透明度（1.0で完全に不透明）</param>
    /// <param name="fadeTime">フェードにかける時間</param>
    IEnumerator UnInterruptibleFadeText(TextMeshProUGUI text, float targetAlpha, float fadeTime)
    {
        // 開始時の透明度を記録
        float startAlpha = text.alpha;

        float timer = GameConstants.FirstTimerValue;

        // フェード時間終了までフェードする
        while (timer < fadeTime)
        {
            // フェード
            text.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);

            timer += Time.deltaTime;
            yield return null;
        }

        // フェードの完了時、透明度を目標値に設定
        text.alpha = targetAlpha;
    }
}
