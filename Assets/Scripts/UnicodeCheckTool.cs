using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;

public class UnicodeCheckTool : MonoBehaviour {

    public static Regex EN_REG = new Regex(@"[a-zA-Z0-9]"); 

    // Use this for initialization
    void Start()
    {
        Debug.Log(Time.time + "，检查byte数组是否为Unicode字符");

        byte[] bytedata = { 95, 0 };//  32,95 };

        string str1 = new string(Encoding.Unicode.GetChars(bytedata));


        Debug.Log(",ischinese=" + isChinese(bytedata) + ",curstr=" + str1);


        //Debug.Log("str=" + str1);
        
    }

    private bool isChinese(byte[] item)
    {
        return item[0] >= 0x00 && item[0] <= 0xBF &&
            item[1] >= 0x4e && item[1] <= 0x9f;
    }
}
