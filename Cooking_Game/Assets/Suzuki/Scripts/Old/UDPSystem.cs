using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;// UdpClient
using System.Net;// IPEndPoint等
using System.Timers;// Timerクラス


namespace Test
{
    /* UDPSystem.cs - UDPを用いたネットワーク通信プログラム
     * 
     * 参考元 https://younaship.com/2018/12/31/unity%E4%B8%8A%E3%81%A7websocket%E3%82%92%E7%94%A8%E3%81%84%E3%81%9F%E9%80%9A%E4%BF%A1%E6%A9%9F%E8%83%BD%E3%83%9E%E3%83%AB%E3%83%81%E3%83%97%E3%83%AC%E3%82%A4%E3%82%92%E5%AE%9F%E8%A3%85%E3%81%99/
     */
    public class UDPSystem
    {
        class IPandPort
        {
            /// <summary>
            /// コンストラクタ　アドレスとポート設定
            /// </summary>
            /// <param name="ipAddress">IPアドレス</param>
            /// <param name="port">ポート番号</param>
            IPandPort(string ipAddress, int port)
            {
                this.ipAddress = ipAddress;
                this.port = port;
            }
            string ipAddress;
            int port;
            UdpClient udpclient;
        }

        readonly int RETRY_SEND_TIME = 10;// ms

        static byte sendTaskCount = 0;// 送信されたタスクの量
        static List<IPandPort> recList = new List<IPandPort>();

        bool finishFlag = false;
        bool onlyFlag = false;

        int sendHostPort = 6001;
        int sendHostPortRange = 0;

        Action<byte[]> callBack;

        string recIP, sendIP;
        int recPort = 5000, sendPort = 5000;// ポートの設定

        UdpClient udpClientSend;
        UdpClient tmpReceiver;// 受信終了用TMP

        public bool SettingDone { get { return recIP != null && sendIP != null; } }

        /*----- コンストラクタ部 -----*/

        /// <summary>
        /// コンストラクタ コールバックを設定
        /// </summary>
        /// <param name="callBack">コールバック、受信時に発生するため、送信用に生成するときはnullでOK</param>
        public UDPSystem(Action<byte[]> callBack)
        {
            this.callBack = callBack;
        }

        /// <summary>
        /// コンストラクタ オーバーロード
        /// </summary>
        /// <param name="recIP">受信側のIP</param>
        /// <param name="recPort">受信側のポート</param>
        /// <param name="sendIP">送信側のIP</param>
        /// <param name="sendPort">送信側のポート</param>
        public UDPSystem(string recIP, int recPort, string sendIP, int sendPort, Action<byte[]> callBack, bool onlyFlag = false)
        {
            /* rec,send IP == null -> AnyIP */

            // IP設定
            this.recIP = recIP;
            this.sendIP = sendIP;
            // ポート設定
            this.recPort = recPort;
            this.sendPort = sendPort;

            this.callBack = callBack;
            this.onlyFlag = onlyFlag;
        }
        /*----- コンストラクタ部ここまで -----*/
        public void Set(string recIP, int recPort, string sendIP, int sendPort, Action<byte[]> callBack = null)
        {
            // IP設定
            this.recIP = recIP;
            this.sendIP = sendIP;
            // ポート設定
            this.recPort = recPort;
            this.sendPort = sendPort;

            if (callBack != null) this.callBack = callBack;// 指定されているときだけ設定する
        }
        /// <summary>
        /// 送信用 自己ポート設定
        /// </summary>
        public void SetSendHostPort(int port, int portRange = 0)
        {
            sendHostPort = port;
            sendHostPortRange = portRange;
        }

        int SendHostPort
        {
            get
            {
                // ポートの範囲が無いとき（単一のポートのみを返すとき）
                if (sendHostPortRange == 0) return sendHostPort;

                // ポートの範囲が設定されているとき
                return UnityEngine.Random.Range(sendHostPort, sendHostPort + 1);
            }
        }
        public void Finish()// エラー時チェック項目：Close()が2度目ではないか
        {
            if (tmpReceiver != null) tmpReceiver.Close();
            else finishFlag = true;
        }
        /// <summary>
        /// ポートの監視を開始
        /// </summary>
        public void Receive()
        {
            string targetIP = recIP;// 受信
            int port = recPort;

            //if (recList.Contains(new IPandPort())) ;

            UdpClient udpClientReceive;

            if (targetIP == null) udpClientReceive = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            else if (targetIP == "") udpClientReceive = new UdpClient(new IPEndPoint(IPAddress.Parse(ScanIPAddress.IP[0]), port));
            else udpClientReceive = new UdpClient(new IPEndPoint(IPAddress.Parse(targetIP), port));

            udpClientReceive.BeginReceive(UDPReceive, udpClientReceive);

            if (targetIP == null) Debug.Log("受信を開始しました。 Any" + IPAddress.Any + " " + port);
            else if (targetIP == "") Debug.Log("受信を開始しました。 Me" + ScanIPAddress.IP[0] + " " + port);
            else Debug.Log("受信を開始しました。" + IPAddress.Parse(targetIP) + " " + port);

            tmpReceiver = udpClientReceive;
        }
        /// <summary>
        /// ポートに着信があると呼ばれる
        /// </summary>
        void UDPReceive(IAsyncResult result)
        {
            if(finishFlag)
            {
                FinishUDP(result.AsyncState as UdpClient);
                return;
            }

            UdpClient getUdp = (UdpClient)result.AsyncState;
            IPEndPoint ipEnd = null;
            byte[] getByte;

            try
            {
                // 受信成功時アクション
                getByte = getUdp.EndReceive(result, ref ipEnd);
                if (callBack != null) callBack(getByte);
            }
            catch(SocketException exception)
            {
                Debug.Log("Error" + exception);
                return;
            }
            catch(ObjectDisposedException)// 破棄されたオブジェクトに操作がされたとき
            {
                Debug.Log("Socket has already closed.");
                return;
            }

            if(finishFlag || onlyFlag)
            {
                FinishUDP(getUdp);
                return;
            }

            Debug.Log("Retry");
            getUdp.BeginReceive(UDPReceive, getUdp);// Retry
        }

        void FinishUDP(UdpClient udp)
        {
            udp.Close();
        }

        /// <summary>
        /// 同時送信を行う（未検証）
        /// </summary>
        public void Send_NonAsync(byte[] sendByte)
        {
            if (udpClientSend == null) udpClientSend = new UdpClient(new IPEndPoint(IPAddress.Parse(ScanIPAddress.IP[0]), sendHostPort));
            udpClientSend.EnableBroadcast = true;

            try
            {
                udpClientSend.Send(sendByte, sendByte.Length, sendIP, sendPort);
            }
            catch(Exception exception)
            {
                Debug.LogError(exception.ToString());
            }
        }

        /// <summary>
        /// 同時送信を行う(検証済)
        /// </summary>
        public void Send_NonAsync2(byte[] sendByte)
        {
            string targetIP = sendIP;
            int port = sendPort;

            if (udpClientSend == null) udpClientSend = new UdpClient(new IPEndPoint(IPAddress.Parse(ScanIPAddress.IP[0]), sendHostPort));

            udpClientSend.EnableBroadcast = true;
            Socket uSocket = udpClientSend.Client;
            uSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

            if(targetIP == null)
            {
                udpClientSend.Send(sendByte, sendByte.Length, new IPEndPoint(IPAddress.Broadcast, sendPort));
                Debug.Log("送信処理しました。" + ScanIPAddress.IP[0] + " > BroadCast " + IPAddress.Broadcast + ":" + sendPort);
            }
            else
            {
                udpClientSend.Send(sendByte, sendByte.Length, new IPEndPoint(IPAddress.Parse(targetIP), sendPort));
                Debug.Log("送信しました。" + ScanIPAddress.IP[0] + " > " + IPAddress.Parse(targetIP) + ":" + sendPort);
            }
        }
        /// <summary>
        /// 非同期通信をUdpClientで開始する（通常）<retry>
        /// </summary>
        public void Send(byte[] sendByte, byte retryCount = 0)
        {
            string targetIP = sendIP;
            int port = sendPort;

            if(sendTaskCount > 0)// 送信中タスクの確認。送信中のものがあった場合、一定時間後リトライ
            {
                Debug.Log("SendTask is There.[" + retryCount);
                retryCount++;

                if(retryCount > 10)// あまりにもリトライされすぎた場合
                {
                    Debug.LogError("Retry OverFlow.");
                    return;
                }

                Timer timer = new Timer(RETRY_SEND_TIME);// リトライまでの時間を設定
                timer.Elapsed += delegate(object obj, ElapsedEventArgs e) { Send(sendByte, retryCount); timer.Stop(); };
                timer.Start();
                return;
            }
            sendTaskCount++;// 送信中タスクを増加

            if(udpClientSend == null)
            {
                udpClientSend = new UdpClient(new IPEndPoint(IPAddress.Parse(ScanIPAddress.IP[0]), SendHostPort));
            }
            if(targetIP == null)
            {
                udpClientSend.BeginSend(sendByte, sendByte.Length, new IPEndPoint(IPAddress.Broadcast, sendPort), UDPSender, udpClientSend);
                Debug.Log("送信処理しました。" + ScanIPAddress.IP[0] + " > BroadCast " + IPAddress.Broadcast + ":" + sendPort);
            }
            else
            {
                udpClientSend.BeginSend(sendByte, sendByte.Length, sendIP, sendPort, UDPSender, udpClientSend);
                Debug.Log("送信処理しました。" + ScanIPAddress.IP[0] + " > " + IPAddress.Parse(targetIP) + ":" + sendPort + "[" + sendByte[0] + "][" + sendByte[1] + "]...");
            }
        }
        void UDPSender(IAsyncResult result)
        {
            UdpClient udp = (UdpClient)result.AsyncState;

            try
            {
                udp.EndSend(result);
                Debug.Log("Send");
            }
            catch(SocketException exception)
            {
                Debug.Log("Error" + exception);
                return;
            }
            catch(ObjectDisposedException)// Finish : Socket Closed
            {
                Debug.Log("Socket has already closed.");
                return;
            }

            sendTaskCount--;
            udp.Close();
        }
        public class ScanIPAddress
        {
            /* 
                ------------------------------------------------------------------------------------------------------------------------
                    スマートフォンの場合はキャリア回線とWifi上のIPアドレスが両方存在するため、IP[n]のnをしっかり設定すること！
                    pcは0で良い
                ------------------------------------------------------------------------------------------------------------------------
             */
            public static string[] IP { get { return Get(); } }
            public static byte[][] ByteIP { get { return GetByte(); } }

            /// <summary>
            /// IPアドレス(IPv4)の取得
            /// </summary>
            /// <returns>IPv4のIPアドレス配列</returns>
            public static string[] Get()
            {
                IPAddress[] addressArray = Dns.GetHostAddresses(Dns.GetHostName());// アドレス達を取得
                List<string> list = new List<string>();
                foreach(IPAddress address in addressArray)
                {
                    if(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)// IPv4か確認
                    {
                        list.Add(address.ToString());
                    }
                }
                if(list.Count == 0) return null;
                return list.ToArray();
            }
            /// <summary>
            /// IPアドレス(IPv4)のバイトを取得
            /// </summary>
            /// <returns>IPv4のIPアドレスのバイト配列</returns>
            public static byte[][] GetByte()
            {
                IPAddress[] addressArray = Dns.GetHostAddresses(Dns.GetHostName());// アドレス達を取得
                List<byte[]> list = new List<byte[]>();
                foreach(IPAddress address in addressArray)
                {
                    if(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)// IPv4か確認
                    {
                        list.Add(address.GetAddressBytes());
                    }
                }
                if (list.Count == 0) return null;
                return list.ToArray();
            }
        }
    }
}
