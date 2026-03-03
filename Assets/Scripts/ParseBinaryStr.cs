using UnityEngine;
using System.Text;
using System.Collections.Generic;

public class ParseBinaryStr : MonoBehaviour
{
    void Start()
    {
        byte[] testBytes = {
            0,100,
            102,47,
            101,135,
            103,44,
            0,13
        };

        getCHStr(testBytes);
    }

    private string getCHStr(byte[] data, int startIndex = 0)
    {
        List<byte> bytelist = new List<byte>();
        for (int index = startIndex; index < data.Length;)
        {
            if (index >= data.Length)
                break;

            byte byte1 = data[index++];
            if (byte1 == 0) //byte=0是ASCII码表中的空字符
            {
                byte byte2 = data[index++];
                if (byte2 != 0) //高地位都为0的话就真的 没有字符
                {
                    bytelist.Add(byte2);
                    bytelist.Add(0);
                }
                else
                {
                    break;
                }
            }
            else
            {
                byte byte2 = data[index++];

                if (byte2 != 0)
                {
                    if (byte1 != 0 && byte2 != 0 && !isChinese(byte2, byte1))
                    {
                        break;
                    }
                    else
                    {
                        bytelist.Add(byte2);
                        bytelist.Add(byte1);
                    }
                }
                else
                {
                    break;
                }
            }
        }
        byte[] res = new byte[bytelist.Count];
        for (int index = 0; index < bytelist.Count; index++)
            res[index] = bytelist[index];

        string str = new string(Encoding.Unicode.GetChars(res));

        Debug.Log("截取字符串=" + str);

        return str;
    }

    //中文字符的区间"[\u4E00-\u9FBF]"
    private bool isChinese(byte item1, byte item2)
    {
        return item1 >= 0x00 && item1 <= 0xBF &&
            item2 >= 0x4e && item2 <= 0x9f;
    }
}