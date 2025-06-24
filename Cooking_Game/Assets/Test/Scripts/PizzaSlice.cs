using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PizzaSlice : MonoBehaviour
{
    List<FoodMove> foodList;// 食べ物のステータスを設定するスクリプトを別に作ったなら型をそちらに変更すること

    public List<FoodMove> FoodList => foodList;
    void Start()
    {
        foodList = new List<FoodMove>();
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<FoodMove>(out FoodMove food)) foodList.Add(food);   
    }

    void OnTriggerExit(Collider other)
    {
        // リストに含まれていれば除去
        for(int i = foodList.Count - 1; i >= 0; i--)
        {
            if(foodList[i].gameObject == other)
            {
                foodList.RemoveAt(i);
                return;
            }
        }
    }
}
