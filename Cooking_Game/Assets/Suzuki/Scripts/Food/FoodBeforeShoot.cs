using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodBeforeShoot : FoodMove
{
    new void Start()
    {
        base.Start();
    }

    new void FixedUpdate()
    {
        // 常に発射前の表情
        if (animator != null) animator.SetBool("Ready", true);
    }
}
