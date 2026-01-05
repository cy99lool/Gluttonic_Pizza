using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class IPJsonDataManager
{
    /// <summary>
    /// IPアドレスの設定を保存する
    /// </summary>
    /// <param name="ip">保存するIPアドレス</param>
    /// <param name="relativeFilePath">保存する場所の相対パス</param>
    public static void SaveIPSetting(string ip, string relativeFilePath)
    {
        // nullチェック
        if (string.IsNullOrEmpty(ip)) return;

        // ラッパークラスを作成
        StringWrapper stringWrapper = new StringWrapper(ip);

        // JSONに変換
        string json = JsonUtility.ToJson(stringWrapper, true);

        WriteJsonToFile(json, relativeFilePath);
    }

    /// <summary>
    /// 保存されたIPアドレスの設定を取得する
    /// </summary>
    /// <param name="relativeFilePath">保存された場所の相対パス</param>
    /// <returns>IPアドレス</returns>
    public static string LoadIPSetting(string relativeFilePath)
    {
        // JSON文字列の取得
        string json = LoadJsonToString(relativeFilePath);

        // ファイルが存在しない等で、正常に取得できなければnull
        if(json == null) return null;

        // ラッパークラスを取得
        StringWrapper stringWrapper = JsonUtility.FromJson<StringWrapper>(json);
        Debug.Log("stringWrapper:" + stringWrapper.String);

        // ipアドレスを取得
        string ip = stringWrapper.String;

        return ip;
    }

    /// <summary>
    /// JSON文字列をファイルに書き出す
    /// </summary>
    /// <param name="Json">JSON文字列</param>
    /// <param name="relativeFilePath">ファイルの相対パス（フォルダ名/ファイル.json）</param>
    static void WriteJsonToFile(string Json, string relativeFilePath)
    {
        try
        {
            Debug.Log("書き込みを行います...");

            // persistentDataPathを含んだ完全なファイルパスを生成
            string fullPath = Path.Combine(Application.persistentDataPath, relativeFilePath);

            // ディレクトリパス（ファイル名を除く）を取得
            string directoryPath = Path.GetDirectoryName(fullPath);

            // ディレクトリが存在しなければ、新しく作成する
            if (!Directory.Exists(directoryPath) && !string.IsNullOrEmpty(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log($"[JSON Manager] Created directory: {directoryPath}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[JSON Manager] Failed to create directory: {ex.Message}");
                }
            }

            // 書き込み
            try
            {
                File.WriteAllText(fullPath, Json);
                Debug.Log($"[Json Manager] Saved at: {fullPath}");
            }
            // 失敗時
            catch (System.Exception ex)
            {
                Debug.LogError($"[Json Manager] Save Error for: {fullPath} due to: {ex}");
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogError($"[Json Manager] CRITICAL ERROR: {ex}");
        }
    }


    /// <summary>
    /// ファイルからJSON文字列を取得する
    /// </summary>
    /// <param name="relativeFilePath">保存された場所の相対パス</param>
    /// <returns>JSON文字列</returns>
    static string LoadJsonToString(string relativeFilePath)
    {
        // persistentDataPathを含んだ完全なファイルパスを生成
        string fullPath = Path.Combine(Application.persistentDataPath, relativeFilePath);
        Debug.Log($"fullPath:{fullPath}");

        // ファイルが見つからない場合、null
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[Json Manager] File not found: {fullPath}");
            return null;
        }

        // 取得
        try
        {
            string json = File.ReadAllText(fullPath);
            return json;
        }
        // 失敗時（nullを返す）
        catch (System.Exception ex)
        {
            Debug.LogError($"[Json Manager] Load Error for: {fullPath} due to: {ex}");
            return null;
        }
    }
}

/// <summary>
/// stringをJSON形式に変換するためのラッパークラス
/// </summary>
[Serializable]
public class StringWrapper
{
    [SerializeField] string targetString;
    public string String => targetString;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="targetString">JSON化したい文字列</param>
    public StringWrapper(string targetString)
    {
        this.targetString = targetString;
    }

    // デフォルトコンストラクタ（FromJsonの際に使用される）
    public StringWrapper() { }
}