using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PizzaSlice : MonoBehaviour
{
    [SerializeField] int index;
    public int Index => index;
    public void SetIndex(int index)
    {
        this.index = index;
    }

    List<FoodMove> foodList;// 食べ物のステータスを設定するスクリプトを別に作ったなら型をそちらに変更すること
    public List<FoodMove> FoodList
    {
        get
        {
            foodList = new List<FoodMove>();

            // 自身の子の食べ物を取得
            foreach(FoodMove foodMove in GetComponentsInChildren<FoodMove>())
            {
                foodList.Add(foodMove);
            }
            return foodList;
        }
    }
}
