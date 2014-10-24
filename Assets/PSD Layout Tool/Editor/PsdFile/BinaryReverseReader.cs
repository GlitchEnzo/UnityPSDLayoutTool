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
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryReverseReader"/> class using the given stream.
        /// </summary>
        /// <param name="stream">The stream to read through.</param>
        public BinaryReverseReader(Stream stream)
            : base(stream, Encoding.UTF7)
        {
        }

        /// <summary>
        /// Reads a 16 bit int (2 bytes) from the stream.
        /// </summary>
        /// <returns>The read 16 bit int.</returns>
        public override short ReadInt16()
        {
            short num = base.ReadInt16();
            unsafe
            {
                SwapBytes((byte*)&num, 2);
            }

            return num;
        }

        /// <summary>
        /// Reads a 32 bit int (4 bytes) from the stream.
        /// </summary>
        /// <returns>The read 32 bit int.</returns>
        public override unsafe int ReadInt32()
        {
            int num = base.ReadInt32();
            SwapBytes((byte*)&num, 4);
            return num;
        }

        /// <summary>
        /// Reads a 64 bit int (8 bytes) from the stream.
        /// </summary>
        /// <returns>The read 64 bit int.</returns>
        public override unsafe long ReadInt64()
        {
            long num = base.ReadInt64();
            SwapBytes((byte*)&num, 8);
            return num;
        }

        /// <summary>
        /// Reads an unsigned 16 bit int (2 bytes) from the stream.
        /// </summary>
        /// <returns>The read unsigned 16 bit int.</returns>
        public override unsafe ushort ReadUInt16()
        {
            ushort num = base.ReadUInt16();
            SwapBytes((byte*)&num, 2);
            return num;
        }

        /// <summary>
        /// Reads an unsigned 32 bit int (4 bytes) from the stream.
        /// </summary>
        /// <returns>The read unsigned 32 bit int.</returns>
        public override unsafe uint ReadUInt32()
        {
            uint num = base.ReadUInt32();
            SwapBytes((byte*)&num, 4);
            return num;
        }

        /// <summary>
        /// Reads an unsigned 64 bit int (8 bytes) from the stream.
        /// </summary>
        /// <returns>The read unsigned 64 bit int.</returns>
        public override unsafe ulong ReadUInt64()
        {
            ulong num = base.ReadUInt64();
            SwapBytes((byte*)&num, 8);
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

            return new string(Encoding.ASCII.GetChars(bytes));
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

        /// <summary>
        /// Reads a string stored with a null byte preceding each character.
        /// </summary>
        /// <returns>The read string.</returns>
        public override string ReadString()
        {
            string str = string.Empty;
            try
            {
                while (BaseStream.Position < BaseStream.Length)
                {
                    if (ReadChar() == 0)
                    {
                        str = str + (char)ReadByte();
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

            return str;
        }

        /// <summary>
        /// Searches through the stream for the given string.  If found, the position in the stream
        /// will be the byte right AFTER the search string.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="search">The string to search for.</param>
        public void Seek(string search)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(search);
            Seek(bytes);
        }

        /// <summary>
        /// Swaps the number of specified bytes in the stream.
        /// </summary>
        /// <param name="ptr">The pointer to the byte stream.</param>
        /// <param name="length">The number of bytes to swap.</param>
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
        /// Searches through the stream for the given byte array.  If found, the position in the stream
        /// will be the byte right AFTER the search array.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="search">The byte array sequence to search for in the stream</param>
        private void Seek(byte[] search)
        {
            // read continuously until we find the first byte
            while (BaseStream.Position < BaseStream.Length && ReadByte() != search[0])
            {
                // do nothing
            }

            // ensure we haven't reached the end of the stream
            if (BaseStream.Position >= BaseStream.Length)
            {
                return;
            }

            // ensure we have found the entire byte sequence
            for (int index = 1; index < search.Length; ++index)
            {
                if (ReadByte() != search[index])
                {
                    // if the sequence doesn't match fully, try seeking for it again
                    Seek(search);
                    break;
                }
            }
        }
    }
}
