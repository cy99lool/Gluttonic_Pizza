using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PizzaSlice : MonoBehaviour
{
    [SerializeField] PizzaManager pizzaManager;
    [Header("ピザが選択されているときのハイライト"), SerializeField] GameObject highlightObject;
    [Header("焼けた後の見た目"), SerializeField] Renderer cookedRenderer;
    public Renderer CookedRenderer => cookedRenderer;

    Vector3 startPos;
    public Vector3 StartPos => startPos;

    List<FoodMove> foodList;// 食べ物のステータスを設定するスクリプトを別に作ったなら型をそちらに変更すること
    public List<FoodMove> FoodList
    {
        get
        {
            foodList = new List<FoodMove>();

            // 自身の子の食べ物を取得
            foreach (FoodMove foodMove in GetComponentsInChildren<FoodMove>())
            {
                foodList.Add(foodMove);
            }
            return foodList;
        }
    }

    void Start()
    {
        startPos = transform.position;
    }

    public void PickThisSlice() => pizzaManager.TakePizzaSlice(this);

    public void EnableHighlightObject()
    {
        if (highlightObject == null) return;
        // 有効化
        if (!highlightObject.activeSelf) highlightObject.SetActive(true);
    }
    public void DisableHighlightObject()
    {
        if (highlightObject == null) return;
        // 無効化
        if (highlightObject.activeSelf) highlightObject.SetActive(false);
    }
}
