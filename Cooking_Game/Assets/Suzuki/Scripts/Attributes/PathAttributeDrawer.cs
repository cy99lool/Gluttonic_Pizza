using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
// 参考にしたサイト：https://kan-kikuchi.hatenablog.com/entry/PathAttribute_1
/// <summary>
/// PathAttributeがInsperctor上でどのように表示されるかを設定するクラス
/// </summary>
[CustomPropertyDrawer(typeof(PathAttribute))]
public class PathAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if(property.propertyType != SerializedPropertyType.String)// string以外に設定されていた場合は反応しない
        {
            return;
        }

        List<Object> dropObjectList = CreateDragAndDropGUI(position);// ドロップされたオブジェクトのリストを取得

        if(dropObjectList.Count > 0)// オブジェクトがドロップされたとき
        {
            property.stringValue = AssetDatabase.GetAssetPath(dropObjectList[0]);
            string removeString = "Assets/StreamingAssets/";
            if(property.stringValue.Substring(0, removeString.Length) == removeString)// "Assets/"が先頭にあったとき
            {
                property.stringValue = property.stringValue.Remove(0, removeString.Length);// "Assets/"を削除
            }
        }

        GUI.Label(position, property.displayName + "：" + property.stringValue);// 現在設定されているパスを表示
    }

    // ドラッグ＆ドロップのGUIを作成
    private List<Object> CreateDragAndDropGUI(Rect rect)
    {
        List<Object> dragAndDropObjects = new List<Object>();

        GUI.Box(rect, "");// ドラッグ＆ドロップできる範囲を描画

        if(!rect.Contains(Event.current.mousePosition))// マウスの位置が範囲内になければ無視する
        {
            return dragAndDropObjects;
        }

        EventType eventType = Event.current.type;
        if(eventType == EventType.DragUpdated || eventType == EventType.DragPerform)// ドラッグ＆ドロップで操作が更新されたor実行したとき
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;// カーソルに+のアイコンを表示

            if(eventType == EventType.DragPerform)
            {
                dragAndDropObjects = new List<Object>(DragAndDrop.objectReferences);// ドロップされたオブジェクトをリストに登録

                DragAndDrop.AcceptDrag();// ドラッグを受け付ける（ドラッグしてカーソルにくっついていたオブジェクトが戻らなくなるように）
            }

            Event.current.Use();// イベントを使用済みにする
        }

        return dragAndDropObjects;
    }
}
#endif
