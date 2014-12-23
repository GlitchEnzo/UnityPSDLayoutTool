namespace PhotoshopFile
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents an image resource.
    /// </summary>
    public class ImageResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageResource"/> class using a reader.
        /// </summary>
        /// <param name="reader">The reader to use to create the instance.</param>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageResource"/> class using another reference.
        /// </summary>
        /// <param name="imgRes">The reference to copy.</param>
        protected ImageResource(ImageResource imgRes)
        {
            ID = imgRes.ID;
            Name = imgRes.Name;
            Data = new byte[imgRes.Data.Length];
            imgRes.Data.CopyTo(Data, 0);
        }

        /// <summary>
        /// Gets the ID of this resource.
        /// </summary>
        public short ID { get; private set; }

        /// <summary>
        /// Gets the internal data associated with this resource.
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// Gets a <see cref="BinaryReverseReader"/> that reads the internal <see cref="Data"/>.
        /// </summary>
        public BinaryReverseReader DataReader
        {
            get { return new BinaryReverseReader(new MemoryStream(Data)); }
        }

        /// <summary>
        /// Gets or sets the name of this resource.
        /// </summary>
        private string Name { get; set; }
    }
}
