namespace PhotoshopFile
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;
    /// <summary>
    /// Reads primitive data types as binary values in in big-endian format
    /// </summary>
    public class BinaryReverseReader : BinaryReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryReverseReader"/> class using the given stream.
        /// </summary>
        /// <param name="stream">The stream to read through.</param>
        public BinaryReverseReader(Stream stream)
            : base(stream, Encoding.Default)
        {
        }

        //读charCount个字节转为串
        public string readStringNew(int charCount)
        {
            return getEncodeStr(ReadBytes(charCount));
        }

        private string getEncodeStr(byte [] bytes)
        {
            return new string(Encoding.Default.GetChars(bytes));
        }
        
        /// <summary>
        /// Reads a 16 bit int (2 bytes) from the stream.
        /// </summary>
        /// <returns>The read 16 bit int.</returns>
        public override short ReadInt16()
        {
            short num = base.ReadInt16();
            num = ReverseBytes(num);
            return num;
        }

        /// <summary>
        /// Reads a 32 bit int (4 bytes) from the stream.
        /// </summary>
        /// <returns>The read 32 bit int.</returns>
        public override int ReadInt32()
        {
            int num = base.ReadInt32();
            num = ReverseBytes(num);
            return num;
        }

        /// <summary>
        /// Reads a 64 bit int (8 bytes) from the stream.
        /// </summary>
        /// <returns>The read 64 bit int.</returns>
        public override long ReadInt64()
        {
            long num = base.ReadInt64();
            num = ReverseBytes(num);
            return num;
        }

        /// <summary>
        /// Reads an unsigned 16 bit int (2 bytes) from the stream.
        /// </summary>
        /// <returns>The read unsigned 16 bit int.</returns>
        public override ushort ReadUInt16()
        {
            ushort num = base.ReadUInt16();
            num = ReverseBytes(num);
            return num;
        }

        /// <summary>
        /// Reads an unsigned 32 bit int (4 bytes) from the stream.
        /// </summary>
        /// <returns>The read unsigned 32 bit int.</returns>
        public override uint ReadUInt32()
        {
            uint num = base.ReadUInt32();
            num = ReverseBytes(num);
            return num;
        }

        /// <summary>
        /// Reads an unsigned 64 bit int (8 bytes) from the stream.
        /// </summary>
        /// <returns>The read unsigned 64 bit int.</returns>
        public override ulong ReadUInt64()
        {
            ulong num = base.ReadUInt64();
            num = ReverseBytes(num);
            return num;
        }

        /// <summary>
        /// Reads a pascal string from the stream.
        /// </summary>
        /// <returns>The read string.</returns>
        public string ReadPascalString()
        {
            byte num1 = ReadByte();
            byte[] bytes = ReadBytes(num1);
            if (num1 % 2 == 0)
            {
                ReadByte();
            }
            return   getEncodeStr(bytes);
        }

        /// <summary>
        /// Reads a floating point number from the stream.  It reads until the newline character '\n' is found.
        /// </summary>
        /// <returns>The read floating point number.</returns>
        public float ReadFloat()
        {
            string str = string.Empty;

            try
            {
                for (int index = PeekChar(); index != 10; index = PeekChar())
                {
                    if (index != 32)
                    {
                        str = str + ReadChar();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (ArgumentException)
            {
                UnityEngine.Debug.LogError("An invalid character was found in the string.");
            }

            if (string.IsNullOrEmpty(str))
            {
                return 0.0f;
            }

            return Convert.ToSingle(str);
        }

        //public string ReadStringCount(int charCount)
        //{

        //}
        ///注意：Unicode字符串终止条件，高低位byte都为0（空字符时）.或者高低位结合起来并不是Unicode字符， 就stop
        ///非ASCII码（两字节中地位为0）  时只 判断是否满足中文
        ///
        /// <summary>
        /// Reads a string stored with a null byte preceding each character.
        /// </summary>
        /// <returns>The read string.</returns>
        public string ReadString( bool testPrintLog = false)
        { 
            string str = string.Empty;

            List<byte> bytelist = new List<byte>();

          //  Debug.Log(Time.time + "total length="+ BaseStream.Length + ",curpos="+BaseStream.Position);

            int readCount = 0;
            try
            {
                while (BaseStream.Position < BaseStream.Length)
                {
                    byte byte1 =  ReadByte();

                    readCount++;
                    if (byte1 == 0) //byte=0是ASCII码表中的空字符
                    {
                        byte byte2 = ReadByte();
                        if (byte2 != 0) //高地位都为0的话就真的 没有字符
                        {
                            bytelist.Add(byte2);
                            bytelist.Add(0);

                            if (testPrintLog)
                            {
                                Debug.Log(Time.time + "add  0,,byte2=" + byte2 + ",position=" + BaseStream.Position);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        byte byte2 = ReadByte();

                        if (byte2 != 0) 
                        {
                            if (byte1 != 0 && byte2 != 0 && !isChinese(byte2, byte1  ))
                            {
                                break;
                            }
                            else
                            {
                                bytelist.Add(byte2);
                                bytelist.Add(byte1);

                                if (testPrintLog)
                                {
                                    Debug.Log(Time.time + "addbyte1=" + byte1 + ",byte2=" + byte2 + ",canadd?" + (byte2 != 0) +
                                        ",position=" + BaseStream.Position);
                                }
                            }
                        }
                        else
                        {
                            break;
                        } 
                    }
                }
                 

            }
            catch (ArgumentException)
            {
                UnityEngine.Debug.LogError("An invalid character was found in the string.");
            }

           
            byte[] res = new byte[bytelist.Count];
            for (int index = 0; index < bytelist.Count; index++)
                res[index] = bytelist[index];
             
           
            str=  new string(Encoding.Unicode.GetChars(res));

            if (testPrintLog)
            {
                Debug.Log(" str=" + str);
                for (int i = 0; i < res.Length; i++)
                {
                    Debug.Log("byte[" + i + "] =" + res[i]);
                }
            }
            
            return str;
        }

        private bool isChinese(byte  item1, byte item2)
        {
            return item1 >= 0x00 && item1 <= 0xBF &&
                item2 >= 0x4e && item2 <= 0x9f;
        }

        /// <summary>
        /// Searches through the stream for the given string.  If found, the position in the stream
        /// will be the byte right AFTER the search string.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="search">The string to search for.</param>
        public void Seek(string search)
        {
            byte[] bytes =  Encoding.Default.GetBytes(search);
            Seek(bytes);
        }

        /// <summary>
        /// Reverses the bytes in order to convert back and forth between Big and Little Endian.
        /// </summary>
        /// <returns>The number with the reversed bytes.</returns>
        /// <param name="value">The number to reverse the bytes of.</param>
        /// <remarks>
        /// See: http://www.csharp-examples.net/reverse-bytes/
        /// And: http://stackoverflow.com/questions/19560436/bitwise-endian-swap-for-various-types
        /// </remarks>
        private Int16 ReverseBytes(Int16 value)
        {
            return (Int16)ReverseBytes((UInt16)value);
        }

        private Byte ReverseBytes(Byte value)
        {
            return (Byte)ReverseBytes((Byte)value);
        }

        /// <summary>
        /// Reverses the bytes in order to convert back and forth between Big and Little Endian.
        /// </summary>
        /// <returns>The number with the reversed bytes.</returns>
        /// <param name="value">The number to reverse the bytes of.</param>
        private Int32 ReverseBytes(Int32 value)
        {
            return (Int32)ReverseBytes((UInt32)value);
        }

        /// <summary>
        /// Reverses the bytes in order to convert back and forth between Big and Little Endian.
        /// </summary>
        /// <returns>The number with the reversed bytes.</returns>
        /// <param name="value">The number to reverse the bytes of.</param>
        private Int64 ReverseBytes(Int64 value)
        {
            return (Int64)ReverseBytes((UInt64)value);
        }

        /// <summary>
        /// Reverses the bytes in order to convert back and forth between Big and Little Endian.
        /// </summary>
        /// <returns>The number with the reversed bytes.</returns>
        /// <param name="value">The number to reverse the bytes of.</param>
        private UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        /// <summary>
        /// Reverses the bytes in order to convert back and forth between Big and Little Endian.
        /// </summary>
        /// <returns>The number with the reversed bytes.</returns>
        /// <param name="value">The number to reverse the bytes of.</param>
        private UInt32 ReverseBytes(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }

        /// <summary>
        /// Reverses the bytes in order to convert back and forth between Big and Little Endian.
        /// </summary>
        /// <returns>The number with the reversed bytes.</returns>
        /// <param name="value">The number to reverse the bytes of.</param>
        private UInt64 ReverseBytes(UInt64 value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                    (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                    (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }

        //public string str = "";
        //public string str2 = "";
        //private int testIndex = 0;
        //private void addString(string inStr)
        //{
        //    MatchCollection match = Regex.Matches(str, "\n");
        //    if (match.Count < 500)
        //        str += inStr;
        //    else
        //        str2 += inStr;

        //    //testIndex++;
        //}

        /// <summary>
        /// Searches through the stream for the given byte array.  If found, the position in the stream
        /// will be the byte right AFTER the search array.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="search">The byte array sequence to search for in the stream</param>
        private void Seek(byte[] search)
        {
            // read continuously until we find the first byte
            while (BaseStream.Position < BaseStream.Length)//&& )
            {
              //  addString("\nBaseStream.Position=" + BaseStream.Position + ",search[0]=" + search[0]);
                byte temp = ReadByte();

             //   addString(",readbyte=" + temp);

                if (temp == search[0])
                {
               //     addString("\n    BREAK");
                    break;
                }
                // do nothing
            }

            // ensure we haven't reached the end of the stream
            if (BaseStream.Position >= BaseStream.Length)
            {
             //   addString("\nstop BaseStream.Position=" + BaseStream.Position + ",BaseStream.Length=" + BaseStream.Length);
                return;
            }

            // ensure we have found the entire byte sequence
            for (int index = 1; index < search.Length; ++index)
            {
             //   addString("\n read for BaseStream.Position=" + BaseStream.Position + ",search[index]=" + search[index]);
                byte byteTemp = ReadByte();
             //   addString(",readbyte=" + byteTemp);

                if (byteTemp != search[index])
                {
                    // if the sequence doesn't match fully, try seeking for it again
                    Seek(search);
                    break;
                }
            }
        }
    }
}
