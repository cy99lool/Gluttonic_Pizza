using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResolusionInitializer
{
    /// <summary>
    /// アプリの起動時に自動で解像度を初期化する
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeResolution()
    {
        // Resourcesフォルダ内のアセットを読み込む
        //ResolutionConfig config = Resources.Load<ResolutionConfig>("HostConfig");
        ResolutionConfig config = Resources.Load<ResolutionConfig>("PlayerConfig");

        if (config != null)
        {
            Screen.SetResolution(config.Width, config.Height, config.ScreenMode);
        }
    }
}
