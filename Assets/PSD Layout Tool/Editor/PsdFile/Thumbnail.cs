namespace PhotoshopFile
{
    using System.Drawing;
    using System.IO;

    /// <summary>
    /// Represents the thumbnail of a layer.
    /// </summary>
    public class Thumbnail : ImageResource
    {
        public Thumbnail(ImageResource imgRes)
            : base(imgRes)
        {
            using (BinaryReverseReader dataReader = DataReader)
            {
                // read unknown data ???
                int num1 = dataReader.ReadInt32();

                // read the width
                dataReader.ReadInt32();

                // read the height
                dataReader.ReadInt32();

                // read unknown data ???
                dataReader.ReadInt32();
                dataReader.ReadInt32();
                dataReader.ReadInt32();
                dataReader.ReadInt16();
                dataReader.ReadInt16();

                if (num1 == 1)
                {
                    using (MemoryStream memoryStream = new MemoryStream(dataReader.ReadBytes((int)(dataReader.BaseStream.Length - dataReader.BaseStream.Position))))
                    {
                        Image.FromStream(memoryStream);
                    }
                }
            }
        }
    }
}
