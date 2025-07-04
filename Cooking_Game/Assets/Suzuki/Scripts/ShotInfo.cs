using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 食べ物の発射時に使用する情報
/// </summary>
public class ShotInfo
{
    GameObject foodPrefab;
    public GameObject FoodPrefab => foodPrefab;
    
    Vector3 shotPosition;
    public Vector3 ShotPosition => shotDirection;

    Vector3 shotDirection;
    public Vector3 ShotDirection => shotDirection;

    float shotPower;
    public float ShotPower => shotPower;

    public ShotInfo(GameObject foodPrefab, Vector3 shotPosition, Vector3 shotDirection, float shotPower)
    {
        this.foodPrefab = foodPrefab;
        this.shotPosition = shotPosition;
        this.shotDirection = shotDirection;
        this.shotPower = shotPower;
    }
}
