using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace PhotoshopFile
{
    /// <summary>
    /// Summary description for Thumbnail.
    /// </summary>
    public class Thumbnail : ImageResource
    {
        public Bitmap Image { get; set; }

        public Thumbnail(ImageResource imgRes)
            : base(imgRes)
        {
            using (BinaryReverseReader dataReader = DataReader)
            {
                int num1 = dataReader.ReadInt32();
                int width = dataReader.ReadInt32();
                int height = dataReader.ReadInt32();
                dataReader.ReadInt32();
                dataReader.ReadInt32();
                dataReader.ReadInt32();
                dataReader.ReadInt16();
                dataReader.ReadInt16();
                if (num1 == 1)
                {
                    using (MemoryStream memoryStream = new MemoryStream(dataReader.ReadBytes((int)(dataReader.BaseStream.Length - dataReader.BaseStream.Position))))
                    {
                        Image = (Bitmap)System.Drawing.Image.FromStream(memoryStream).Clone();
                    }
                }
                else
                    Image = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }
        }
    }
}
