using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SystemManager;

public class Result : MonoBehaviour
{
    [SerializeField] SystemManager systemManager;
    [Header("リザルトUI"), SerializeField] GameObject resultGroup;
    [Header("カウントするスピード（スコア/秒）"), SerializeField] int countSpeed;
    [Header("バーの長さ（スコアあたり）"), SerializeField] float extendPerScore = 3f;
    [Header("メイン画面\nリザルトバーの表示位置"), SerializeField] List<RectTransform> mainUIBarPositions;
    [Header("タブレット画面\nリザルトバーの表示位置"), SerializeField] List<RectTransform> tabletUIBarPositions;

    // リザルト画面表示
    public IEnumerator ShowResult()
    {
        if (resultGroup != null) resultGroup.SetActive(true);// リザルトUIを有効化

        systemManager.Teams.Sort((a, b) => (a.Score - b.Score));// スコアの大きい順にソート

        yield return StartCoroutine(ExtendScoreBar(systemManager.Teams));// スコアのバーを伸ばす
    }

    IEnumerator ExtendScoreBar(List<SystemManager.Team> teams)
    {
        int maxScore = teams[GameConstants.HeadIndex].Score;// 最大スコアを記録

        // 順位の位置設定
        for (int i = 0; i < teams.Count; i++)
        {
            if (teams[i].MainScoreBar != null && mainUIBarPositions[i] != null) teams[i].MainScoreBar.anchoredPosition = mainUIBarPositions[i].anchoredPosition;
        }

        // 伸ばす
        int nowScore = 0;
        while(nowScore < maxScore)
        {
            foreach(SystemManager.Team team in teams)
            {
                // スコアを更新する対象はゲージを伸ばす
                if (nowScore <= team.Score && team.MainScoreBar != null)
                {
                    // スコアをカウントアップするならここに追加
                    team.MainScoreBar.offsetMax = new Vector2(team.MainScoreBar.offsetMax.x + nowScore * extendPerScore, team.MainScoreBar.offsetMax.y);
                }
            }

            // スコア加算
            nowScore += (int)(countSpeed * Time.deltaTime);
            if(nowScore > maxScore) nowScore = maxScore;// 最大スコアを超えないように

            yield return null;
        }
    }
}
