using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title : MonoBehaviour
{
    [Header("遷移先のシーン"), SerializeField] Object nextScene;
    [Header("遷移にかける時間"), SerializeField] float changeSceneSeconds;

    bool changing = false;
    // Start is called before the first frame update
    void Start()
    {
        changing = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown && !changing) StartCoroutine(ChangeScene(changeSceneSeconds));
    }

    IEnumerator ChangeScene(float changeSeconds)
    {
        changing = true;
        float timer = GameConstants.FirstTimerValue;
        Debug.Log("Change to Next Scene...");

        while(timer < changeSeconds)
        {
            timer += Time.deltaTime;

            if(timer >= changeSeconds) Debug.Log("Loading...");
            yield return null;
        }
        SceneManager.LoadScene(nextScene.name);
    }
}
