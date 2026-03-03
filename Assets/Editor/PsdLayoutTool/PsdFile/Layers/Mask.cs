namespace PhotoshopFile
{
    using System.Collections.Specialized;
    using UnityEngine;

    /// <summary>
    /// The mask data for a layer
    /// </summary>
    public class Mask
    {
        /// <summary>
        /// The bit indicating whether the position is relative or not.
        /// </summary>
        private static readonly int PositionIsRelativeBit = BitVector32.CreateMask();

        /// <summary>
        /// The <see cref="Rect"/> making up the mask.
        /// </summary>
        private Rect rect;

        /// <summary>
        /// The flags for the mask.
        /// </summary>
        private BitVector32 flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mask"/> class.
        /// </summary>
        /// <param name="reader">The reader to use to initialize the instance.</param>
        /// <param name="layer">The layer this mask belongs to.</param>
        internal Mask(BinaryReverseReader reader, Layer layer)
        {
            Layer = layer;
            uint num1 = reader.ReadUInt32();
            Debug.Log("mask num1=" + num1);
            if (num1 <= 0U)
            {
                return;
            }

            long position = reader.BaseStream.Position;
            rect = new Rect();
            rect.y = reader.ReadInt32();
            rect.x = reader.ReadInt32();
            rect.height = reader.ReadInt32() - rect.y;
            rect.width = reader.ReadInt32() - rect.x;
            DefaultColor = reader.ReadByte();
            flags = new BitVector32(reader.ReadByte());

            int tempNum1 = -1;
            int tempNum2 = -1;
            int tempNum3 = -1;
            int tempNum4 = -1;
            int tempNum5 = -1;
            int tempNum6 = -1;

            if ((int)num1 == 36)
            {
                tempNum1= reader.ReadByte();  // bit vector
                tempNum2= reader.ReadByte();  // ???
             tempNum3=   reader.ReadInt32(); // rect Y
                tempNum4= reader.ReadInt32(); // rect X
                tempNum5= reader.ReadInt32(); // rect total height (actual height = this - Y)
                tempNum6= reader.ReadInt32(); // rect total width (actual width = this - Y)
            }
             
            Debug.Log("燕茹 Mask data"
                +",num1="+num1
                + ",DefaultColor=" + DefaultColor
                +",flags="+flags
                + ",tempNum1="+ tempNum1
                + ",tempNum2=" + tempNum2
                + ",tempNum3=" + tempNum3
                + ",tempNum4=" + tempNum4
                + ",tempNum5=" + tempNum5
                + ",tempNum6=" + tempNum6
                );
            reader.BaseStream.Position = position + num1;
        }

        /// <summary>
        /// Gets the layer to which this mask belongs
        /// </summary>
        public Layer Layer { get; private set; }

        /// <summary>
        /// Gets the rectangle enclosing the mask.
        /// </summary>
        public Rect Rect
        {
            get { return rect; }
        }

        /// <summary>
        /// Gets a value indicating whether the position of the mask is relative to the layer or not.
        /// </summary>
        public bool PositionIsRelative
        {
            get { return flags[PositionIsRelativeBit]; }
        }

        /// <summary>
        /// Gets the raw image data from the channel.
        /// </summary>
        public byte[] ImageData
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the default color of the mask
        /// </summary>
        private byte DefaultColor { get; set; }

        /// <summary>
        /// Reads the pixel data from a reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the pixel data.</param>
        internal void LoadPixelData(BinaryReverseReader reader)
        {
            if (rect.width <= 0 || !Layer.SortedChannels.ContainsKey(-2))
            {
                return;
            }

            Channel channel = Layer.SortedChannels[-2];
            channel.Data = reader.ReadBytes(channel.Length);
            using (BinaryReverseReader dataReader = channel.DataReader)
            {
                channel.ImageCompression = (ImageCompression)dataReader.ReadInt16();
                int columns = 0;
                switch (Layer.PsdFile.Depth)
                {
                    case 1:
                        columns = (int)rect.width;
                        break;
                    case 8:
                        columns = (int)rect.width;
                        break;
                    case 16:
                        columns = (int)rect.width * 2;
                        break;
                }

                channel.ImageData = new byte[(int)rect.height * columns];
                for (int index = 0; index < channel.ImageData.Length; ++index)
                {
                    channel.ImageData[index] = 171;
                }

                ImageData = (byte[])channel.ImageData.Clone();
                switch (channel.ImageCompression)
                {
                    case ImageCompression.Raw:
                        dataReader.Read(channel.ImageData, 0, channel.ImageData.Length);
                        break;
                    case ImageCompression.Rle:
                        int[] nums = new int[(int)rect.height];
                        for (int i = 0; i < (int)rect.height; i++)
                        {
                            nums[i] = dataReader.ReadInt16();
                        }

                        for (int index = 0; index < (int)rect.height; ++index)
                        {
                            int startIdx = index * (int)rect.width;
                            RleHelper.DecodedRow(dataReader.BaseStream, channel.ImageData, startIdx, columns);
                        }

                        break;
                }

                ImageData = (byte[])channel.ImageData.Clone();
            }
        }
    }
}