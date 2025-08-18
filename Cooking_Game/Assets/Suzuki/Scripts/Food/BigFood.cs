using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigFood : FoodMove
{
    static readonly Vector3 ShrinkedScale = new Vector3(2f, 2f, 2f);

    [Header("発射時の勢いのかかりにくさ"), SerializeField] float shotFactor = 0.9f;
    [Header("着弾してから縮み始めるまでの時間"), SerializeField] float bigTime = 2f;
    [Header("縮み切るまでの時間"), SerializeField] float shrinkTime = 0.5f;
    [Header("縮んだ後の跳ね返りやすさ"), Range(0f, 100f), SerializeField] float shrinkReflectRate = 90f;

    Vector3 firstScale;
    bool shrinkCountStarted = false;

    new void Start()
    {
        base.Start();// 親クラスのStart

        firstScale = transform.localScale;
        shrinkCountStarted = false;
    }

    new void FixedUpdate()
    {
        base.FixedUpdate();// 親クラスのFixedUpdate

        // 初めて接地したときにカウントダウン開始
        if(!shrinkCountStarted && IsGround)
        {
            StartCoroutine(Shrink());
        }
    }

    IEnumerator Shrink()
    {
        shrinkCountStarted = true;// 縮むカウントに入った
        yield return new WaitForSeconds(bigTime);// 大きいままの時間を待つ

        float timer = GameConstants.FirstTimerValue;
        SetReflectRate(shrinkReflectRate);// 通常弾と同じ反射時の速度維持率にする

        // 縮小
        while(timer <= shrinkTime)
        {
            //// 親のスケールの影響を受けないように、グローバルなスケールを擬似的に指定している（グローバルなスケールはReadOnlyのため）
            //Vector3 targetScale = Vector3.Lerp(firstScale, ShrinkedScale, timer / shrinkTime);
            //transform.localScale = GameConstants.MultiplyVector3(GameConstants.DivineVector3(targetScale, transform.lossyScale), targetScale);
            transform.localScale = Vector3.Lerp(firstScale, ShrinkedScale, timer / shrinkTime);

            timer += Time.deltaTime;// 時間を経過
            yield return null;
        }

        // 縮小が完了した時間になったら、確実にスケールを設定しておく（誤差を防ぐため）
        transform.localScale = ShrinkedScale;
    }

    public override void AddForce(Vector3 direction, float power)
    {
        base.AddForce(direction, power * shotFactor);// 勢いに倍率をかけて、少しパワーを弱めて発射している
    }
}
