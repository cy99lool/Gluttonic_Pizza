using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PizzaManager : MonoBehaviour
{
    [SerializeField] List<PizzaSlice> pizzaSlices;
    [Header("回転速度"), SerializeField] float rotateSpeed = 20f;

    bool canSpin = true;

    public List<PizzaSlice> PizzaSlices => pizzaSlices;

    void OnValidate()
    {
        for (int i = 0; i < pizzaSlices.Count; i++)
        {
            pizzaSlices[i].SetIndex(i);
        }
    }

    void Update()
    {
        if (canSpin) Spin(rotateSpeed);
    }

    /// <summary>
    /// ピザのスライスを取り上げ、上に乗っている具材に応じてポイントを獲得させる
    /// </summary>
    /// <param name="indexes">取り上げるスライス</param>
    public void TakePizzaSlice(List<int> indexes)
    {
        // リストを小さい順にソート
        indexes = SortByLowest(indexes);

        // ピザを取り上げる処理
        for (int i = indexes.Count - 1; i >= 0; i--)
        {
            if (indexes[i] > pizzaSlices.Count) return;

            List<FoodMove> foodList = pizzaSlices[indexes[i]].FoodList;// リストをコピー
            if (foodList.Count > 0)
            {
                for (int j = foodList.Count - 1; j >= 0; j--)
                {
                    // 消去処理、ポイント獲得処理等を書く
                    Debug.Log(foodList[i].name);
                    foodList[i].gameObject.SetActive(false);
                }
                foodList.Clear();
            }

            pizzaSlices[indexes[i]].gameObject.SetActive(false);// 仮の除去処理
            pizzaSlices.RemoveAt(indexes[i]);// ピザのリストから除外
            indexes.RemoveAt(i);

            // 残りのインデックスの指定がずれる場合の対策
            //for(int j = i; j < indexes.Count; j++)
            //{
            //    if (indexes[j] > indexes[i]) indexes[j]--;// ピザのリストの削除分ずらす
            //}
        }
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
