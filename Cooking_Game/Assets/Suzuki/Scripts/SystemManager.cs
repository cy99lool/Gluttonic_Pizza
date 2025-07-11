using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    [System.Serializable]
    public class Team
    {
        [SerializeField]FoodMove.TeamColor color;
        public FoodMove.TeamColor Color => color;

        [SerializeField] int score;
        // チームごとのUIを追加する予定

        public void AddScore(int score)
        {
            this.score += score;
        }
    }

    [SerializeField] List<Team> teams;

    public List<Team> Teams => teams;

    PizzaManager pizzaManager;

    void Start()
    {
        pizzaManager = FindObjectOfType<PizzaManager>();

        StartCoroutine(Main());
    }

    IEnumerator Main()
    {
        while (pizzaManager.PizzaSlices.Count > 0)
        {
            // ピザの取得（デバッグ）
            yield return DebugPick(5f, 3);
        }
    }

    IEnumerator DebugPick(float pickTime, uint pickCount = 1)
    {
        // 選択個数がピザ切れの総数より多かった場合は、ピザ切れの総数にする
        if (pickCount > pizzaManager.PizzaSlices.Count) pickCount = (uint)pizzaManager.PizzaSlices.Count;

        //int pickIndex = Random.Range(0, pizzaManager.PizzaSlices.Count);
        List<int> pickIndexes = new List<int>();
        List<PizzaSlice> pickableSlices = new List<PizzaSlice>();
        //pickableSlices = pizzaManager.PizzaSlices;
        for(int i = 0; i < pizzaManager.PizzaSlices.Count; i++)
        {
            pickableSlices.Add(pizzaManager.PizzaSlices[i]);
        }

        // 取る個数分取る場所を指定
        for(int i = 0; i < pickCount; i++)
        {
            if (i >= pickableSlices.Count) break;

            int index = Random.Range(0, pickableSlices.Count);
            pickIndexes.Add(index);
            Debug.Log($"{pickableSlices[index]}が選ばれた");
            
            pickableSlices.RemoveAt(index);
        }

        float timer = 0f;
        while (timer < pickTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

            Debug.Log("取得");
            pizzaManager.TakePizzaSlice(pickIndexes);
    }
}
