namespace PhotoshopFile
{
    using System.IO;

    /// <summary>
    /// Represents a set of helper methods for RLE.
    /// </summary>
    internal static class RleHelper
    {
        /// <summary>
        /// Reads a row of data from an RLE.
        /// </summary>
        /// <param name="stream">The stream containing the data</param>
        /// <param name="imgData">The output image data</param>
        /// <param name="startIdx">The starting index</param>
        /// <param name="columns">The number of columns</param>
        public static void DecodedRow(Stream stream, byte[] imgData, int startIdx, int columns)
        {
            int num1 = 0;
        label_11:
            while (num1 < columns)
            {
                int num2 = stream.ReadByte();
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
                        {
                            goto label_11;
                        }
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
                        {
                            goto label_11;
                        }
                    }
                }
            }
        }
    }
}
