using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class QuickRenamer : EditorWindow
{
    // Unity上部メニューの「Tools > Quick Rename」から実行する
    [MenuItem("Tools/Quick Rename")]
    public static void Rename()
    {
        // 1つ以下しか選択されていなければ何もしない
        if (Selection.gameObjects.Length <= GameConstants.One) return;

        // 選択されたオブジェクトを取得（ヒエラルキー上の順序でソートされたものを得る）
        GameObject[] selectedObjects = Selection.gameObjects.OrderBy(targetObject  => targetObject.transform.GetSiblingIndex()).ToArray();

        // Undoグループの作成と命名
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Batch Rename Objects");
        int undoGroupIndex = Undo.GetCurrentGroup();

        // 選択された中でヒエラルキーの一番上にあるオブジェクトの名前を基準にする
        string baseName = selectedObjects[GameConstants.FirstIndex].name;
        
        // １つ目のオブジェクトは1番目として数える
        int baseNameIndex = GameConstants.One;
        for(int i = 0; i < selectedObjects.Length; i++)
        {
            // 各オブジェクトの変更を同じグループに記録
            Undo.RecordObject(selectedObjects[i], "Rename Object");

            // 名前変更（○つ目のオブジェクトは「オブジェクト名_○」となる）
            selectedObjects[i].name = $"{baseName}_{i + baseNameIndex}";
        }

        // Undoグループをまとめて記録
        Undo.CollapseUndoOperations(undoGroupIndex);

        Debug.Log($"{selectedObjects.Length} 個のオブジェクトを改名しました(Ctrl + Z で一括で復元できます)");
    }
}
