using System.Collections.Specialized;
using System.Drawing;

namespace PhotoshopFile
{
    /// <summary>
    /// The mask data for a layer
    /// </summary>
    public class Mask
    {
        private static readonly int m_positionIsRelativeBit = BitVector32.CreateMask();
        private static readonly int m_disabledBit = BitVector32.CreateMask(m_positionIsRelativeBit);
        private static readonly int m_invertOnBlendBit = BitVector32.CreateMask(m_disabledBit);
        private Rectangle m_rect = Rectangle.Empty;
        private BitVector32 m_flags;

        /// <summary>
        /// The layer to which this mask belongs
        /// </summary>
        public Layer Layer { get; private set; }

        /// <summary>
        /// The rectangle enclosing the mask.
        /// </summary>
        public Rectangle Rect
        {
            get
            {
                return m_rect;
            }
            set
            {
                m_rect = value;
            }
        }

        /// <summary>
        /// Gets/Sets the default color of the mask
        /// </summary>
        public byte DefaultColor { get; set; }

        /// <summary>
        /// If true, the position of the mask is relative to the layer.
        /// </summary>
        public bool PositionIsRelative
        {
            get
            {
                return m_flags[m_positionIsRelativeBit];
            }
            set
            {
                m_flags[m_positionIsRelativeBit] = value;
            }
        }

        /// <summary>
        /// Gets/Sets if the mask is diabled
        /// </summary>
        public bool Disabled
        {
            get
            {
                return m_flags[m_disabledBit];
            }
            set
            {
                m_flags[m_disabledBit] = value;
            }
        }

        /// <summary>
        /// if true, invert the mask when blending.
        /// </summary>
        public bool InvertOnBlendBit
        {
            get
            {
                return m_flags[m_invertOnBlendBit];
            }
            set
            {
                m_flags[m_invertOnBlendBit] = value;
            }
        }

        /// <summary>
        /// The raw image data from the channel.
        /// </summary>
        public byte[] ImageData { get; set; }

        static Mask()
        {
        }

        internal Mask(Layer layer)
        {
            Layer = layer;
            Layer.MaskData = this;
        }

        internal Mask(BinaryReverseReader reader, Layer layer)
        {
            Layer = layer;
            uint num1 = reader.ReadUInt32();
            if (num1 <= 0U)
                return;
            long position = reader.BaseStream.Position;
            m_rect = new Rectangle();
            m_rect.Y = reader.ReadInt32();
            m_rect.X = reader.ReadInt32();
            m_rect.Height = reader.ReadInt32() - m_rect.Y;
            m_rect.Width = reader.ReadInt32() - m_rect.X;
            DefaultColor = reader.ReadByte();
            m_flags = new BitVector32(reader.ReadByte());
            if ((int)num1 == 36)
            {
                reader.ReadByte();  // bit vector
                reader.ReadByte();  // ???
                reader.ReadInt32(); // rect Y
                reader.ReadInt32(); // rect X
                reader.ReadInt32(); // rect total height (actual height = this - Y)
                reader.ReadInt32(); // rect total width (actual width = this - Y)
            }
            reader.BaseStream.Position = position + num1;
        }

        internal void LoadPixelData(BinaryReverseReader reader)
        {
            if (m_rect.IsEmpty || !Layer.SortedChannels.ContainsKey(-2))
                return;
            Channel channel = Layer.SortedChannels[-2];
            channel.Data = reader.ReadBytes(channel.Length);
            using (BinaryReverseReader dataReader = channel.DataReader)
            {
                channel.ImageCompression = (ImageCompression)dataReader.ReadInt16();
                int columns = 0;
                switch (Layer.PsdFile.Depth)
                {
                    case 1:
                        columns = m_rect.Width;
                        break;
                    case 8:
                        columns = m_rect.Width;
                        break;
                    case 16:
                        columns = m_rect.Width * 2;
                        break;
                }
                channel.ImageData = new byte[m_rect.Height * columns];
                for (int index = 0; index < channel.ImageData.Length; ++index)
                    channel.ImageData[index] = 171;
                ImageData = (byte[])channel.ImageData.Clone();
                switch (channel.ImageCompression)
                {
                    case ImageCompression.Raw:
                        dataReader.Read(channel.ImageData, 0, channel.ImageData.Length);
                        break;
                    case ImageCompression.Rle:
						int[] nums = new int[m_rect.Height];
						for (int i = 0; i < m_rect.Height; i++ )
                            nums[i] = dataReader.ReadInt16();
                        for (int index = 0; index < m_rect.Height; ++index)
                        {
                            int startIdx = index * m_rect.Width;
                            RleHelper.DecodedRow(dataReader.BaseStream, channel.ImageData, startIdx, columns);
                        }
                        break;
                }
                ImageData = (byte[])channel.ImageData.Clone();
            }
        }
    }
}