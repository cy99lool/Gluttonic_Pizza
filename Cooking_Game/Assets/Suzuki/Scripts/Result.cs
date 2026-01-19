using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SystemManager;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

public class Result : MonoBehaviour
{
    [SerializeField] SystemManager systemManager;
    [Header("--- リザルト表示設定 ---")]
    [Header("リザルトUI"), SerializeField] GameObject resultGroup;
    [Header("カーテンのタイムライン"), SerializeField] TimelineInfo curtainTimeline;
    [Header("赤チーム:勝利タイムライン"), SerializeField] TimelineInfo redWinTimeline;
    [Header("緑チーム:勝利タイムライン"), SerializeField] TimelineInfo greenWinTimeline;
    [Header("引き分け：両チーム勝利タイムライン"), SerializeField] TimelineInfo drawTimeline;

    [Header("--- チームのスコア表示設定 ---")]
    [Header("全体：数字のカウントアップ速度（数値/秒）"), SerializeField] float numCountSpeed;

    [Header("- 事前点表示 -")]
    [Header("カウントアニメーション終了SE"), SerializeField] PlayerSoundType prePointCountedSE;
    [Header("赤チーム:テキスト"), SerializeField] TextMeshProUGUI redPrePointText;
    [Header("緑チーム:テキスト"), SerializeField] TextMeshProUGUI greenPrePointText;

    [Header("- 具材点表示 -")]
    [Header("カウントアニメーション終了SE"), SerializeField] PlayerSoundType foodPointCountedSE;
    [Header("赤チーム:テキスト"), SerializeField] TextMeshProUGUI redFoodPointText;
    [Header("緑チーム:テキスト"), SerializeField] TextMeshProUGUI greenFoodPointText;

    [Header("- 総得点表示 -")]
    [Header("カウントアニメーション終了SE"), SerializeField] PlayerSoundType sumPointCountedSE;
    [Header("赤チーム:テキスト"), SerializeField] TextMeshProUGUI redSumPointText;
    [Header("緑チーム:テキスト"), SerializeField] TextMeshProUGUI greenSumPointText;
    
    int redPreScore;
    int greenPreScore;

    int redFoodScore;
    int greenFoodScore;
    

    NumberCountUp numberCounter = new NumberCountUp();

    // リザルト画面表示、timelineからのsignalでの呼び出しを考慮して細かく分けるかも
    public IEnumerator ShowResult(float debugReloadTime = 5f, float debugSccoreWaitTime = 2f)
    {
        if (resultGroup != null) resultGroup.SetActive(true);// リザルトUIを有効化

        // チームのスコア計算
        CalcTeamScore();

        int redSumScore = redPreScore + redFoodScore;
        int greenSumScore = greenFoodScore + greenPreScore;

        // 得点表示
        // 事前点（現状は爆発のみ）
        yield return ShowBothTeamScore(redPrePointText, greenPrePointText, redPreScore, greenPreScore, prePointCountedSE);

        // 具材点
        yield return ShowBothTeamScore(redFoodPointText, greenFoodPointText, redFoodScore, greenFoodScore, foodPointCountedSE);

        // 合計
        yield return ShowBothTeamScore(redSumPointText, greenSumPointText, redSumScore, greenSumScore, sumPointCountedSE);

        // 待つ
        yield return new WaitForSeconds(debugSccoreWaitTime);

        // 非同期でカーテンのTimeline再生
        yield return GameConstants.PlayTimeline(curtainTimeline.DirectorParent, curtainTimeline.Director);

        // 勝者によって切り替える
        if(redSumScore > greenSumScore)
        {
            yield return GameConstants.PlayAndWaitTimeline(redWinTimeline.DirectorParent, redWinTimeline.Director);
        }
        else if (redSumScore < greenSumScore)
        {
            yield return GameConstants.PlayAndWaitTimeline(greenWinTimeline.DirectorParent, greenWinTimeline.Director);
        }
        // 引き分け
        else
        {
           yield return GameConstants.PlayAndWaitTimeline(drawTimeline.DirectorParent, drawTimeline.Director);
        }

            yield return new WaitForSeconds(debugReloadTime);

        // テストプレイ用、シーンを再読み込み（接続待機画面に戻る）
        SceneManager.LoadScene("PizzaTestScene");

        // 個人戦のときの処理
        //systemManager.Teams.Sort((a, b) => (a.Score - b.Score));// スコアの大きい順にソート

        //yield return StartCoroutine(ExtendScoreBar(systemManager.Teams));// スコアのバーを伸ばす
    }

    public void SetResultGroupActive(bool active) => resultGroup.SetActive(active);

    /// <summary>
    /// 両チームの得点を表示(表示が終わるまで待つ)
    /// </summary>
    /// <param name="redTargetText">赤チームのテキスト</param>
    /// <param name="greenTargetText">緑チームのテキスト</param>
    /// <param name="redScore">赤チームのスコア</param>
    /// <param name="greenScore">緑チームのスコア</param>
    /// <param name="soundType">アニメーション完了時に鳴らすSE</param>
    IEnumerator ShowBothTeamScore(TextMeshProUGUI redTargetText, TextMeshProUGUI greenTargetText, int redScore, int greenScore, PlayerSoundType soundType)
    {
        // 両チームの得点表示
        if (redTargetText != null) StartCoroutine(numberCounter.CountUpNumBySpeed(redTargetText, redScore, numCountSpeed, () => PlaySE(soundType)));
        if (greenTargetText != null) StartCoroutine(numberCounter.CountUpNumBySpeed(greenTargetText, greenScore, numCountSpeed, () => PlaySE(soundType)));

        // アニメーション終了まで待つ
        yield return new WaitUntil(() => numberCounter.IsAllFinished);
    }

    // SEの再生（Windowsのみ）
    void PlaySE(PlayerSoundType soundType) => systemManager.PlaySE_Windows(soundType, transform);

    /// <summary>
    /// スコアを計算
    /// </summary>
    void CalcTeamScore()
    {
        // 初期化
        redFoodScore = GameConstants.Zero;
        greenFoodScore = GameConstants.Zero;

        foreach(Team team in systemManager.Teams)
        {
            // 赤チームにスコア加算
            if (team.Color == TeamColor.Red || team.Color == TeamColor.Yellow)
            {
                redFoodScore += team.Score;
                redPreScore += team.ExplosionScore;
            }

            // 緑チームにスコア加算
            if (team.Color == TeamColor.Green || team.Color == TeamColor.Blue)
            {
                greenFoodScore += team.Score;
                greenPreScore += team.ExplosionScore;
            }
        }
    }
}
