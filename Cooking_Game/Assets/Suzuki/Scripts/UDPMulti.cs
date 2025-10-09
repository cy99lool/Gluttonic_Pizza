using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;// ConCurrentQueue（スレッドセーフなキュー）を使う

public class UDPMulti : MonoBehaviour
{
    [Serializable]
    public class ClientInfo
    {
        [SerializeField] string ip = "127.0.0.1";// 何も指定されなければ自身を指す
        public string IP => ip;

        [SerializeField] int port = 0;
        public int Port => port;

        [Header("同期するオブジェクト"), SerializeField] GameObject trackObject;
        public GameObject TrackObject => trackObject;

        // アイテム適用情報を持つデータのクラス
        [SerializeField] CursorInfo cursorInfo;
        public CursorInfo Cursor => cursorInfo;

        IPEndPoint endPoint;
        float disconnectTimer;

        public IPEndPoint EndPoint => endPoint;
        public float DisconnectTimer => disconnectTimer;

        // コンストラクタ
        public ClientInfo() { }

        public ClientInfo(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void SetEP(IPEndPoint iPEndPoint)
        {
            endPoint = iPEndPoint;
        }

        // 再接続要求のためのメソッド
        public void ElapseDiscconectTimer()
        {
            // 接続できていない時間の経過
            disconnectTimer += Time.deltaTime;
        }
        public void ResetDiscconectTimer()
        {
            // 接続できていない時間をリセット
            disconnectTimer = 0f;
        }

        //public void SetCursorInfo()
        //{
        //    cursorInfo = trackObject.GetComponent<CursorInfo>();
        //}
    }

    /// <summary>
    /// Json形式で通信するクラス
    /// </summary>]
    [Serializable]
    class ObjectInfo
    {
        [SerializeField] ClientInfo clientInfo;
        [SerializeField] Vector3 position;
        [SerializeField] float yRot;

        public ClientInfo ClientInfo => clientInfo;
        public Vector3 Position => position;
        public float YRot => yRot;

        // 送信時に別で保存しておく
        [SerializeField] CursorInfo.Mode nowFoodMode;
        public CursorInfo.Mode NowFoodMode => nowFoodMode;

        [SerializeField] List<CursorInfo.Mode> canModeList;
        public List<CursorInfo.Mode> CanModeList => canModeList;

        public ObjectInfo(ClientInfo clientInfo, Vector3 position, float Yrot)
        {
            this.clientInfo = clientInfo;
            this.position = position;
            this.yRot = Yrot;
        }

        public void UpdateTransformInfo()
        {
            if (clientInfo.TrackObject == null) return;// 設定されていないなら動かさない

            // 動かす
            clientInfo.TrackObject.transform.position = position;
            Vector3 eulerAngles = clientInfo.TrackObject.transform.eulerAngles;
            eulerAngles.y = yRot;
            clientInfo.TrackObject.transform.localEulerAngles = eulerAngles;
        }
        public void SetClientInfo(ClientInfo clientInfo)
        {
            this.clientInfo = clientInfo;
        }

        // 送信時に行う処理
        public void OnSend()
        {
            nowFoodMode = clientInfo.Cursor.FoodMode;// 現在のモードを設定
            //Debug.Log(nowFoodMode);
            canModeList = clientInfo.Cursor.CanModes;// 移行可能なモード一覧を設定
        }
    }

    class ReceivedUnit
    {
        ClientInfo clientInfo;
        byte[] message;
        public IPEndPoint SenderEP => clientInfo != null ? clientInfo.EndPoint : null;
        public ClientInfo Info => clientInfo;
        public byte[] Message => message;

        public ReceivedUnit(IPEndPoint senderEp, byte[] message, ClientInfo clientInfo)
        {
            this.clientInfo = clientInfo;
            if (this.clientInfo != null) this.clientInfo.SetEP(senderEp);
            this.message = message;
        }
    }

    [Header("自分の情報"), SerializeField] ClientInfo myInfo;
    [Header("接続する相手たち"), SerializeField] List<ClientInfo> clients = new List<ClientInfo>();
    [Header("接続が切れた判定をするまでの時間"), SerializeField] float disconnectThreshold = 3f;
    [SerializeField] SystemManager systemManager;

    const int MaxPlayerNum = 4;                                     // 最大プレイヤー数
    const int MessageStackSize = 30;                                // メッセージの待機列のサイズ
    const int PosDataMargin = 3;                                    // 受け取った位置情報の保有可能量
    const int RecieveBufferSize = 65536;                            // 受信バッファのサイズ
    const int MaxParsePerFrame = 100;                               // 1フレームごとのパース可能回数
    const int ThreadSleepMillisecond = 1;                           // スレッドの処理を一時停止する時間（ミリ秒）

    static int sendPerSecond = 10;                                // 1秒に何回送信するか
    static float SendInterval => (GameConstants.OneSecond / sendPerSecond) * GameConstants.MillisecondPerSecond;// 送信ごとの間隔（1秒 / 1秒に送信する回数、ミリ秒の単位）

    UdpClient client;
    Thread receiveThread;                                           // 受信用スレッド
    Thread sendThread;                                              // 送信用スレッド
    bool isSendTiming = false;                                      // 送信タイミングかどうかのフラグ
    volatile bool isReceiving = false;                              // 受信を行っている（受信スレッドをループしている）かどうか
    bool isSending = false;                                         // 送信を行っているかどうか
    List<IPEndPoint> answerWaiting = new List<IPEndPoint>(MaxPlayerNum);       // 応答待機のリスト
    [SerializeField] List<ClientInfo> connectedPlayerInfos = new List<ClientInfo>(MaxPlayerNum);  // 接続できたプレイヤーのリスト
    ConcurrentQueue<ReceivedUnit> messageQueue = new ConcurrentQueue<ReceivedUnit>();       // メッセージの待機列（ConCurrentQueueを使用することで複数のスレッドでも安心）
    // ゲーム情報
    List<ObjectInfo> otherPlayerObjectInfo = new List<ObjectInfo>(PosDataMargin);

    void Start()
    {
        client = new UdpClient(new IPEndPoint(IPAddress.Any, myInfo.Port));
        client.Client.ReceiveBufferSize = RecieveBufferSize;

        isReceiving = true;
        receiveThread = new Thread(new ThreadStart(ThreadReceive));
        receiveThread.Start();// 受信スレッド開始

        isSending = false;
    }

    float debugTimer = 0f;
    void Update()
    {
        // 送信タイミング
        if (isSendTiming)
        {
            BroadcastStatus();
            isSendTiming = false;
        }

        //// デバッグ、現在のメッセージキューのサイズを1秒ごとにだす
        //debugTimer += Time.deltaTime;
        //if (debugTimer >= 1f)
        //{
        //    debugTimer = 0f;
        //    Debug.Log($"[QUEUE] size = {messageQueue.Count}");
        //    foreach (ClientInfo player in connectedPlayerInfos)
        //    {
        //        Debug.Log($"[DisconnectTimer] {player.IP}'s timer = {player.DisconnectTimer}");
        //    }
        //}

        // パース
        ParseMessages();

        // 各プレイヤーの情報アップデート
        for (int i = otherPlayerObjectInfo.Count - 1; i >= 0; i--)
        {
            otherPlayerObjectInfo[i].UpdateTransformInfo();// 位置の更新

            // 強化状況の更新
            for (int j = 0; j < clients.Count; j++)
            {
                // 対象の特定
                if (otherPlayerObjectInfo[i].ClientInfo.IP == clients[j].IP)
                {
                    if (clients[j].Cursor != null)
                    {
                        // 2回の変更でオブジェクトのモードが揃っていたとき
                        clients[j].Cursor.SetMode(otherPlayerObjectInfo[i].NowFoodMode);// 食材のモードを更新
                        clients[j].Cursor.SetModeFlag(otherPlayerObjectInfo[i].CanModeList);// 移行可能モードを更新

                    }
                }
            }

            otherPlayerObjectInfo.RemoveAt(i);
        }

        // 通信が切断されているかの確認
        for (int i = connectedPlayerInfos.Count - 1; i >= 0; i--)
        {
            connectedPlayerInfos[i].ElapseDiscconectTimer();// 通信ができていない時間を計測
            if (connectedPlayerInfos[i].DisconnectTimer >= disconnectThreshold)
            {
                // 再接続を要求
                RequestReconnection(connectedPlayerInfos[i].IP, connectedPlayerInfos[i].Port);

                // 接続リストから削除
                connectedPlayerInfos.RemoveAt(i);
            }
        }
    }

    void ParseMessages()
    {
        ReceivedUnit dequeued;
        int count = 0;
        // パース可能回数が残っていて、かつ受信メッセージがある場合
        while (count < MaxParsePerFrame && messageQueue.TryDequeue(out dequeued))
        {
            // メッセージの中身を解読
            Parse(dequeued);
            count++;// パース回数を増加
        }
    }

    ///// <summary>
    ///// パース用のスレッド
    ///// </summary>
    //void ThreadParse()
    //{
    //    ReceivedUnit dequeued;

    //    while (isParsing)
    //    {
    //        // 受信メッセージがある場合
    //        while (messageQueue.TryDequeue(out dequeued))
    //        {
    //            // メッセージの中身を解読（現在デバッグのためコメントアウト）
    //            Parse(dequeued);
    //        }

    //        // CPUの負荷対策
    //        Thread.Sleep(ThreadSleepMillisecond);
    //    }
    //    Debug.LogWarning("パーススレッド終了");
    //}

    void RequestReconnection(string ip, int port)
    {
        // sendThreadを止め、古いclientを使わないように
        isSending = false;
        if (sendThread != null && sendThread.IsAlive) sendThread.Join();// スレッドの終了を待機

        // 受信を停止
        isReceiving = false;
        client?.Close();// Receiveがブロックしてるならここで例外を出してループを抜ける
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join();// スレッドの終了を待機

        // ここで新しいソケットとスレッドを作成
        client = new UdpClient(new IPEndPoint(IPAddress.Any, myInfo.Port));
        client.Client.ReceiveBufferSize = RecieveBufferSize;

        // 受信を再開
        isReceiving = true;
        receiveThread = new Thread(new ThreadStart(ThreadReceive));
        receiveThread.Start();// 受信スレッド開始

        // 必要に応じて送信を再開
        RegisterOpponentPort(ip, port);
        if (!isSending) SendThreadStart();

        // デバッグ
        Debug.Log("再接続を要求");
        Debug.LogError($"[RECONNECT] Triggered at {DateTime.Now:HH:mm:ss.fff}");
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
        client.Send(message, message.Length, opponentEP);// メッセージを送信
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

    public void OnRegister()// ボタンを押したとき
    {
        foreach (ClientInfo client in clients)
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
        try
        {
            while (isReceiving)
            {
                IPEndPoint senderEP = null;
                try// 情報を受け取れないときに切断されないようにしている
                {
                    byte[] receivedBytes = client.Receive(ref senderEP);
                    Debug.Log($"受信成功 bytes={receivedBytes.Length}");// デバッグ

                    // メッセージの長さチェック
                    if (receivedBytes != null && receivedBytes.Length >= sizeof(Int32))
                    {
                        // メッセージの種類を判別
                        UDPMessageType type = receivedBytes.ToUDPMessageType();

                        // 接続生存確認のメッセージやホストからのメッセージのとき
                        if (type == UDPMessageType.ConnectCheck || type == UDPMessageType.HostMessage)
                        {
                            // ClientInfoがなくてもキューに追加する
                            ReceivedUnit ConnectCheckUnit = new ReceivedUnit(senderEP, receivedBytes, new ClientInfo(senderEP.Address.ToString(), senderEP.Port));
                            messageQueue.Enqueue(ConnectCheckUnit);
                            Debug.Log("Enqueue成功 Info=" + (ConnectCheckUnit.Info?.IP ?? "null"));// デバッグ
                            Debug.Log("CheckConnect呼ぶ");
                            CheckConnect(ConnectCheckUnit);// 接続状態の更新
                            Debug.Log("CheckConnect呼んだ");
                            continue;
                        }

                        ClientInfo foundInfo = null;// メッセージで受信できたClientInfoを入れる
                        try
                        {
                            // 接続時
                            // ClientInfoを取得
                            foundInfo = SearchClientInfo(receivedBytes.ToClientInfo(sizeof(Int32)));// UDPMessage型のメッセージの先にあるJsonファイルから、ClientInfoを取得する

                            //Debug.Log($"受け取ったメッセージ長: {receivedBytes.Length}");
                            //messageStack.Add(new ReceivedUnit(senderEP, receivedBytes, clientInfo));
                        }
                        catch
                        {
                            // Jsonのパースか変換でエラーが起きたとき、無視して後で処理する
                            foundInfo = null;
                            //Debug.LogWarning($"Jsonのパース失敗 {System.Text.Encoding.UTF8.GetString(receivedBytes)}");
                        }
                        //catch
                        //{
                        //    // 通信時
                        //    string objectInfoJson = System.Text.Encoding.UTF8.GetString(receivedBytes, sizeof(Int32), receivedBytes.Length - sizeof(Int32));// UDPMessage型のメッセージの先
                        //    ObjectInfo objectInfo = JsonUtility.FromJson<ObjectInfo>(objectInfoJson);

                        //    Debug.Log($"ポート：{objectInfo.ClientInfo.Port}, 受け取った位置：{objectInfo.Position}");
                        //    messageStack.Add(new ReceivedUnit(senderEP, receivedBytes, objectInfo.ClientInfo));
                        //}

                        if (foundInfo == null)
                        {
                            // 通信時
                            string objectInfoJson = System.Text.Encoding.UTF8.GetString(receivedBytes, sizeof(Int32), receivedBytes.Length - sizeof(Int32));// UDPMessage型のメッセージの先
                                                                                                                                                            //Debug.Log($"[RAW MESSAGE] {objectInfoJson}");
                            ObjectInfo objectInfo = JsonUtility.FromJson<ObjectInfo>(objectInfoJson);// ObjectInfoを取得

                            // 同期するオブジェクトの情報が不完全なときはキューに追加しない
                            if (objectInfo == null || objectInfo.ClientInfo == null)
                            {
                                //Debug.LogWarning("ObjectInfoのパース失敗" + objectInfoJson);
                                continue;
                            }

                            foundInfo = objectInfo.ClientInfo;// ClientInfoを設定
                        }

                        // メッセージをキューに追加
                        ReceivedUnit unit = new ReceivedUnit(senderEP, receivedBytes, foundInfo);
                        messageQueue.Enqueue(unit);
                        Debug.Log("Enqueue成功 Info=" + (unit.Info?.IP ?? "null"));// デバッグ

                        // 接続している状況の更新
                        Debug.Log("CheckConnect呼ぶ");
                        CheckConnect(unit);
                        Debug.Log("CheckConnect呼んだ");

                        //Debug.Log($"[RECEIVE] {DateTime.Now:HH:mm:ss.fff} bytes={receivedBytes.Length} from={senderEP}");// デバッグ
                    }
                }
                catch (SocketException sockerException)
                {
                    if (!isReceiving) break;// clientが閉じられていたら抜ける
                                            //Debug.LogError($"Socket Exception:{sockerException.Message}");
                }
                catch (Exception exception)
                {

                }

            }
        }
        catch (Exception exception)
        {
            Debug.LogError("ThreadReceive Exception" + exception);
        }

        finally
        {
            Debug.LogWarning("ThreadReceive Ended");
        }
        //Debug.LogWarning("受信スレッド終了");// デバッグ
    }

    /// <summary>
    /// オブジェクトのアタッチされたクライアント情報と結びつける
    /// </summary>
    ClientInfo SearchClientInfo(ClientInfo info)
    {
        for (int i = 0; i < clients.Count; i++)
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
        // mainのthread以外でTime.deltaTimeを使用することができないため、.NET標準の時間クラスを使用
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();// ストップウォッチ開始
        long last = GameConstants.Zero;

        while (isSending)
        {
            //OnUpdateSend();

            long now = stopwatch.ElapsedMilliseconds;// ストップウォッチ開始からの経過時間を取得
            // 送信タイミングになったとき
            if (now - last >= SendInterval)
            {
                isSendTiming = true;
                last = now;// タイマーの初期化
            }
            Thread.Sleep(ThreadSleepMillisecond);// CPUの食いすぎを防止する

            // Thread.Sleepではネットワークに応答なしと判断される可能性があったため変更している
            //Thread.Sleep(1000 / sendPerSecond);
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

        // 接続時に使用する変数の設定
        int answerWaitRegisterIndex = GameConstants.DefaultIndex;    // 接続待ちリストの中のインデックス
        int connectedIndex = GameConstants.DefaultIndex;             // 接続済みの中のインデックス
        if (type == UDPMessageType.AnswerWait || type == UDPMessageType.Answered)
        {
            answerWaitRegisterIndex = answerWaiting.IndexOfPort(unit.SenderEP.Port);
            connectedIndex = connectedPlayerInfos.IndexOfPort(unit.SenderEP.Port);
        }

        //Debug.Log("メッセージを受信");
        switch (type)
        {
            case UDPMessageType.AnswerWait:
                {
                    Debug.Log(answerWaitRegisterIndex);
                    if (answerWaitRegisterIndex == GameConstants.DefaultIndex) break;// 応答待ちリストに存在しなければ処理を行わない

                    connectedPlayerInfos.Add(unit.Info);// 接続済みプレイヤーのリストに追加
                    answerWaiting.RemoveAt(answerWaitRegisterIndex);// 応答待ちリストから削除

                    // 初めてプレイヤーと接続したときのみ送信スレッドを開始させる
                    if (!isSending && connectedPlayerInfos.Count == 1)
                    {
                        SendThreadStart();
                    }
                    Debug.Log("他の人から接続がありました:" + unit.Info.Port);

                    // 相手に応答したことを返す
                    // 送信するメッセージの作成
                    byte[] udpMessage = UDPMessageType.Answered.ToByte();
                    byte[] infoMessage = myInfo.ToByte();
                    byte[] message = MergeBytes(udpMessage, infoMessage);// 合成

                    client.SendAsync(message, message.Length, unit.SenderEP);// 送信

                    ActivateTrackObject(unit);
                    CheckConnect(unit);
                    break;
                }
            case UDPMessageType.Answered:
                {
                    print(answerWaitRegisterIndex);
                    if (answerWaitRegisterIndex == -1) break;// 応答待ちリストに存在しなければ処理を行わない

                    connectedPlayerInfos.Add(unit.Info);// 接続済みプレイヤーのリストに追加
                    answerWaiting.RemoveAt(answerWaitRegisterIndex);// 応答待ちリストから削除

                    // 初めてプレイヤーと接続したときのみ送信スレッドを開始させる
                    if (!isSending && connectedPlayerInfos.Count == 1)
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
                    // メッセージからJson形式に直し、位置情報のクラスに変換する
                    string objectInfoJson = System.Text.Encoding.UTF8.GetString(unit.Message, sizeof(Int32), unit.Message.Length - sizeof(Int32));// UDPMessage型のメッセージの先
                    ObjectInfo playerObjectInfo = JsonUtility.FromJson<ObjectInfo>(objectInfoJson);// Json形式からObjectInfo型に変換

                    playerObjectInfo.SetClientInfo(SearchClientInfo(playerObjectInfo.ClientInfo));// 動かすオブジェクトと結びつけたものに直す
                    otherPlayerObjectInfo.Add(playerObjectInfo);// 動かすリストに追加
                    //Debug.Log("位置を受信：" + unit.Info.Port);
                    break;
                }
            case UDPMessageType.ConnectCheck:
                {
                    // 接続しているかの確認のみなので何もしない
                    break;
                }
            case UDPMessageType.HostMessage:
                {
                    string dtoJson = System.Text.Encoding.UTF8.GetString(unit.Message, sizeof(Int32), unit.Message.Length - sizeof(Int32));// UDPMessage型のメッセージの先
                    HostMessageDto receiveDto = JsonUtility.FromJson<HostMessageDto>(dtoJson);// Json形式からSystemManagerに変換

                    // 残弾数や強化状態を反映
                    foreach(SystemManager.Team team in receiveDto.HostSystemManager.Teams)
                    {
                        // 自身の色についての情報だった場合
                        if(myInfo.Cursor.Team.Color == team.Color)
                        {
                            myInfo.Cursor.Team.SetBulletCount(team.BulletCount);// 残弾数を同期
                            myInfo.Cursor.SetModeFlag(receiveDto.CanModes);// 強化の使用可能状況を同期
                            break;
                        }
                    }
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
        //Debug.Log("[CheckConnect] 呼び出し");
        // connectedPlayerInfos内に渡されたunitに当たるプレイヤーがいるか調べる（いなければnull）
        ClientInfo existing = connectedPlayerInfos.FirstOrDefault(player => player.IP == unit.Info.IP && player.Port == unit.Info.Port);

        // 見つかった場合
        if (existing != null)
        {
            //Debug.Log("[CheckConnect] 発見" + existing.IP);
            existing.ResetDiscconectTimer();// 切断判定までのタイマーをリセット
        }
        //else Debug.Log("[CheckConnect] 発見不可");
    }

    /// <summary>
    /// 通信相手全員に自分の状態を送る
    /// </summary>
    void BroadcastStatus()
    {
        // 送信処理
        // (これから自分の番かの判定を追加予定)
        if (myInfo.TrackObject != null) SendInfo();  // 情報を送る
        else if (systemManager.IsStarted) SendHostMessage();// ホストの情報を送る
        else SendOnlyConnection();                      // 接続しているかどうかの情報のみを送る
    }

    /// <summary>
    /// 情報の送信を行う
    /// </summary>
    void SendInfo()
    {
        byte[] udpMessage = UDPMessageType.PositionUpdate.ToByte();// 位置情報送信モード

        // 位置情報のクラスからJson形式に変換し、メッセージにする
        ObjectInfo myObjectInfo = new ObjectInfo(myInfo, myInfo.TrackObject.transform.position, myInfo.TrackObject.transform.eulerAngles.y);
        myObjectInfo.OnSend();// 送信前に受診後に使いたい情報を保存しておく
        string myObjectInfoJson = JsonUtility.ToJson(myObjectInfo);

        byte[] myObjectInfoMessage = System.Text.Encoding.UTF8.GetBytes(myObjectInfoJson);// StringをByte配列に変換

        byte[] posMessage = MergeBytes(udpMessage, myObjectInfoMessage);// メッセージの結合

        //Debug.Log("Send Size:" + posMessage.Length);
        // メッセージの送信
        SendAsyncToPlayers(posMessage);
    }

    void SendHostMessage()
    {
        byte[] udpMessage = UDPMessageType.HostMessage.ToByte();// ホストメッセージモード

        foreach(ClientInfo clientInfo in connectedPlayerInfos)
        {
            //string systemManagerJson = JsonUtility.ToJson(systemManager);

            //byte[] systemManagerMessage = System.Text.Encoding.UTF8.GetBytes(systemManagerJson);
            List<CursorInfo.Mode> canModes = clientInfo.Cursor.CanModes;

            HostMessageDto hostMessageDto = new HostMessageDto(systemManager, canModes);
            string hostMessageDtoJson = JsonUtility.ToJson(hostMessageDto);
            byte[] dtoMessage = System.Text.Encoding.UTF8.GetBytes(hostMessageDtoJson);// Jsonに変換
            HostMessageDto debugDto = JsonUtility.FromJson<HostMessageDto>(hostMessageDtoJson);
            byte[] hostMessage = MergeBytes(udpMessage, dtoMessage);

            SendAsyncToPlayers(hostMessage);
        }
    }

    void SendAsyncToPlayer(byte[] message, IPEndPoint endPoint)
    {
        try
        {
            client.SendAsync(message, message.Length, endPoint);
        }
        catch (SocketException e)
        {
            Debug.LogException(e);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// 接続状況の送信を行う
    /// </summary>
    void SendOnlyConnection()
    {
        byte[] udpMessage = UDPMessageType.ConnectCheck.ToByte();// 接続状況送信モード

        // メッセージの送信
        SendAsyncToPlayers(udpMessage);
    }

    /// <summary>
    /// つながっているプレイヤー全員にメッセージを送信する
    /// </summary>
    void SendAsyncToPlayers(byte[] message)
    {
        foreach (ClientInfo clientInfo in connectedPlayerInfos)
        {
            try
            {
                client.SendAsync(message, message.Length, clientInfo.EndPoint);
                Debug.Log($"[SEND] {DateTime.Now:HH:mm:ss.fff} to={clientInfo.EndPoint}");// デバッグ

            }
            catch (ObjectDisposedException)
            {
                // clientが閉じられたタイミングでは何も行わない
            }
            catch (SocketException e)
            {
                Debug.LogError(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    void SendThreadStart()
    {
        if (sendThread != null && sendThread.IsAlive) return;// 生きているsendThreadがあるなら更に開始はしない
        isSending = true;
        sendThread = new Thread(new ThreadStart(ThreadSend));
        sendThread.Start();
    }

    void ActivateTrackObject(ReceivedUnit unit)
    {
        if (unit.Info == null || unit.Info.TrackObject == null) return;// Infoがnullであったり、動かす対象が登録されていない（ピザの画面を映すPC）場合は動かさない

        for (int i = 0; i < connectedPlayerInfos.Count; i++)
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
        const int ThreadMilliSecondsTimeOut = 500;

        // 送信スレッド
        isSending = false;
        if (sendThread != null && sendThread.IsAlive) sendThread.Join(ThreadMilliSecondsTimeOut);// 指定された時間が経過するまで呼び出し元のスレッドをブロック

        // 受信スレッド
        isReceiving = false;
        client?.Close();// UDP接続を終了
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(ThreadMilliSecondsTimeOut);// 指定された時間が経過するまで呼び出し元のスレッドをブロック

        client?.Dispose();// リソースを開放
    }
}

enum UDPMessageType
{
    AnswerWait = 100001,
    Answered,
    PositionUpdate,
    ConnectCheck,
    HostMessage,
}

/// <summary>
/// 通信のための変換等を行う
/// </summary>
static class MultiPlayerMessenger
{
    // バイト配列への変換
    public static byte[] ToByte(this UDPMessageType udpMessage)
    {
        return BitConverter.GetBytes((int)udpMessage);
    }
    public static byte[] ToByte(this UDPMulti.ClientInfo clientInfo)
    {
        string infoJson = JsonUtility.ToJson(clientInfo);// Json形式に変更
        return System.Text.Encoding.UTF8.GetBytes(infoJson);
    }
    public static byte[] ToByte(this Vector3 vector3)
    {
        byte[] x = BitConverter.GetBytes(vector3.x);
        byte[] y = BitConverter.GetBytes(vector3.y);
        byte[] z = BitConverter.GetBytes(vector3.z);
        return x.Concat(y).Concat(z).ToArray();// 連結
    }
    public static byte[] ToByte(this TeamColor teamColor)
    {
        return BitConverter.GetBytes((int) teamColor);
    }

    // バイト配列からの変換
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
    public static TeamColor ToTeamColor(this byte[] bytes, int startIndex = 0)
    {
        int number = BitConverter.ToInt32(bytes, startIndex);
        return (TeamColor)number;
    }
    public static Vector3 ToVector3(this byte[] bytes, int startIndex)
    {
        float x = BitConverter.ToSingle(bytes, startIndex);
        float y = BitConverter.ToSingle(bytes, startIndex + sizeof(int));// int型のサイズを1つ分ずらしている
        float z = BitConverter.ToSingle(bytes, startIndex + sizeof(int) + sizeof(int));// 2つ分ずらしている
        return new Vector3(x, y, z);
    }

    // ポート番号と一致するリストの番号を検索
    public static int IndexOfPort(this List<UDPMulti.ClientInfo> endPoints, int targetPort)
    {
        int index = GameConstants.DefaultIndex;// 合うポートが見つからなければ初期値(-1)を返すように
        for (int i = 0; i < endPoints.Count; i++)
        {
            if (endPoints[i].Port == targetPort) index = i;// 合うポートの番号を設定
        }
        return index;
    }
    public static int IndexOfPort(this List<IPEndPoint> endPoints, int targetPort)
    {
        int index = GameConstants.DefaultIndex;// 合うポートが見つからなければ初期値（-1）を返すように
        for (int i = 0; i < endPoints.Count; i++)
        {
            if (endPoints[i].Port == targetPort) index = i;// 合うポートの番号を設定
        }
        return index;
    }
}
