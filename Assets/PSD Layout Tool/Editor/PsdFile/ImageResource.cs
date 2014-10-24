namespace PhotoshopFile
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents an image resource.
    /// </summary>
    public class ImageResource
    {
        protected ImageResource(ImageResource imgRes)
        {
            ID = imgRes.ID;
            Name = imgRes.Name;
            Data = new byte[imgRes.Data.Length];
            imgRes.Data.CopyTo(Data, 0);
        }

        public ImageResource(BinaryReverseReader reader)
        {
            // read the OS type
            string osType = new string(reader.ReadChars(4));
            if (osType != "8BIM" && osType != "MeSa")
            {
                throw new InvalidOperationException("Could not read an image resource");
            }

            // read the ID
            ID = reader.ReadInt16();

            // read the name
            Name = string.Empty;
            Name = reader.ReadPascalString();

            // read the length of the data in bytes
            uint length = reader.ReadUInt32();

            // read the actual data
            Data = reader.ReadBytes((int)length);
            if (reader.BaseStream.Position % 2L != 1L)
            {
                return;
            }
            reader.ReadByte();
        }

        public short ID { get; private set; }

        private string Name { get; set; }

        public byte[] Data { get; private set; }

        public BinaryReverseReader DataReader
        {
            get
            {
                return new BinaryReverseReader(new MemoryStream(Data));
            }
        }
    }
}
