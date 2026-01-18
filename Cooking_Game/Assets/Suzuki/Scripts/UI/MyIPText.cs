using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;

public class MyIPText : MonoBehaviour
{
    [Header("表示テキスト"), SerializeField] TextMeshProUGUI myIPText;
    [SerializeField] UDPMulti udpMulti;

    void Awake()
    {
        if(udpMulti != null) myIPText.text = IPJsonDataManager.LoadIPSetting(udpMulti.MyRelativeFilePath);
    }

    public void UpdateMyIP()
    {
        // IPアドレス取得
        string myIP = GetLocalMyIPAddress();

        // 更新
        udpMulti.UpdateMyIP(myIP);

        // テキストに反映
        myIPText.text = myIP;
    }

    string GetLocalMyIPAddress()
    {
        // ホスト取得
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        foreach(IPAddress ip in host.AddressList)
        {
            // IPv4のアドレスのときのみ返す
            if(ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        }

        // なければ空白を返す
        return null;
    }
}
