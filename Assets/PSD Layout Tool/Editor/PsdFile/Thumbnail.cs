namespace PhotoshopFile
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;

    /// <summary>
    /// Summary description for Thumbnail.
    /// </summary>
    public class Thumbnail : ImageResource
    {
        private Bitmap image;

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
                        image = (Bitmap)Image.FromStream(memoryStream).Clone();
                    }
                }
                else
                    image = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }
        }
    }
}
