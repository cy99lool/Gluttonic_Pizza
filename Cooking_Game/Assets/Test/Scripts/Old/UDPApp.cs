using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;// UdpClient
using System.Net;// IPEndPoint,DNS等
using System;
using System.Threading;
using TMPro;

public class UDPApp : MonoBehaviour
{
    [SerializeField] Transform sendObject;
    [Header("IPアドレス"), SerializeField] TextMeshProUGUI ipText;
    //[Header("接続確認テキスト"), SerializeField] TextMeshProUGUI connectCheckText;
    [Header("IP"), SerializeField] GameObject myIpInput, opponentIpInput;
    [Header("ポート"), SerializeField] GameObject myPortInput, opponentPortInput;

    UdpClient udpClient;
    int tickRate = 10;// 一秒間あたりの送信回数
    Thread receiveThread;
    IPEndPoint receiveEndPoint = new IPEndPoint(IPAddress.Any, 9000);
    void Awake()
    {
        udpClient = new UdpClient(receiveEndPoint);

        receiveThread = new Thread(new ThreadStart(ThreadReceive));
        receiveThread.Start();
        Debug.Log("受信セットアップ完了");
        ipText.text = GetLocalIPAddress();
        //StartCoroutine(SendMessage());
    }


    void Update()
    {
        
    }

    void ThreadReceive()
    {
        while(true)
        {
            IPEndPoint senderEndPoint = null;
            byte[] receivedBytes = udpClient.Receive(ref senderEndPoint);
            Parse(senderEndPoint, receivedBytes);
        }
    }

    // 受信
    void Parse(IPEndPoint senderEndPoint, byte[] message)
    {
        // 受信時に行う処理
        string messageString = message.ToString();
        Debug.Log(messageString);
    }

    // メッセージ送信
    IEnumerator SendMessage()
    {
        yield return new WaitForSeconds(1f / tickRate);
        // 送信処理
        byte[] message = System.Text.Encoding.UTF8.GetBytes(sendObject.position.ToString());
        udpClient.SendAsync(message, message.Length, receiveEndPoint);
        Debug.Log(sendObject.position);
    }

    void RegisterOpponentPort(GameObject myIpText, GameObject opponentIpText, GameObject myPortText, GameObject opponentPortText)
    {
        if (myIpText == null || opponentIpText == null || myPortText == null || opponentPortText == null)
        {
            Debug.LogError("Not Enough Attachment!");
            return;
        }
        if (!myIpText.TryGetComponent<TMP_InputField>(out TMP_InputField myIp) || !opponentIpText.TryGetComponent<TMP_InputField>(out TMP_InputField opponentIp)
            || !myPortText.TryGetComponent(out TMP_InputField myPortField) || !opponentPortText.TryGetComponent(out TMP_InputField opponentPortField))
        {
            Debug.LogError("TMP_InputField has not been attached!");
            return;
        }

        // 下記の方法で実行確認できたら削除予定
        //string hostIpAddress = host != null ? host.text : "192.168.116.73";
        //string clientIpAddress = client != null ? client.text : "192.168.116.72";

        string myIpAddress = myIp.text;
        string myPort = myPortField.text;
        string opponentIpAddress = opponentIp.text;
        string opponentPort = opponentPortField.text;

        byte[] message = System.Text.Encoding.UTF8.GetBytes("100001");
        IPEndPoint opponentEP = new IPEndPoint(IPAddress.Parse(opponentIpAddress), int.Parse(opponentPort));// 相手のエンドポイントを設定
        udpClient.Send(message, message.Length, opponentEP);

    }

    public void ConnectButton()
    {
        RegisterOpponentPort(myIpInput, opponentIpInput, myPortInput, opponentPortInput);
    }

    /// <summary>
    /// IPv4アドレスを取得
    /// </summary>
    string GetLocalIPAddress()
    {
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());// ホスト名を取得
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)// IPv4の場合
            {
                return ip.ToString();
            }
        }
        // IPv4が無かったら
        throw new System.Exception("システムにIPv4アドレスのネットワークアダプターがありません！");
    }
}
