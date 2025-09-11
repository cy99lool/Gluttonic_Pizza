using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PizzaManager : MonoBehaviour
{
    [SerializeField] List<PizzaSlice> pizzaSlices;
    [Header("回転速度"), SerializeField] float rotateSpeed = 20f;

    bool canSpin = false;
    SystemManager systemManager;

    public List<PizzaSlice> PizzaSlices => pizzaSlices;

    void Start()
    {
        systemManager = FindObjectOfType<SystemManager>();
    }

    void Update()
    {
        if (canSpin) Spin(rotateSpeed);
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

            List<FoodMove> foodList = pizzaSlices[pizzaIndexes[i]].FoodList;// リストをコピー
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

            pizzaSlices[pizzaIndexes[i]].gameObject.SetActive(false);// 仮の除去処理
            pizzaSlices.RemoveAt(pizzaIndexes[i]);// ピザのリストから除外
            pizzaIndexes.RemoveAt(i);
        }
    }

    void AddScore(FoodMove food)
    {
        for(int i = 0; i < systemManager.Teams.Count; i++)
        {
            // 同じ色のチームにポイントを与える
            if(food.Team == systemManager.Teams[i].Color)
            {
                systemManager.Teams[i].AddScore(food.ScorePoint);
                return;// 与えたらそれ以降の処理は行わない
            }
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
