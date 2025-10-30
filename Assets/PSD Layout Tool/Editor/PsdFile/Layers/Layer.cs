using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using UnityEngine;

namespace PhotoshopFile
{
    /// <summary>
    /// Contains the data representation of a PSD layer
    /// </summary>
    public class Layer
    {
        /// <summary>
        /// The bit flag representing transparency being protected.
        /// </summary>
        private static readonly int ProtectTransparencyBit = BitVector32.CreateMask();

        /// <summary>
        /// The bit flag representing the layer being visible.
        /// </summary>
        private static readonly int VisibleBit = BitVector32.CreateMask(ProtectTransparencyBit);

        /// <summary>
        /// The bit flag representing the layer being obsolete.  ???
        /// </summary>
        private static readonly int ObsoleteBit = BitVector32.CreateMask(VisibleBit);

        /// <summary>
        /// The bit flag representing the layer being version 5+.  ???
        /// </summary>
        private static readonly int Version5OrLaterBit = BitVector32.CreateMask(ObsoleteBit);

        /// <summary>
        /// The bit flag representing the layer's pixel data being irrelevant (a group layer, for example).
        /// </summary>
        private static readonly int PixelDataIrrelevantBit = BitVector32.CreateMask(Version5OrLaterBit);

        /// <summary>
        /// The set of flags associated with this layer.
        /// </summary>
        private BitVector32 flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="Layer"/> class using the provided reader containing the PSD file data.
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data.</param>
        /// <param name="psdFile">The PSD file to set as the parent.</param>
        public Layer(BinaryReverseReader reader, PsdFile psdFile)
        {
            Children = new List<Layer>();
            PsdFile = psdFile;

            // read the rect
            Rect rect = new Rect();
            rect.y = reader.ReadInt32();
            rect.x = reader.ReadInt32();
            rect.height = reader.ReadInt32() - rect.y;
            rect.width = reader.ReadInt32() - rect.x;
            Rect = rect;

            // read the channels
            int channelCount = reader.ReadUInt16();
            Channels = new List<Channel>();
            SortedChannels = new SortedList<short, Channel>();
            for (int index = 0; index < channelCount; ++index)
            {
                Channel channel = new Channel(reader, this);
                Channels.Add(channel);
                SortedChannels.Add(channel.ID, channel);
            }

            // read the header and verify it
            if (new string(reader.ReadChars(4)) != "8BIM")
            {
                throw new IOException("Layer Channelheader error!");
            }

            // read the blend mode key
            BlendModeKey = new string(reader.ReadChars(4));

            // read the opacity
            Opacity = reader.ReadByte();

            // read the clipping (unused) (< 0 = base, > 0 = non base)
            reader.ReadByte();

            // read all of the flags (protectTrans, visible, obsolete, ver5orLater, pixelDataIrrelevant)
            flags = new BitVector32(reader.ReadByte());

            // skip a padding byte
            reader.ReadByte();

            uint num3 = reader.ReadUInt32();
            long position1 = reader.BaseStream.Position;
            MaskData = new Mask(reader, this);
            BlendingRangesData = new BlendingRanges(reader);
            long position2 = reader.BaseStream.Position;

            // read the name
            Name = reader.ReadPascalString();

            // read the adjustment info
            int count = (int)((reader.BaseStream.Position - position2) % 4L);
            reader.ReadBytes(count);
            AdjustmentInfo = new List<AdjustmentLayerInfo>();
            long num4 = position1 + num3;
            while (reader.BaseStream.Position < num4)
            {
                try
                {
                    AdjustmentInfo.Add(new AdjustmentLayerInfo(reader, this));
                }
                catch
                {
                    reader.BaseStream.Position = num4;
                }
            }

            foreach (AdjustmentLayerInfo adjustmentLayerInfo in AdjustmentInfo)
            {
                if (adjustmentLayerInfo.Key == "TySh")
                {
                    ReadTextLayer(adjustmentLayerInfo.DataReader);
                }
                else if (adjustmentLayerInfo.Key == "luni")
                {
                    // read the unicode name
                    BinaryReverseReader dataReader = adjustmentLayerInfo.DataReader;
                    dataReader.ReadBytes(3);
                    dataReader.ReadByte();
                    Name = dataReader.ReadString().TrimEnd(new char[1]);
                }
            }

            reader.BaseStream.Position = num4;
        }

        #region Properties

        #region Text Layer Properties

        /// <summary>
        /// Gets a value indicating whether this layer is a text layer.
        /// </summary>
        public bool IsTextLayer { get; private set; }

        /// <summary>
        /// Gets the actual text string, if this is a text layer.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets the point size of the font, if this is a text layer.
        /// </summary>
        public float FontSize { get; private set; }

        /// <summary>
        /// Gets the name of the font used, if this is a text layer.
        /// </summary>
        public string FontName { get; private set; }

        /// <summary>
        /// Gets the justification of the text, if this is a text layer.
        /// </summary>
        public TextJustification Justification { get; private set; }

        /// <summary>
        /// Gets the Fill Color of the text, if this is a text layer.
        /// </summary>
        public Color FillColor { get; private set; }

        /// <summary>
        /// Gets the style of warp done on the text, if it is a text layer.
        /// Can be warpNone, warpTwist, etc.
        /// </summary>
        public string WarpStyle { get; private set; }

        #endregion

        /// <summary>
        /// Gets a list of the children <see cref="Layer"/>s that belong to this Layer.
        /// </summary>
        public List<Layer> Children { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this layer has Effects/Styles or not.
        /// </summary>
        public bool HasEffects { get; set; }

        /// <summary>
        /// Gets or sets the layer effects/styles applied to this layer.
        /// </summary>
        public PsdLayoutTool.LayerEffects LayerEffects { get; set; }
        
        /// <summary>
        /// Gets or sets the adjustment layer effects applied to this layer.
        /// </summary>
        public PsdLayoutTool.AdjustmentLayerEffects AdjustmentEffects { get; set; }
        
        /// <summary>
        /// Gets or sets the smart filters applied to this layer.
        /// </summary>
        public PsdLayoutTool.SmartFilters SmartFilters { get; set; }

        /// <summary>
        /// Gets the rectangle containing the contents of the layer.
        /// </summary>
        public Rect Rect { get; private set; }

        /// <summary>
        /// Gets a list of the Channel information.
        /// </summary>
        public List<Channel> Channels { get; private set; }

        /// <summary>
        /// Gets a sorted list of Channel information.
        /// </summary>
        public SortedList<short, Channel> SortedChannels { get; private set; }

        /// <summary>
        /// Gets the opacity of this layer.  0 = transparent and 255 = opaque/solid.
        /// </summary>
        public byte Opacity { get; private set; }

        /// <summary>
        /// Gets the blend mode key for this layer (e.g., "norm", "mult", "scrn").
        /// </summary>
        public string BlendModeKey { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this layer is visible or not.
        /// </summary>
        public bool Visible
        {
            get
            {
                return !flags[VisibleBit];
            }
        }

        /// <summary>
        /// Gets a value indicating whether this layer's pixel data is irrelevant.  This is often the case with group layers.
        /// </summary>
        public bool IsPixelDataIrrelevant
        {
            get
            {
                return flags[PixelDataIrrelevantBit];
            }
        }

        /// <summary>
        /// Gets or sets the name of the layer.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the mask data for this layer.
        /// </summary>
        public Mask MaskData { get; private set; }

        /// <summary>
        /// Gets the <see cref="PsdFile"/> that this <see cref="Layer"/> belongs to.
        /// </summary>
        internal PsdFile PsdFile { get; private set; }

        /// <summary>
        /// Gets or sets the blending ranges data for this layer.
        /// </summary>
        private BlendingRanges BlendingRangesData { get; set; }

        /// <summary>
        /// Gets or sets the list of adjustment information for this layer.
        /// </summary>
        public List<AdjustmentLayerInfo> AdjustmentInfo { get; set; }

        #endregion

        /// <summary>
        /// Reads the text information for the layer.
        /// </summary>
        /// <param name="dataReader">The reader to use to read the text data.</param>
        private void ReadTextLayer(BinaryReverseReader dataReader)
        {
            IsTextLayer = true;

            // read the text layer's text string
            dataReader.Seek("/Text");
            dataReader.ReadBytes(4);
            Text = dataReader.ReadString();

            // read the text justification
            dataReader.Seek("/Justification ");
            int justification = dataReader.ReadByte() - 48;
            Justification = TextJustification.Left;
            if (justification == 1)
            {
                Justification = TextJustification.Right;
            }
            else if (justification == 2)
            {
                Justification = TextJustification.Center;
            }

            // read the font size
            dataReader.Seek("/FontSize ");
            FontSize = dataReader.ReadFloat();

            // read the font fill color
            dataReader.Seek("/FillColor");
            dataReader.Seek("/Values [ ");
            float alpha = dataReader.ReadFloat();
            dataReader.ReadByte();
            float red = dataReader.ReadFloat();
            dataReader.ReadByte();
            float green = dataReader.ReadFloat();
            dataReader.ReadByte();
            float blue = dataReader.ReadFloat();
            FillColor = new Color(red * byte.MaxValue, green * byte.MaxValue, blue * byte.MaxValue, alpha * byte.MaxValue);

            // read the font name
            dataReader.Seek("/FontSet ");
            dataReader.Seek("/Name");
            dataReader.ReadBytes(4);
            FontName = dataReader.ReadString();

            // read the warp style
            dataReader.Seek("warpStyle");
            dataReader.Seek("warpStyle");
            dataReader.ReadBytes(3);
            int num13 = dataReader.ReadByte();
            WarpStyle = string.Empty;

            for (; num13 > 0; --num13)
            {
                string str = WarpStyle + dataReader.ReadChar();
                WarpStyle = str;
            }
        }
    }
}
