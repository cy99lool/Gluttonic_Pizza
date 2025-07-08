﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;
using System.Linq;

public class UDPMulti : MonoBehaviour
{
    [Serializable]
    public class ClientInfo
    {
        [SerializeField] string ip = "127.0.0.1";// 何も指定されなければ自身を指す
        [SerializeField] int port = 0;
        [Header("同期するオブジェクト"), SerializeField] GameObject trackObject;

        IPEndPoint endPoint;
        float disconnectTimer;
        public string IP => ip;
        public int Port => port;
        public GameObject TrackObject => trackObject;

        public IPEndPoint EndPoint => endPoint;
        public float DisconnectTimer => disconnectTimer;
        public void SetEP(IPEndPoint iPEndPoint)
        {
            endPoint = iPEndPoint;
        }
        public void ElapseDiscconectTimer()
        {
            disconnectTimer += Time.deltaTime;
        }
        public void ResetDiscconectTimer()
        {
            if(disconnectTimer > 0f) disconnectTimer = 0f;
        }
    }

    /// <summary>
    /// 仮置き、変更したものを使うかも
    /// </summary>]
    [Serializable]
    class PositionAndRotation
    {
        [SerializeField]ClientInfo clientInfo;
        [SerializeField]Vector3 position;
        [SerializeField]float yRot;

        public ClientInfo ClientInfo => clientInfo;
        public Vector3 Position => position;
        public float YRot => yRot;

        public PositionAndRotation(ClientInfo clientInfo, Vector3 position, float Yrot)
        {
            this.clientInfo = clientInfo;
            this.position = position;
            this.yRot = Yrot;
        }

        public void UpdateTransformInfo()
        {
            if (clientInfo.TrackObject == null) return;// 設定されていないなら動かさない

            clientInfo.TrackObject.transform.position = position;
            Vector3 eulerAngles = clientInfo.TrackObject.transform.eulerAngles;
            eulerAngles.y = yRot;
            clientInfo.TrackObject.transform.localEulerAngles = eulerAngles;
        }
        public void SetClientInfo(ClientInfo clientInfo)
        {
            this.clientInfo = clientInfo;
        }
    }

    class ReceivedUnit
    {
        ClientInfo clientInfo;
        byte[] message;
        public IPEndPoint SenderEP => clientInfo.EndPoint;
        public ClientInfo Info => clientInfo;
        public byte[] Message => message;

        public ReceivedUnit(IPEndPoint senderEp, byte[] message, ClientInfo clientInfo)
        {
            this.clientInfo = clientInfo;
            this.clientInfo.SetEP(senderEp);
            this.message = message;
        }
    }

    [Header("自分の情報"), SerializeField] ClientInfo myInfo;
    [Header("接続する相手たち"), SerializeField] List<ClientInfo> clients = new List<ClientInfo>();
    [Header("接続が切れた判定をするまでの時間"), SerializeField] float disconnectThreshold = 3f;

    int sendPerSecond = 20;                                            // 送信レート（秒間）
    UdpClient client;
    Thread receiveThread;                                           // 受信用スレッド
    Thread sendThread;                                              // 送信用スレッド
    bool isSendTiming = false;                                      // 送信タイミングかどうかのフラグ
    List<IPEndPoint> answerWaiting = new List<IPEndPoint>(4);       // 応答待機のリスト
    [SerializeField] List<ClientInfo> connectedPlayerInfos = new List<ClientInfo>(4);  // 接続できたプレイヤーのリスト
    List<ReceivedUnit> messageStack = new List<ReceivedUnit>(15);       // メッセージの待機列
    // ゲーム情報
    List<PositionAndRotation> otherPlayerObjectInfo = new List<PositionAndRotation>(3);

    void Start()
    {
        client = new UdpClient(new IPEndPoint(IPAddress.Any, myInfo.Port));
        receiveThread = new Thread(new ThreadStart(ThreadReceive));
        receiveThread.Start();// 受信スレッド開始
    }

    void Update()
    {
        // 送信タイミング
        if(isSendTiming)
        {
            BroadcastStatus();
            isSendTiming = false;
        }

        // 受信メッセージがある場合
        for(int i = 0; i < messageStack.Count; i++)
        {
            Parse(messageStack[i]);
            messageStack.RemoveAt(i);
            i--;
        }

        // 各プレイヤーの位置アップデート
        for(int i = otherPlayerObjectInfo.Count - 1; i > 0; i--)
        {
            otherPlayerObjectInfo[i].UpdateTransformInfo();
            otherPlayerObjectInfo.RemoveAt(i);
        }
        // 滑らかに移動させるときに使うかも
        //for(int i = 0; i < connectedPlayerEPs.Count; i++)
        //{
        //    if (i >= otherPlayerObjectInfo.Count) return;
            
        //    // 位置を反映

        //}

        // 通信が切断されているかの確認
        for(int i = connectedPlayerInfos.Count - 1; i >= 0; i--)
        {
            connectedPlayerInfos[i].ElapseDiscconectTimer();// 通信ができていない時間を計測
            if (connectedPlayerInfos[i].DisconnectTimer >= disconnectThreshold)
            {
                // 再接続を要求
                RegisterOpponentPort(connectedPlayerInfos[i].IP, connectedPlayerInfos[i].Port);
                Debug.Log("再接続を要求");

                // 接続リストから削除
                connectedPlayerInfos.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 指定されたIPアドレスとポート番号を接続待機リストに追加する
    /// </summary>
    void RegisterOpponentPort(string ip, int port)
    {
        byte[] udpMessage = UDPMessageType.AnswerWait.ToByte();
        byte[] infoMessage = myInfo.ToByte();
        byte[] message = MergeBytes(udpMessage, infoMessage);// 情報をメッセージにまとめる
        
        IPEndPoint opponentEP = new IPEndPoint(IPAddress.Parse(ip), port);
        client.Send(message, message.Length, opponentEP);
        answerWaiting.Add(opponentEP);// 接続待機リストに追加
        Debug.Log("IP:" + ip + "," + port + " に接続要求");
    }

    [ContextMenu("Register")]
    public void OnClickRegister()// Inspector上での右クリックメニュー
    {
        foreach (ClientInfo client in clients)
        {
            RegisterOpponentPort(client.IP, client.Port);
        }
    }

    public void OnRegister()// ボタン
    {
        foreach(ClientInfo client in clients)
        {
            RegisterOpponentPort(client.IP, client.Port);
        }
    }

    /// <summary>
    /// Byte配列を結合する
    /// </summary>
    /// <returns>結合後のByte配列</returns>
    byte[] MergeBytes(byte[] byte1, byte[] byte2)
    {
        byte[] message = new byte[byte1.Length + byte2.Length];// 合わせた長さで作成しておく

        Array.Copy(byte1, message, byte1.Length);
        Array.Copy(byte2, 0, message, byte1.Length, byte2.Length);

        return message;
    }

    /// <summary>
    /// 受信用のスレッド。受信した際に情報をスタックに保存しておく。
    /// </summary>
    void ThreadReceive()
    {
        while (true)
        {
            IPEndPoint senderEP = null;
            try// 情報を受け取れないときに切断されないようにしている
            {
                byte[] receivedBytes = client.Receive(ref senderEP);
                if (receivedBytes.Length - sizeof(Int32) > 0)
                {
                    try
                    {
                        // 接続時
                        ClientInfo clientInfo = SearchClientInfo(receivedBytes.ToClientInfo(sizeof(Int32)));// ClientInfoを取得

                        //Debug.Log($"受け取ったメッセージ長: {receivedBytes.Length}");
                        messageStack.Add(new ReceivedUnit(senderEP, receivedBytes, clientInfo));
                    }
                    catch
                    {
                        // 通信時
                        string objectInfoJson = System.Text.Encoding.UTF8.GetString(receivedBytes, sizeof(Int32), receivedBytes.Length - sizeof(Int32));// UDPMessage型のメッセージの先
                        PositionAndRotation objectInfo = JsonUtility.FromJson<PositionAndRotation>(objectInfoJson);

                        Debug.Log($"ポート：{objectInfo.ClientInfo.Port}, 受け取った位置：{objectInfo.Position}");
                        messageStack.Add(new ReceivedUnit(senderEP, receivedBytes, objectInfo.ClientInfo));
                    }
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }
    }

    /// <summary>
    /// オブジェクトのアタッチされたクライアント情報と結びつける
    /// </summary>
    ClientInfo SearchClientInfo(ClientInfo info)
    {
        for(int i = 0; i < clients.Count; i++)
        {
            if (clients[i].Port == info.Port)// 同じポート番号の場合
            {
                return clients[i];
            }
        }
        return null;
    }

    /// <summary>
    /// 送信用のスレッド。送信タイミングでisSendTimingをtrueにする。
    /// </summary>
    void ThreadSend()
    {
        while (true)
        {
            //OnUpdateSend();

            // 送信タイミングになった
            isSendTiming = true;
            Thread.Sleep(1000 / sendPerSecond);
        }
    }

    /// <summary>
    /// 受信したメッセージの内容によって処理を行う
    /// </summary>
    /// <param name="unit">受信した情報</param>
    void Parse(ReceivedUnit unit)
    {
        UDPMessageType type = unit.Message.ToUDPMessageType();
        //ClientInfo clientInfo = unit.Message.ToClientInfo(sizeof(Int32));// ClientInfoを取得

        int answerWaitRegisterIndex = answerWaiting.IndexOfPort(unit.SenderEP.Port);
        int connectedIndex = connectedPlayerInfos.IndexOfPort(unit.SenderEP.Port);

        // 接続している状況の更新
        CheckConnect(unit);

        //Debug.Log("メッセージを受信");
        switch (type)
        {
            case UDPMessageType.AnswerWait:
                {
                    Debug.Log(answerWaitRegisterIndex);
                    if (answerWaitRegisterIndex == -1) break;
                    connectedPlayerInfos.Add(unit.Info);
                    answerWaiting.RemoveAt(answerWaitRegisterIndex);
                    if (connectedPlayerInfos.Count == 1)
                    {
                        SendThreadStart();
                    }
                    Debug.Log("他の人から接続がありました:" + unit.Info.Port);
                    byte[] udpMessage = UDPMessageType.Answered.ToByte();
                    byte[] infoMessage = myInfo.ToByte();
                    byte[] message = MergeBytes(udpMessage, infoMessage);

                    client.SendAsync(message, message.Length, unit.SenderEP);

                    ActivateTrackObject(unit);
                    CheckConnect(unit);
                    break;
                }
            case UDPMessageType.Answered:
                {
                    print(answerWaitRegisterIndex);
                    if (answerWaitRegisterIndex == -1) break;
                    connectedPlayerInfos.Add(unit.Info);
                    answerWaiting.RemoveAt(answerWaitRegisterIndex);
                    if (connectedPlayerInfos.Count == 1)
                    {
                        SendThreadStart();
                    }
                    Debug.Log("他の人から接続がありました:" + unit.Info.Port);

                    ActivateTrackObject(unit);
                    CheckConnect(unit);
                    break;
                }
            case UDPMessageType.PositionUpdate:
                {
                    string objectInfoJson = System.Text.Encoding.UTF8.GetString(unit.Message, sizeof(Int32), unit.Message.Length - sizeof(Int32));// UDPMessage型のメッセージの先
                    PositionAndRotation playerObjectInfo = JsonUtility.FromJson<PositionAndRotation>(objectInfoJson);

                    playerObjectInfo.SetClientInfo(SearchClientInfo(playerObjectInfo.ClientInfo));// 動かすオブジェクトと結びつけたものに直す
                    otherPlayerObjectInfo.Add(playerObjectInfo);
                    //Debug.Log("位置を受信：" + unit.Info.Port);
                    break;
                }
            case UDPMessageType.ConnectCheck:
                {
                    // 接続しているかの確認のみなので何もしない
                    break;
                }
            default:
                {
                    Debug.LogError("形式が違います！");
                    break;
                }
        }
    }

    void CheckConnect(ReceivedUnit unit)
    {
        foreach (ClientInfo connectedPlayer in connectedPlayerInfos)
        {
            // 接続を確認したのでタイマーをリセット
            if (unit.Info.IP == connectedPlayer.IP)
            {
                connectedPlayer.ResetDiscconectTimer();
                break;
            }
        }
    }

    /// <summary>
    /// 通信相手全員に自分の状態を送る
    /// </summary>
    void BroadcastStatus()
    {
        // 送信処理
        // (これから自分の番かの判定を追加予定)
        if(myInfo.TrackObject != null) SendPosition();  // 位置を送る
        else SendOnlyConnection();                      // 接続しているかどうかの情報のみを送る
    }

    /// <summary>
    /// 位置の送信を行う
    /// </summary>
    void SendPosition()
    {
        byte[] udpMessage = UDPMessageType.PositionUpdate.ToByte();// 位置情報送信モード

        PositionAndRotation myObjectInfo = new PositionAndRotation(myInfo, myInfo.TrackObject.transform.position, myInfo.TrackObject.transform.eulerAngles.y);
        string myObjectInfoJson = JsonUtility.ToJson(myObjectInfo);
        byte[] myObjectInfoMessage = System.Text.Encoding.UTF8.GetBytes(myObjectInfoJson);// StringをByte配列に変換

        byte[] posMessage = MergeBytes(udpMessage, myObjectInfoMessage);// 結合

        SendAsyncToPlayers(posMessage);
    }
    /// <summary>
    /// 接続状況の送信を行う
    /// </summary>
    void SendOnlyConnection()
    {
        byte[] udpMessage = UDPMessageType.ConnectCheck.ToByte();// 接続状況送信モード

        SendAsyncToPlayers(udpMessage);
    }

    /// <summary>
    /// つながっているプレイヤー全員にメッセージを送信する
    /// </summary>
    void SendAsyncToPlayers(byte[] message)
    {
        foreach(ClientInfo clientInfo in connectedPlayerInfos)
        {
            client.SendAsync(message, message.Length, clientInfo.EndPoint);
        }
    }

    void SendThreadStart()
    {
        sendThread = new Thread(new ThreadStart(ThreadSend));
        sendThread.Start();
    }

    void ActivateTrackObject(ReceivedUnit unit)
    {
        if (unit.Info.TrackObject == null) return;

        for(int i = 0; i < connectedPlayerInfos.Count; i++)
        {
            if (connectedPlayerInfos[i].IP == unit.Info.IP && !connectedPlayerInfos[i].TrackObject.activeSelf)// 応答があったIPアドレスなら
            {
                connectedPlayerInfos[i].TrackObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// アプリ終了時にスレッドを終了させる
    /// </summary>
    void OnApplicationQuit()
    {
        if (sendThread != null) sendThread.Abort();
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}

enum UDPMessageType
{
    AnswerWait = 100001,
    Answered,
    PositionUpdate,
    ConnectCheck,
}

/// <summary>
/// 通信のための変換等を行う
/// </summary>
static class MultiPlayerMessenger
{
    public static byte[] ToByte(this UDPMessageType udpMessage)
    {
        return BitConverter.GetBytes((int)udpMessage);
    }
    public static byte[] ToByte(this UDPMulti.ClientInfo clientInfo)
    {
        string infoJson = JsonUtility.ToJson(clientInfo);// Json形式に変更
        return System.Text.Encoding.UTF8.GetBytes(infoJson);
    }

    public static UDPMessageType ToUDPMessageType(this byte[] bytes, int startIndex = 0)
    {
        int number = BitConverter.ToInt32(bytes, startIndex);
        return (UDPMessageType)Enum.ToObject(typeof(UDPMessageType), number);
    }
    public static UDPMulti.ClientInfo ToClientInfo(this byte[] bytes, int startIndex = 0)
    {
        string infoJson = System.Text.Encoding.UTF8.GetString(bytes, startIndex, bytes.Length - startIndex);// Json部分を抽出
        return JsonUtility.FromJson<UDPMulti.ClientInfo>(infoJson);// 本来の形式に直す
    }
    public static byte[] ToByte(this Vector3 vector3)
    {
        byte[] x = BitConverter.GetBytes(vector3.x);
        byte[] y = BitConverter.GetBytes(vector3.y);
        byte[] z = BitConverter.GetBytes(vector3.z);
        return x.Concat(y).Concat(z).ToArray();// 連結
    }
    public static Vector3 ToVector3(this byte[] bytes, int startIndex)
    {
        float x = BitConverter.ToSingle(bytes, startIndex);
        float y = BitConverter.ToSingle(bytes, startIndex + 4);// int型のサイズ分ずらしている
        float z = BitConverter.ToSingle(bytes, startIndex + 8);
        return new Vector3(x, y, z);
    }
    public static int IndexOfPort(this List<UDPMulti.ClientInfo> endPoints, int targetPort)
    {
        int index = -1;// 合うポートが見つからなければ-1を返すように
        for (int i = 0; i < endPoints.Count; i++)
        {
            if (endPoints[i].Port == targetPort) index = i;
        }
        return index;
    }

    public static int IndexOfPort(this List<IPEndPoint> endPoints, int targetPort)
    {
        int index = -1;// 合うポートが見つからなければ-1を返すように
        for (int i = 0; i < endPoints.Count; i++)
        {
            if (endPoints[i].Port == targetPort) index = i;
        }
        return index;
    }
}
