///////
///参考サイト
///「UNITY上でUDP/IPを用いた通信機能（マルチプレイ）を実装する」
///https://younaship.com/2018/12/31/unity%E4%B8%8A%E3%81%A7websocket%E3%82%92%E7%94%A8%E3%81%84%E3%81%9F%E9%80%9A%E4%BF%A1%E6%A9%9F%E8%83%BD%E3%83%9E%E3%83%AB%E3%83%81%E3%83%97%E3%83%AC%E3%82%A4%E3%82%92%E5%AE%9F%E8%A3%85%E3%81%99/
///////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;// BitConverter

namespace Test
{
    /// <summary>
    /// UDPでの通信で送るデータのテスト（今回は位置情報のVector3）
    /// </summary>
    public class Data
    {
        float x;
        float y;
        float z;

        // コンストラクタ（byteを使用する場合・12Bytes想定）
        public Data(byte[] bytes)
        {
            x = BitConverter.ToSingle(bytes, 0);// 0byte目から(１つめのデータ)
            y = BitConverter.ToSingle(bytes, 4);// 4byte目から（２つめのデータ）
            z = BitConverter.ToSingle(bytes, 8);// 8byte目から（３つめのデータ）
        }

        // コンストラクタ（Vector3を使用する場合）
        public Data(Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }

        /// <summary>
        /// 位置（x,y,z）を12bytesに変換
        /// </summary>
        /// <returns>位置情報(x,y,z float型なので各4bytes)</returns>
        public byte[] ToByte()
        {
            // 指定した形式の情報をbyte配列で返す
            byte[] x = BitConverter.GetBytes(this.x);
            byte[] y = BitConverter.GetBytes(this.y);
            byte[] z = BitConverter.GetBytes(this.z);

            return MargeByte(MargeByte(x, y), z);
        }

        /// <summary>
        /// データをVector3型にする
        /// </summary>
        public Vector3 ToVector3()
        {
            return new Vector3(this.x, this.y, this.z);
        }

        /// <summary>
        /// byteを合成
        /// </summary>
        /// <param name="baseByte">元となる（先頭の方になる）側のbyte</param>
        /// <param name="addByte">追加される（後の方に足される）側のbyte</param>
        /// <returns>合成されたbyte</returns>
        public static byte[] MargeByte(byte[] baseByte, byte[] addByte)
        {
            byte[] resultByte = new byte[baseByte.Length + addByte.Length];// 合成後の長さ分を取っておく
            for (int i = 0; i < resultByte.Length; i++)
            {
                // 元となるbyteのコピー
                if (i < baseByte.Length) resultByte[i] = baseByte[i];

                // 追加されるbyteのコピー
                else resultByte[i] = addByte[i - baseByte.Length];
            }
            return resultByte;
        }
    }
}
