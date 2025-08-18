using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpButton : MonoBehaviour
{
    [Header("パワーアップ適用時のボタンの見た目"), SerializeField] Sprite onPowerUpSprite;
    [Header("適用させるプレイヤーのトラッカー"), SerializeField] CursorInfo cursor;
    [Header("対応させるパワーアップ項目"), SerializeField] CursorInfo.Mode mode;

    UnityEngine.UI.Image image;
    Sprite defaultSprite;

    void Start()
    {
        image = GetComponent<UnityEngine.UI.Image>();// 自身のボタンに使用している画像
        defaultSprite = image.sprite;// 通常時のスプライトを設定
    }

    void Update()
    {
        // パワーアップが適用されていないとき
        if(cursor.FoodMode != mode) image.sprite = defaultSprite;

        // 対応したパワーアップが適用されているとき
        if (cursor.FoodMode == mode) image.sprite = onPowerUpSprite;
    }
}
