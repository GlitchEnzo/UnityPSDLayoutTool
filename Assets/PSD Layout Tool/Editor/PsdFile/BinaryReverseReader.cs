using System.IO;
using System.Text;

namespace PhotoshopFile
{
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

        protected unsafe void SwapBytes(byte* ptr, int nLength)
        {
            for (long index = 0L; index < (long)(nLength / 2); ++index)
            {
                byte num = ptr[index];
                ptr[index] = *(ptr + nLength - index - 1);
                *(ptr + nLength - index - 1) = num;
            }
        }
    }
}
