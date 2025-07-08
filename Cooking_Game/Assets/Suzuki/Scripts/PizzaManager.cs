using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PizzaManager : MonoBehaviour
{
    [SerializeField] List<PizzaSlice> pizzaSlices;
    [Header("回転速度"), SerializeField] float rotateSpeed = 20f;

    bool canSpin = true;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DebugPick(10f));
    }

    // Update is called once per frame
    void Update()
    {
        if (canSpin) Spin(rotateSpeed);
    }

    /// <summary>
    /// ピザのスライスを取り上げ、上に乗っている具材に応じてポイントを獲得させる
    /// </summary>
    /// <param name="index">ピザの番号</param>
    public void TakePizzaSlice(int index)
    {
        if (index > pizzaSlices.Count) return;

        List<FoodMove> foodList = pizzaSlices[index].FoodList;// リストをコピー
        if (foodList.Count > 0)
        {
            for (int i = foodList.Count - 1; i >= 0; i--)
            {
                // 消去処理、ポイント獲得処理等を書く
                Debug.Log(foodList[i].name);
                foodList[i].gameObject.SetActive(false);
            }
            foodList.Clear();
        }

        pizzaSlices[index].gameObject.SetActive(false);// 仮の除去処理
        pizzaSlices.RemoveAt(index);// ピザのリストから除外
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

    IEnumerator DebugPick(float pickTime)
    {
        int pickIndex = Random.Range(0, pizzaSlices.Count);
        Debug.Log($"{pickIndex}が選ばれた");

        float timer = 0f;
        while (timer < pickTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        Debug.Log("取得");
        TakePizzaSlice(pickIndex);

        if(pizzaSlices.Count > 0) yield return StartCoroutine(DebugPick(pickTime));
    }
}
