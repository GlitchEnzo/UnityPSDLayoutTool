namespace PhotoshopFile
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Reads primitive data types as binary values in in big-endian format
    /// </summary>
    public class BinaryReverseReader : BinaryReader
    {
        public BinaryReverseReader(Stream stream)
            : base(stream, Encoding.UTF7)
        {
        }

        public override short ReadInt16()
        {
            short num = base.ReadInt16();
            unsafe
            {
                SwapBytes((byte*)&num, 2);
            }

            return num;
        }

        public override unsafe int ReadInt32()
        {
            int num = base.ReadInt32();
            SwapBytes((byte*)&num, 4);
            return num;
        }

        public override unsafe long ReadInt64()
        {
            long num = base.ReadInt64();
            SwapBytes((byte*)&num, 8);
            return num;
        }

        public override unsafe ushort ReadUInt16()
        {
            ushort num = base.ReadUInt16();
            SwapBytes((byte*)&num, 2);
            return num;
        }

        public override unsafe uint ReadUInt32()
        {
            uint num = base.ReadUInt32();
            SwapBytes((byte*)&num, 4);
            return num;
        }

        public override unsafe ulong ReadUInt64()
        {
            ulong num = base.ReadUInt64();
            SwapBytes((byte*)&num, 8);
            return num;
        }

        public string ReadPascalString()
        {
            byte num1 = ReadByte();
            byte[] bytes = ReadBytes(num1);
            if (num1 % 2 == 0)
            {
                ReadByte();
            }

            return new string(Encoding.ASCII.GetChars(bytes));
        }

        private unsafe void SwapBytes(byte* ptr, int length)
        {
            for (long index = 0L; index < (long)(length / 2); ++index)
            {
                byte num = ptr[index];
                ptr[index] = *(ptr + length - index - 1);
                *(ptr + length - index - 1) = num;
            }
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
                        str = str + ReadChar();
                    else
                        break;
                }
            }
            catch (ArgumentException)
            {
                UnityEngine.Debug.LogError("An invalid character was found in the string.");
            }

            if (string.IsNullOrEmpty(str))
                return 0.0f;

            return Convert.ToSingle(str);
        }

        /// <summary>
        /// Reads a string stored with a null byte preceding each character.
        /// </summary>
        /// <returns></returns>
        public override string ReadString()
        {
            string str = string.Empty;
            try
            {
                while (BaseStream.Position < BaseStream.Length)
                {
                    if (ReadChar() == 0)
                        str = str + (char)ReadByte();
                    else
                        break;
                }
            }
            catch (ArgumentException)
            {
                UnityEngine.Debug.LogError("An invalid character was found in the string.");
            }
            return str;
        }

        /// <summary>
        /// Searches through the stream for the given string.  If found, the position in the stream
        /// will be the byte right AFTER the search string.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="search"/>
        public void Seek(string search)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(search);
            Seek(bytes);
        }

        /// <summary>
        /// Searches through the stream for the given byte array.  If found, the position in the stream
        /// will be the byte right AFTER the search array.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="search">The byte array sequence to search for in the stream</param>
        private void Seek(byte[] search)
        {
            while (BaseStream.Position < BaseStream.Length && ReadByte() != search[0])
            {
                // do nothing
            }

            if (BaseStream.Position >= BaseStream.Length)
            {
                return;
            }

            for (int index = 1; index < search.Length; ++index)
            {
                if (ReadByte() != search[index])
                {
                    Seek(search);
                    break;
                }
            }
        }
    }
}
