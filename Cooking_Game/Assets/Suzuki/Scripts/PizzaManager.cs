using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PizzaManager : MonoBehaviour
{
    [SerializeField] List<PizzaSlice> pizzaSlices;
    public List<PizzaSlice> PizzaSlices => pizzaSlices;

    [Header("回転速度"), SerializeField] float rotateSpeed = 20f;

    bool canSpin = false;
    [SerializeField] SystemManager systemManager;

    List<PizzaSlice> pickableSlices = new List<PizzaSlice>();
    public List<PizzaSlice> PickableSlices => pickableSlices;
    void RemovePickableSlices(PizzaSlice slice)
    {
        // リストやその中に対象が存在しなければreturn
        if (pickableSlices == null && !pickableSlices.Contains(slice)) return;

        // 対象を除外
        pickableSlices.Remove(slice);
    }

    void Start()
    {
        if(systemManager != null) systemManager = FindObjectOfType<SystemManager>();

        FillAllPickableSlices();
    }

    void Update()
    {
        if (canSpin) Spin(rotateSpeed);
    }

    /// <summary>
    /// ピザの場所を戻す
    /// </summary>
    public void ActivatePizzaSlices()
    {
        // ピザの場所を戻す
        //foreach(PizzaSlice slice in pizzaSlices) 

        // 再有効化（仮の処理）
        foreach (PizzaSlice slice in pizzaSlices) slice.gameObject.SetActive(true);
    }
    public void FillAllPickableSlices()
    {
        // リストを空にする
        pickableSlices.Clear();

        // リストにすべて追加
        pickableSlices.AddRange(pizzaSlices);
        //foreach(PizzaSlice slice in pizzaSlices) pickableSlices.Add(slice);
    }
    public IEnumerator PrepareTakePizza(float waitTime)
    {
        // ピザの上にあるすべての食べ物を取得
        List<FoodMove> foodList = GetAllFoodOnPizza();

        foreach(FoodMove food in foodList)
        {
            food.SetAnimatorBool("PickPhase", true);// ピザを取る前の表情に変化
        }
        yield return new WaitForSeconds(waitTime);
    }

    List<FoodMove> GetAllFoodOnPizza()
    {
        List<FoodMove> foodList = new List<FoodMove>();
        for (int i = 0; i < pizzaSlices.Count; i++)
        {
            for (int j = 0; j < pizzaSlices[i].FoodList.Count; j++)
            {
                foodList.Add(pizzaSlices[i].FoodList[j]);
            }
        }

        return foodList;
    }

    /// <summary>
    /// ピザのスライスを取り上げ、上に乗っている具材に応じてポイントを獲得させる
    /// </summary>
    /// <param name="pizzaIndexes">取り上げるスライス</param>
    public void TakePizzaSlice(List<int> pizzaIndexes)
    {
        // リストを小さい順にソート
        pizzaIndexes = SortByLowest(pizzaIndexes);

        // ピザを取り上げる処理
        for (int i = pizzaIndexes.Count - 1; i >= 0; i--)
        {
            if (pizzaIndexes[i] > pizzaSlices.Count) return;

            // 取得、スコア計上
            Take(pizzaSlices[pizzaIndexes[i]]);

            //pizzaSlices[pizzaIndexes[i]].gameObject.SetActive(false);// 仮の除去処理
            //pizzaSlices.RemoveAt(pizzaIndexes[i]);// ピザのリストから除外
            pickableSlices.RemoveAt(pizzaIndexes[i]);
            pizzaIndexes.RemoveAt(i);
        }
        Debug.Log(pickableSlices.Count);
    }

    /// <summary>
    /// ピザのスライスを取り上げ、上に乗っている具材に応じてポイントを獲得させる
    /// </summary>
    /// <param name="targetSlice">取り上げるスライス</param>
    public void TakePizzaSlice(PizzaSlice targetSlice)
    {
        PizzaSlice pickSlice = null;

        // スライスがあるか調べる
        foreach(PizzaSlice slice in pizzaSlices)
        {
            if(slice == targetSlice)
            {
                pickSlice = slice;
                break;
            }
        }

        // なければ取らない
        if (pickSlice == null) return;

        // ピザを取り上げる処理
        Take(pickSlice);

        // ハイライトを外す
        pickSlice.DisableHighlightObject();

        // 取得可能番号から除外
        RemovePickableSlices(pickSlice);
    }

    void Take(PizzaSlice slice)
    {
        List<FoodMove> foodList = slice.FoodList;// リストをコピー
        if (foodList.Count > 0)
        {
            for (int j = foodList.Count - 1; j >= 0; j--)
            {
                // 消去処理、ポイント獲得処理等を書く
                Debug.Log(foodList[j].Team);
                // ポイント増加処理
                AddScore(foodList[j]);

                foodList[j].gameObject.SetActive(false);
            }
            foodList.Clear();
        }

        // 除去処理（仮）
        slice.gameObject.SetActive(false);
    }

    /// <summary>
    /// 食材ごとのポイントの加算
    /// </summary>
    /// <param name="food">調べる食材</param>
    void AddScore(FoodMove food)
    {
        foreach (SystemManager.Team team in systemManager.Teams)
        {
            // 同じ色のチームにポイントを与える
            if (food.Team == team.Color)
            {
                team.AddScore(food.ScorePoint);
                Debug.Log(team.Color + ":" + team.Score);
                return;// 与えたらそれ以降の処理は行わない
            }
        }
    }

    /// <summary>
    /// 指定したチームに得点を加算
    /// </summary>
    /// <param name="color">チーム</param>
    /// <param name="score">加算する得点</param>
    public void AddScore(TeamColor color, int score)
    {
        foreach(SystemManager.Team team in systemManager.Teams)
        {
            // 同じ色のチームにポイントを与える
            if (color == team.Color)
            {
                team.AddScore(score);
                Debug.Log(team.Color + ":" + team.Score);
                return;// 与えたらそれ以降の処理は行わない
            }
        }
    }

    public void AddExplosionScore(TeamColor color, int score)
    {
        // ゲーム開始前の爆発はカウントしない
        if (systemManager.CurrentPhase != SystemManager.GamePhase.InGame) return;

        foreach (SystemManager.Team team in systemManager.Teams)
        {
            // 同じ色のチームにポイントを与える
            if (color == team.Color)
            {
                team.AddExplosionScore(score);
                Debug.Log(team.Color + ":" + team.ExplosionScore);
                return;// 与えたらそれ以降の処理は行わない
            }
        }
    }

    /// <summary>
    /// すべてのピザを取得、ポイントを計算（アニメーションのイベントから呼ぶことでアニメーションの回収タイミングと同期できる）
    /// </summary>
    public void TakeAllPizza()
    {
        foreach(PizzaSlice slice in pizzaSlices)
        {
            // 取得、ポイント計上
            Take(slice);
        }
        //pizzaSlices.Clear();
    }

    public void ClearAllFood()
    {
        foreach(PizzaSlice slice in pizzaSlices)
        {
            ClearFood(slice);
        }
    }

    void ClearFood(PizzaSlice slice)
    {
        List<FoodMove> foodList = slice.FoodList;// リストをコピー
        if (foodList.Count > 0)
        {
            for (int j = foodList.Count - 1; j >= 0; j--)
            {
                // 消去処理
                Destroy(foodList[j].gameObject);
            }
            foodList.Clear();
        }
    }

    public void StartSpin()
    {
        canSpin = true;
    }
    public void StopSpin()
    {
        canSpin = false;
    }

    /// <summary>
    /// ピザを回転させる
    /// </summary>
    void Spin(float speed)
    {
        Vector3 angles = transform.eulerAngles;
        angles.y += speed * Time.deltaTime;
        transform.eulerAngles = angles;
    }

    /// <summary>
    /// リストの値が小さい順にソートする
    /// </summary>
    /// <param name="baseList">並び替えるリスト</param>
    /// <returns>数字が低い順に並んだリスト</returns>
    List<int> SortByLowest(List<int> baseList)
    {
        // 要素数0ならソートしない（アクセスしようとするとエラーが起きる）
        if (baseList.Count == 0) return baseList;

        // バブルソートを使用（想定される最大の要素数が8と少ないため）
        for (int i = 0; i < baseList.Count; i++)
        {
            for (int j = 0; j < baseList.Count - i - 1; j++)
            {
                if (baseList[j] > baseList[j + 1])// 前の要素の値が、後の要素の値より大きいとき
                {
                    int tempNum = baseList[j];      // 値をコピーしておく（後の要素の値になる方）
                    baseList[j] = baseList[j + 1];  // 前の要素に値を代入
                    baseList[j + 1] = tempNum;      // 後の要素の値を代入
                }
            }
        }

        return baseList;
    }
}
