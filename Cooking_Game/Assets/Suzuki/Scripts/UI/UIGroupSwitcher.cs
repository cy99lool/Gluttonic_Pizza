using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGroupSwitcher : MonoBehaviour
{
    [Header("--- まとめて変更するUIの設定（必要がない箇所は割り当てなくても良い） ---")]
    [Header("有効にするUIグループ"), SerializeField] GameObject enableGroup;
    [Header("無効にするUIグループ"), SerializeField] GameObject disableGroup;

    /// <summary>
    /// 表示するUIグループを変更
    /// </summary>
    public void ChangeUIGroup()
    {
        // UIを有効化
        if(enableGroup != null && !enableGroup.activeSelf) enableGroup.SetActive(true);

        // UIを無効化
        if(disableGroup != null && disableGroup.activeSelf) disableGroup.SetActive(false);
    }

    public void EnableUIGroup(GameObject enableGroup)
    {
        if (enableGroup != null && !enableGroup.activeSelf) enableGroup.SetActive(true);
    }
}
