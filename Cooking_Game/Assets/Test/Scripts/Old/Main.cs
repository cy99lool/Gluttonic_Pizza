using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net;// DNS

namespace Test
{
    public class Main : MonoBehaviour
    {
        [Header("動かすオブジェクト"), SerializeField] GameObject gameObject;
        [Header("座標表示テキスト"), SerializeField] TextMeshProUGUI posText;
        [Header("IPアドレス"), SerializeField] TextMeshProUGUI ipText;
        [Header("接続確認テキスト"), SerializeField] TextMeshProUGUI connectCheckText;
        [SerializeField]GameObject hostIpInput, clientIpInput;

        //string hostIpAddress = "192.168.116.73";// ホスト側のIPアドレス
        //string clientIpAddress = "192.168.116.72";// クライアント側IPアドレス

        Vector3 vector3 = Vector3.zero;
        UDPSystem udpSystem;
        char device = 'A'; // ホスト側動作はA、クライアント側動作はB

        void Awake()
        {
            //Connect();
            ipText.text += GetLocalIPAddress();
        }

        void Start()
        {
            udpSystem = new UDPSystem(null);
        }

        void Update()
        {
            if (!udpSystem.SettingDone) return;// 設定されていない場合は処理を行わない
            if(device == 'A')
            {
                ipText.text = "現在のモード：ホスト\nIPアドレス：";

                vector3 = gameObject.transform.position;    // 動かすオブジェクトの位置を取得
                Data sendData = new Data(vector3);          // 送信用データを用意
                udpSystem.Send(sendData.ToByte(), 99);      // 送信
            }
            if(device == 'B')
            {
                ipText.text = "現在のモード：クライアント\nIPアドレス：";

                gameObject.transform.position = vector3;
            }
            posText.text = "(" + vector3.x + "," + vector3.y + "," + vector3.z + ")";
        }

        /// <summary>
        /// データの受信
        /// </summary>
        void Receive(byte[] bytes)
        {
            Data getData = new Data(bytes); // データの受信
            vector3 = getData.ToVector3();  // Vector3に変換

            if(vector3 == null)
            {
                connectCheckText.text = "接続していません";
                return;
            }
            connectCheckText.text = "接続中";
        }

        /// <summary>
        /// IPv4アドレスを取得
        /// </summary>
        string GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());// ホスト名を取得
            foreach(IPAddress ip in host.AddressList)
            {
                if(ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)// IPv4の場合
                {
                    return ip.ToString();
                }
            }
            // IPv4が無かったら
            throw new System.Exception("システムにIPv4アドレスのネットワークアダプターがありません！");
        }

        void Connect(GameObject hostInputObj, GameObject clientInputObj)
        {
            if(hostInputObj == null || clientInputObj == null)
            {
                Debug.LogError("Not Enough Attachment!");
                return;
            }
            if(!hostInputObj.TryGetComponent<TMP_InputField>(out TMP_InputField host) || !clientInputObj.TryGetComponent<TMP_InputField>(out TMP_InputField client))
            {
                Debug.LogError("TMP_InputField has not been attached!");
                return;
            }

            // 下記の方法で実行確認できたら削除予定
            //string hostIpAddress = host != null ? host.text : "192.168.116.73";
            //string clientIpAddress = client != null ? client.text : "192.168.116.72";

            string hostIpAddress = host.text;
            string clientIpAddress = client.text;

            switch (device)
            {
                case 'A':
                    udpSystem.Set(hostIpAddress, 5001, clientIpAddress, 5002);
                    break;
                case 'B':
                    udpSystem = new UDPSystem((x) => Receive(x));
                    udpSystem.Set(clientIpAddress, 5002, hostIpAddress, 5001);
                    udpSystem.Receive();
                    break;
            }
        }

        public void ConnectButton()
        {
            Connect(hostIpInput, clientIpInput);
        }
    }
}
