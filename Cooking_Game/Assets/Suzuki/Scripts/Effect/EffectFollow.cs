using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectFollow : MonoBehaviour
{
    Transform target;

    public void SetTarget(Transform target) => this.target = target;

    void Update()
    {
        if(target == null) return;

        // 追従
        transform.position = target.position;
    }
}
