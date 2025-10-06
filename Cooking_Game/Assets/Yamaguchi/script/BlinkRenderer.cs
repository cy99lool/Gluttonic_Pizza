using UnityEngine;
using System.Collections;

public class BlinkAll : MonoBehaviour
{
    private Renderer[] renderers;
    public float interval = 0.3f;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (true)
        {
            foreach (Renderer r in renderers)
                r.enabled = !r.enabled;

            yield return new WaitForSeconds(interval);
        }
    }
}
