using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net;

public class IPInputField : MonoBehaviour
{
    [Header("--- IPアドレス適用先の設定 ---")]
    [Header("変更を適用するスクリプト"), SerializeField] UDPMulti udpMulti;
    [Header("対象のプレイヤー番号（ホスト宛はプレイヤー1と同じく「1」）"), SerializeField] int playerNum = 1;
    [Header("適用するIPの入力文"), SerializeField] TMP_InputField ipInputField;

    void Awake()
    {
        // 有効化されたときにJSONファイルからIPアドレスを設定し、入力画面にも反映
        ipInputField.text = udpMulti.UpdateOtherIP(playerNum);
    }

    /// <summary>
    /// 変更を実際の通信設定に適用（ボタンでは引数を2つ設定できないため）
    /// </summary>
    public void OnEditIP()
    {
        // 入力がなければ変更を適用しない
        if (ipInputField == null || ipInputField.text == null) return;

        // IPアドレスが不正なら適用しない
        IPAddress checkAddress;
        if (!IPAddress.TryParse(ipInputField.text, out checkAddress))
        {
            Debug.Log("IPアドレスが不正");
            ipInputField.text = "";
            return;
        }

        udpMulti.ApplyIPChange(playerNum, ipInputField.text);
    }
}
