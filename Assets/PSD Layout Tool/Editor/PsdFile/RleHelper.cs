namespace PhotoshopFile
{
    using System.IO;

    internal static class RleHelper
    {
        public static void DecodedRow(Stream stream, byte[] imgData, int startIdx, int columns)
        {
            int num1 = 0;
        label_11:
            while (num1 < columns)
            {
                int num2 = (byte)stream.ReadByte();
                if (num2 < 128)
                {
                    int num3 = num2 + 1;
                    while (true)
                    {
                        if (num3 != 0 && startIdx + num1 < imgData.Length)
                        {
                            byte num4 = (byte)stream.ReadByte();
                            imgData[startIdx + num1] = num4;
                            ++num1;
                            --num3;
                        }
                        else
                            goto label_11;
                    }
                }

                if (num2 > 128)
                {
                    int num3 = (num2 ^ byte.MaxValue) + 2;
                    byte num4 = (byte)stream.ReadByte();
                    while (true)
                    {
                        if (num3 != 0 && startIdx + num1 < imgData.Length)
                        {
                            imgData[startIdx + num1] = num4;
                            ++num1;
                            --num3;
                        }
                        else
                            goto label_11;
                    }
                }
            }
        }
    }
}
