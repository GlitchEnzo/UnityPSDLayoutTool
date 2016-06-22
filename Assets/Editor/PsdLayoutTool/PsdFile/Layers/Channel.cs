namespace PhotoshopFile
{
    using System.IO;

    /// <summary>
    /// The channel data for a layer
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Channel"/> class.
        /// </summary>
        /// <param name="reader">The reader to use to initialize the instance.</param>
        /// <param name="layer">The layer this channel belongs to.</param>
        internal Channel(BinaryReverseReader reader, Layer layer)
        {
            ID = reader.ReadInt16();
            Length = reader.ReadInt32();
            Layer = layer;
        }

        /// <summary>
        /// Gets the length of the compressed channel data.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Gets the ID of the channel.
        /// 0 = red, 1 = green, etc.
        /// –1 = transparency mask
        /// –2 = user supplied layer mask
        /// </summary>
        public short ID { get; private set; }

        /// <summary>
        /// Gets or sets the compressed raw channel data
        /// </summary>
        public byte[] Data { private get; set; }

        /// <summary>
        /// Gets or sets the raw image data from the channel.
        /// </summary>
        public byte[] ImageData { get; set; }

        /// <summary>
        /// Gets or sets the compression method of the image
        /// </summary>
        public ImageCompression ImageCompression { get; set; }

        /// <summary>
        /// Gets a BinaryReverseReader setup to read the Channel data contained within this channel.
        /// </summary>
        public BinaryReverseReader DataReader
        {
            get
            {
                if (Data == null)
                {
                    return null;
                }

                return new BinaryReverseReader(new MemoryStream(Data));
            }
        }

        /// <summary>
        /// Gets or sets the layer to which this channel belongs
        /// </summary>
        private Layer Layer { get; set; }

        /// <summary>
        /// Reads the pixel data from a reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the pixel data.</param>
        internal void LoadPixelData(BinaryReverseReader reader)
        {
            Data = reader.ReadBytes(Length);
            using (BinaryReverseReader dataReader = DataReader)
            {
                ImageCompression = (ImageCompression)dataReader.ReadInt16();
                int columns = 0;
                switch (Layer.PsdFile.Depth)
                {
                    case 1:
                        columns = (int)Layer.Rect.width;
                        break;
                    case 8:
                        columns = (int)Layer.Rect.width;
                        break;
                    case 16:
                        columns = (int)Layer.Rect.width * 2;
                        break;
                }

                ImageData = new byte[(int)Layer.Rect.height * columns];
                switch (ImageCompression)
                {
                    case ImageCompression.Raw:
                        dataReader.Read(ImageData, 0, ImageData.Length);
                        break;
                    case ImageCompression.Rle:
                        int[] nums = new int[(int)Layer.Rect.height];

                        for (int i = 0; i < Layer.Rect.height; i++)
                        {
                            nums[i] = dataReader.ReadInt16();
                        }

                        for (int index = 0; index < Layer.Rect.height; ++index)
                        {
                            int startIdx = index * (int)Layer.Rect.width;
                            RleHelper.DecodedRow(dataReader.BaseStream, ImageData, startIdx, columns);
                        }

                        break;
                }
            }
        }
    }
}
