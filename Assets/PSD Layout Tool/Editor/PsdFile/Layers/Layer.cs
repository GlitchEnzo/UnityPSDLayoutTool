using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Text;
using UnityEngine;
using Color = System.Drawing.Color;

namespace PhotoshopFile
{
    /// <summary>
    /// Contains the data representation of a PSD layer
    /// </summary>
    public class Layer
    {
        private static readonly int m_protectTransBit = BitVector32.CreateMask();
        private static readonly int m_visibleBit = BitVector32.CreateMask(m_protectTransBit);
        private static readonly int m_obsoleteBit = BitVector32.CreateMask(m_visibleBit);
        private static readonly int m_ver5orLaterBit = BitVector32.CreateMask(m_obsoleteBit);
        private static readonly int m_pixelDataIrrelevantBit = BitVector32.CreateMask(m_ver5orLaterBit);
        private readonly string m_blendModeKey = "norm";
        private BitVector32 m_flags;
        private static bool errorShown;

        /// <summary>
        /// A list of the children <see cref="Layer"/>s that belong to this Layer.
        /// </summary>
        public List<Layer> Children { get; private set; }

        /// <summary>
        /// Set to true if the layer contains Text information.
        /// </summary>
        public bool IsTextLayer { get; private set; }

        /// <summary>
        /// If it is a text layer, this contains the actual text string
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// If it is a text layer, this is the point size of the font
        /// </summary>
        public float FontSize { get; private set; }

        /// <summary>
        /// If it is a text layer, this is the name of the font used
        /// </summary>
        public string FontName { get; private set; }

        /// <summary>
        /// Gets whether or not this layer has Effects/Styles
        /// </summary>
        public bool HasEffects { get; set; }

        /// <summary>
        /// If it is a text layer, this is the justification of the text.
        /// Can be Left, Right, or Center.
        /// </summary>
        public string Justification { get; private set; }

        /// <summary>
        /// If it is a text layer, this is the Fill Color of the text.
        /// Uses System.Drawing.Color object.
        /// </summary>
        public Color FillColor { get; private set; }

        /// <summary>
        /// If it is a text layer, this is the style of warp done on the text.
        /// Can be warpNone, warpTwist, etc.
        /// </summary>
        public string WarpStyle { get; private set; }

        internal PsdFile PsdFile { get; private set; }

        /// <summary>
        /// The rectangle containing the contents of the layer.
        /// </summary>
        public Rectangle Rect { get; private set; }

        /// <summary>
        /// Channel information
        /// </summary>
        public List<Channel> Channels { get; private set; }

        /// <summary>
        /// Channels sorted
        /// </summary>
        public SortedList<short, Channel> SortedChannels { get; private set; }

        public string BlendModeKey
        {
            get
            {
                return m_blendModeKey;
            }
            set
            {
                if (value.Length != 4)
                    throw new ArgumentException("Key length must be 4");
            }
        }

        /// <summary>
        /// 0 = transparent ... 255 = opaque
        /// </summary>
        public byte Opacity { get; set; }

        /// <summary>
        /// false = base, true = non–base
        /// </summary>
        public bool Clipping { get; set; }

        /// <summary>
        /// If true, the layer is visible.
        /// </summary>
        public bool Visible
        {
            get
            {
                return !m_flags[m_visibleBit];
            }
            set
            {
                m_flags[m_visibleBit] = !value;
            }
        }

        /// <summary>
        /// Protect the transparency
        /// </summary>
        public bool ProtectTrans
        {
            get
            {
                return m_flags[m_protectTransBit];
            }
            set
            {
                m_flags[m_protectTransBit] = value;
            }
        }

        /// <summary>
        /// If true, the pixel data in this layer doesn't affect the document.
        /// </summary>
        public bool IsPixelDataIrrelevant
        {
            get
            {
                return m_flags[m_pixelDataIrrelevantBit];
            }
        }

        /// <summary>
        /// The descriptive layer name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The blending ranges data of the layer
        /// </summary>
        public BlendingRanges BlendingRangesData { get; set; }

        /// <summary>
        /// The mask data of the layer
        /// </summary>
        public Mask MaskData { get; set; }

        /// <summary>
        /// The list of adjustment information of the layer
        /// </summary>
        public List<AdjusmentLayerInfo> AdjustmentInfo { get; set; }

        static Layer()
        {
        }

        /// <summary>
        /// Constructor that builds a new layer using the provided reader containing the PSD file data
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param><param name="psdFile">The PSD file to set as the reference</param>
        public Layer(BinaryReverseReader reader, PsdFile psdFile)
        {
            Children = new List<Layer>();
            PsdFile = psdFile;
            Rectangle rect = new Rectangle();
            rect.Y = reader.ReadInt32();
            rect.X = reader.ReadInt32();
            rect.Height = reader.ReadInt32() - rect.Y;
            rect.Width = reader.ReadInt32() - rect.X;
            Rect = rect;
            int num1 = reader.ReadUInt16();
            Channels = new List<Channel>();
            SortedChannels = new SortedList<short, Channel>();
            for (int index = 0; index < num1; ++index)
            {
                Channel channel = new Channel(reader, this);
                Channels.Add(channel);
                SortedChannels.Add(channel.ID, channel);
            }
            if (new string(reader.ReadChars(4)) != "8BIM")
                throw new IOException("Layer Channelheader error!");
            m_blendModeKey = new string(reader.ReadChars(4));
            Opacity = reader.ReadByte();
            Clipping = reader.ReadByte() > 0;
            m_flags = new BitVector32(reader.ReadByte());
            reader.ReadByte();
            uint num3 = reader.ReadUInt32();
            long position1 = reader.BaseStream.Position;
            MaskData = new Mask(reader, this);
            BlendingRangesData = new BlendingRanges(reader, this);
            long position2 = reader.BaseStream.Position;
            Name = reader.ReadPascalString();
            int count = (int)((reader.BaseStream.Position - position2) % 4L);
            reader.ReadBytes(count);
            AdjustmentInfo = new List<AdjusmentLayerInfo>();
            long num4 = position1 + num3;
            while (reader.BaseStream.Position < num4)
            {
                try
                {
                    AdjustmentInfo.Add(new AdjusmentLayerInfo(reader, this));
                }
                catch
                {
                    reader.BaseStream.Position = num4;
                }
            }
            foreach (AdjusmentLayerInfo adjusmentLayerInfo in AdjustmentInfo)
            {
                if (adjusmentLayerInfo.Key == "TySh")
                {
                    IsTextLayer = true;
                    BinaryReverseReader dataReader = adjusmentLayerInfo.DataReader;

                    Seek(dataReader, "/Text");
                    dataReader.ReadBytes(4);
                    Text = ReadString(dataReader);

                    Seek(dataReader, "/Justification ");
                    int num5 = dataReader.ReadByte() - 48;
                    Justification = "Left";
                    if (num5 == 1)
                        Justification = "Right";
                    else if (num5 == 2)
                        Justification = "Center";

                    Seek(dataReader, "/FontSize ");
                    FontSize = ReadFloat(dataReader);

                    Seek(dataReader, "/FillColor");
                    Seek(dataReader, "/Values [ ");
                    float alpha = ReadFloat(dataReader);
                    dataReader.ReadByte();
                    float red = ReadFloat(dataReader);
                    dataReader.ReadByte();
                    float green = ReadFloat(dataReader);
                    dataReader.ReadByte();
                    float blue = ReadFloat(dataReader);
                    FillColor = Color.FromArgb((int)(alpha * (double)byte.MaxValue), (int)(red * (double)byte.MaxValue), (int)(green * (double)byte.MaxValue), (int)(blue * (double)byte.MaxValue));

                    Seek(dataReader, "/FontSet ");
                    Seek(dataReader, "/Name");
                    dataReader.ReadBytes(4);
                    FontName = ReadString(dataReader);

                    Seek(dataReader, "warpStyle");
                    Seek(dataReader, "warpStyle");
                    dataReader.ReadBytes(3);
                    int num13 = dataReader.ReadByte();
                    WarpStyle = string.Empty;

                    for (; num13 > 0; --num13)
                    {
                        Layer layer = this;
                        string str = layer.WarpStyle + dataReader.ReadChar();
                        layer.WarpStyle = str;
                    }
                }
                else if (adjusmentLayerInfo.Key == "luni")
                {
                    BinaryReverseReader dataReader = adjusmentLayerInfo.DataReader;
                    dataReader.ReadBytes(3);
                    dataReader.ReadByte();
                    Name = ReadString(dataReader).TrimEnd(new char[1]);
                }
            }
            reader.BaseStream.Position = num4;
        }

        /// <summary>
        /// Reads a floating point number from the stream.  It reads until the newline character '\n' is found.
        /// </summary>
        /// <param name="reader"/>
        /// <returns/>
        private static float ReadFloat(BinaryReverseReader reader)
        {
            string str = string.Empty;

            try
            {
                for (int index = reader.PeekChar(); index != 10; index = reader.PeekChar())
                {
                    if (index != 32)
                        str = str + reader.ReadChar();
                    else
                        break;
                }
            }
            catch (ArgumentException)
            {
                if (!errorShown)
                {
                    errorShown = true;
					Debug.LogError("An invalid character was found in the string.");
                }
            }

            if (string.IsNullOrEmpty(str))
                return 0.0f;
            
            return Convert.ToSingle(str);
        }

        /// <summary>
        /// Reads a string stored with a null byte preceding each character.
        /// </summary>
        /// <param name="reader"/>
        /// <returns/>
        private static string ReadString(BinaryReverseReader reader)
        {
            string str = string.Empty;
            try
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    if (reader.ReadChar() == 0)
                        str = str + (char)reader.ReadByte();
                    else
                        break;
                }
            }
            catch (ArgumentException)
            {
                if (!errorShown)
                {
                    errorShown = true;
					Debug.LogError("An invalid character was found in the string.");
                }
            }
            return str;
        }

        /// <summary>
        /// Searches through the stream for the given string.  If found, the position in the stream
        /// will be the byte right AFTER the search string.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="reader"/><param name="search"/>
        private static void Seek(BinaryReverseReader reader, string search)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(search);
            Seek(reader, bytes);
        }

        /// <summary>
        /// Searches through the stream for the given byte array.  If found, the position in the stream
        /// will be the byte right AFTER the search array.  If it is not found, the position will be the
        /// end of the stream.
        /// </summary>
        /// <param name="reader">The reader to use to read through the stream</param><param name="search">The byte array sequence to search for in the stream</param>
        private static void Seek(BinaryReverseReader reader, byte[] search)
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length && reader.ReadByte() != search[0])
            {
                // do nothing
            }

            if (reader.BaseStream.Position >= reader.BaseStream.Length)
            {
                return;
            }

            for (int index = 1; index < search.Length; ++index)
            {
                if (reader.ReadByte() != search[index])
                {
                    Seek(reader, search);
                    break;
                }
            }
        }

        /// <summary>
        /// Converts the layer to a human readable string
        /// </summary>
        /// <returns>The layer in a human readable string format</returns>
        public override string ToString()
        {
            return "Layer: " + Name;
        }
    }
}
