namespace PhotoshopFile
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents an image resource.
    /// </summary>
    public class ImageResource
    {
        public short ID { get; private set; }

        private string Name { get; set; }

        public byte[] Data { get; private set; }

        private string OsType { get; set; }

        public BinaryReverseReader DataReader
        {
            get
            {
                return new BinaryReverseReader(new MemoryStream(Data));
            }
        }

        protected ImageResource()
        {
            OsType = string.Empty;
            Name = string.Empty;
        }

        protected ImageResource(ImageResource imgRes)
        {
            OsType = string.Empty;
            ID = imgRes.ID;
            Name = imgRes.Name;
            Data = new byte[imgRes.Data.Length];
            imgRes.Data.CopyTo(Data, 0);
        }

        public ImageResource(BinaryReverseReader reader)
        {
            Name = string.Empty;
            OsType = new string(reader.ReadChars(4));
            if (OsType != "8BIM" && OsType != "MeSa")
            {
                throw new InvalidOperationException("Could not read an image resource");
            }
            ID = reader.ReadInt16();
            Name = reader.ReadPascalString();
            uint num1 = reader.ReadUInt32();
            Data = reader.ReadBytes((int)num1);
            if (reader.BaseStream.Position%2L != 1L)
            {
                return;
            }
            reader.ReadByte();
        }
    }
}
