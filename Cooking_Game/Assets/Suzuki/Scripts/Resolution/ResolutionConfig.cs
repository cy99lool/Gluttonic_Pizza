using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="ResolutionConfig", menuName ="Configs/ResolutionConfig")]
public class ResolutionConfig : ScriptableObject
{
    [Header("--- 解像度設定 ---")]
    [Header("幅(px)"), SerializeField] int width = 1920;
    public int Width => width;

    [Header("高さ(px)"), SerializeField] int height = 1080;
    public int Height => height;

    [Header("スクリーンモード"), SerializeField] FullScreenMode screenMode = FullScreenMode.Windowed;
    public FullScreenMode ScreenMode => screenMode;
    
}
