using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SystemManager;
using TMPro;
using UnityEngine.SceneManagement;

public class Result : MonoBehaviour
{
    [SerializeField] SystemManager systemManager;
    [Header("--- リザルト表示設定 ---")]
    [Header("リザルトUI"), SerializeField] GameObject resultGroup;
    [Header("カウントするスピード（スコア/秒）"), SerializeField] int countSpeed;
    [Header("バーの長さ（スコアあたり）"), SerializeField] float extendPerScore = 3f;
    [Header("メイン画面\nリザルトバーの表示位置"), SerializeField] List<RectTransform> mainUIBarPositions;
    [Header("タブレット画面\nリザルトバーの表示位置"), SerializeField] List<RectTransform> tabletUIBarPositions;

    [Header("--- チームのスコア設定 ---")]
    [Header("赤チームの得点表示テキスト"), SerializeField] TextMeshProUGUI redText;
    [Header("緑チームの得点表示テキスト"), SerializeField] TextMeshProUGUI greenText;

    float redTeamPizzaScore;
    float redTeamExplosionScore;

    float greenTeamPizzaScore;
    float greenTeamExplosionScore;

    // リザルト画面表示
    public IEnumerator ShowResult(float debugReloadTime = 5f)
    {
        if (resultGroup != null) resultGroup.SetActive(true);// リザルトUIを有効化

        // チームのスコア計算
        CalcTeamScore();

        // 得点表示
        if(redText != null) redText.text = "赤チーム：" + (redTeamPizzaScore + redTeamExplosionScore);
        if(greenText != null) greenText.text = "緑チーム：" + (greenTeamPizzaScore + greenTeamExplosionScore);

        yield return new WaitForSeconds(debugReloadTime);

        // テストプレイ用、シーンを再読み込み（接続待機画面に戻る）
        SceneManager.LoadScene("PizzaTestScene");

        // 個人戦のときの処理
        //systemManager.Teams.Sort((a, b) => (a.Score - b.Score));// スコアの大きい順にソート

        //yield return StartCoroutine(ExtendScoreBar(systemManager.Teams));// スコアのバーを伸ばす
    }

    void CalcTeamScore()
    {
        // 初期化
        redTeamPizzaScore = GameConstants.Zero;
        greenTeamPizzaScore = GameConstants.Zero;

        foreach(Team team in systemManager.Teams)
        {
            // 赤チームにスコア加算
            if (team.Color == TeamColor.Red || team.Color == TeamColor.Yellow)
            {
                redTeamPizzaScore += team.Score;
                redTeamExplosionScore += team.ExplosionScore;
            }

            // 緑チームにスコア加算
            if (team.Color == TeamColor.Green || team.Color == TeamColor.Blue)
            {
                greenTeamPizzaScore += team.Score;
                greenTeamExplosionScore += team.ExplosionScore;
            }
        }
    }

    //IEnumerator ExtendScoreBar(List<SystemManager.Team> teams)
    //{
    //    int maxScore = teams[GameConstants.HeadIndex].Score;// 最大スコアを記録

    //    // 順位の位置設定
    //    for (int i = 0; i < teams.Count; i++)
    //    {
    //        if (teams[i].MainScoreBar != null && mainUIBarPositions[i] != null) teams[i].MainScoreBar.anchoredPosition = mainUIBarPositions[i].anchoredPosition;
    //    }

    //    // 伸ばす
    //    int nowScore = 0;
    //    while(nowScore < maxScore)
    //    {
    //        foreach(SystemManager.Team team in teams)
    //        {
    //            // スコアを更新する対象はゲージを伸ばす
    //            if (nowScore <= team.Score && team.MainScoreBar != null)
    //            {
    //                // スコアをカウントアップするならここに追加
    //                team.MainScoreBar.offsetMax = new Vector2(team.MainScoreBar.offsetMax.x + nowScore * extendPerScore, team.MainScoreBar.offsetMax.y);
    //            }
    //        }

    //        // スコア加算
    //        nowScore += (int)(countSpeed * Time.deltaTime);
    //        if(nowScore > maxScore) nowScore = maxScore;// 最大スコアを超えないように

    //        yield return null;
    //    }
    //}
}
