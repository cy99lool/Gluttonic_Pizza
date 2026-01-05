using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SystemManager;

public class EatTrigger : MonoBehaviour
{
    [Header("判定元の食べ物"), SerializeField] FoodMove food;

    void OnTriggerEnter(Collider other)
    {
        // 捕食モードでなければ捕食しない
        if (!food.EatMode) return;

        // 食べ物以外は捕食しない
        if (!other.TryGetComponent<FoodMove>(out FoodMove opponentFood)) return;

        // 捕食の相性を取得
        InteractionType type = FoodInteractionRules.GetInteractionType(food.Team, opponentFood.Team, food.EatMode);

        // 捕食リストに追加
        if(type == InteractionType.Eat)
        {
            food.StageManager.AddEatEventList(food, opponentFood);
        }
    }
}
