namespace PhotoshopFile
{
    using PsdLayoutTool;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;

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


        static string testtotal = "";

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

                //Debug.Log(Time.time + "channel.ID=" + channel.ID + ",layer=" + this.Name);
                SortedChannels.Add(channel.ID, channel);
            }

            string head = reader.readStringNew(4);
            //Debug.Log(Time.time + ",head=" + head);
            // read the header and verify it
            if (head != "8BIM")
            {
                throw new IOException("Layer Channelheader error!");
            }

            // read the blend mode key (unused) (defaults to "norm")
            //reader.ReadChars(4);
            string layerRecordsBlendModeKey = reader.readStringNew(4);
             
            // read the opacity
            Opacity = reader.ReadByte();

            // read the clipping (unused) (< 0 = base, > 0 = non base)
            int Clipping = reader.ReadByte();

            // read all of the flags (protectTrans, visible, obsolete, ver5orLater, pixelDataIrrelevant)
            flags = new BitVector32(reader.ReadByte());

            // skip a padding byte
            int Filler = reader.ReadByte();

            imageTransparent =Convert.ToSingle( Opacity) / byte.MaxValue;
            Debug.Log("layerRecordsBlendModeKey=" + layerRecordsBlendModeKey
                + ",Opacity=" + Opacity
                + ",Clipping=" + Clipping
                + ",flags=" + flags
                + ", Filler=" + Filler
                + ",LayerTransparent=" + imageTransparent);

            uint num3 = reader.ReadUInt32();
            long position1 = reader.BaseStream.Position;
            MaskData = new Mask(reader, this);

            BlendingRangesData = new BlendingRanges(reader);
            long position2 = reader.BaseStream.Position;

            // read the name
            Name = reader.ReadPascalString();
            //Debug.Log(Time.time + ",read layer Name=" + Name + ".end");

            // read the adjustment info
            int count = (int)((reader.BaseStream.Position - position2) % 4L);
            reader.ReadBytes(count);
            //Debug.Log(Time.time + ",read count=" + count + ".end");

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


            string keyInfo = "";
            foreach (AdjustmentLayerInfo adjustmentLayerInfo in AdjustmentInfo)
            {
                keyInfo += ",key=" + adjustmentLayerInfo.Key + "\n";

                if (adjustmentLayerInfo.Key == "TySh")
                {
                    ReadTextLayer(adjustmentLayerInfo.DataReader);
                }
                else if (adjustmentLayerInfo.Key == "luni")
                {
                    // read the unicode name
                    BinaryReverseReader dataReader = adjustmentLayerInfo.DataReader;
                    byte[] temp1 = dataReader.ReadBytes(3);
                    byte charCount = dataReader.ReadByte();
                    //本来 charCount 是文本串的长度，可以传入ReadString()限定读取长度，但Text除串头无文本长度信息，因此改为读一段Unicode字符串
                    Name = dataReader.ReadString();
                    if (Name == "")
                        Name = defaultLayerName;

                }
                //此处针对字体  图层样式
                else if (adjustmentLayerInfo.Key == "lrFX")//样式 相关，对于字体来说，就是描边之类的
                {
                    parseLrfxKeyword(adjustmentLayerInfo);//yanruTODO测试屏蔽
                }
                //仅对于图片的 
                else if (adjustmentLayerInfo.Key== "lspf")
                {
                    BinaryReverseReader dataReader = adjustmentLayerInfo.DataReader;
                    byte[] data = dataReader.ReadBytes(4);
                    printbytes(data, "lspf data", true);
                }
                else if(adjustmentLayerInfo.Key == "lclr")
                {
                    BinaryReverseReader dataReader = adjustmentLayerInfo.DataReader;
                    byte[] data = dataReader.ReadBytes(10);
                    printbytes(data, "lclr data", true);
                }
            }

            Debug.Log("layer="+Name+ ",Totalkey=\n" + keyInfo);

            reader.BaseStream.Position = num4;
        }

        //图层效果相关
        private void parseLrfxKeyword(AdjustmentLayerInfo adjustmentLayerInfo)
        {
            BinaryReverseReader dataReader = adjustmentLayerInfo.DataReader;

            int version = dataReader.ReadInt16();
            int effectCount = dataReader.ReadInt16();
            //Debug.Log("lrfx version=" + version + ",effectCount=" + effectCount);

            string effectStr = "";

            for (int index = 0; index < effectCount; index++)
            {
                string sigNature = dataReader.readStringNew(4);
                string type = dataReader.readStringNew(4);
                //Debug.Log("cur read type=" + type + ",sigNature=" + sigNature);

                switch (type)
                {
                    case "cmnS"://OK
                        int cmnsSize = dataReader.ReadInt32();
                        int cmnsVersion = dataReader.ReadInt32();
                        bool cmnsBool = dataReader.ReadBoolean();
                        int cmnsUnused = dataReader.ReadInt16();

                        Debug.Log("cmnsSize =" + cmnsSize+ ",cmnsBool="+ cmnsBool);
                        break;
                    case "dsdw"://可能有用
                                //byte[] testbyte2 = dataReader.ReadBytes(55);
                                //effectStr += "\n" + printbytes(testbyte2, "dsdw");
                                //break;
                    case "isdw":
                        int dropSize = dataReader.ReadInt32();
                        int dropVersion = dataReader.ReadInt32();
                        int dropBlurValue = dataReader.ReadInt32();
                        int Intensityasapercent = dataReader.ReadInt32();
                        int angleindegrees = dataReader.ReadInt32();
                        int distanceinp = dataReader.ReadInt32();

                        byte[] colortest = dataReader.ReadBytes(10);
                         
                        dataReader.ReadBytes(4);
                        string dropBlendmode = dataReader.readStringNew(4);

                        bool dropeffectEnable = dataReader.ReadBoolean();
                        byte usethisangle = dataReader.ReadByte();
                        int dropOpacity = dataReader.ReadByte();


                        int dropSpace11 = dataReader.ReadInt16();
                        int color111 = dataReader.ReadInt16();
                        int color211 = dataReader.ReadInt16();
                        int color311 = dataReader.ReadInt16();
                        int color411 = dataReader.ReadInt16();

                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        effectStr += "\n" + dataReader.ReadBytes(10);
                        string sign1 = dataReader.readStringNew(4);
                        string key1 = dataReader.readStringNew(4);

                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);
                        if (dropVersion == 2)
                        {
                            dataReader.ReadBytes(10);
                        }

                        break;
                    case "oglw"://有用:字体的描边！
                        int sizeofRemainItems = dataReader.ReadInt32();
                        int oglwversion = dataReader.ReadInt32();

                        byte[] blurdata = dataReader.ReadBytes(4);

                        outLineDis = Convert.ToInt32(blurdata[1]); //也是小坑，四个故意放在第二个字节 也不说明( ▼-▼ )

                        effectStr += printbytes(blurdata, "blurdata ");

                        //int blurvalue = dataReader.ReadInt32();

                        int intensityPercent = dataReader.ReadInt32();
                         
                        byte outline_r = 0;
                        byte outline_g = 0;
                        byte outline_b = 0;
                        byte outline_a = 0;

                        dataReader.ReadBytes(2);
                        outline_r = dataReader.ReadByte();
                        dataReader.ReadByte();
                        outline_g = dataReader.ReadByte();
                        dataReader.ReadByte();
                        outline_b = dataReader.ReadByte();
                        dataReader.ReadByte();
                        outline_a = dataReader.ReadByte();
                        dataReader.ReadByte();

                        string curSign = dataReader.readStringNew(4);
                        string key = dataReader.readStringNew(4);

                        bool effectEnable = dataReader.ReadBoolean(); //yanruTODO 不可靠，如果整个effect 层 禁用了，子字段可能依然为true，暂时找不到上层effect开关


                        byte opacityPercent = dataReader.ReadByte();//描边透明度

                        if (oglwversion == 2)
                        {
                            byte[] oglwColor2 = dataReader.ReadBytes(10);
                        }


                        if (!effectEnable) //指明了没有描边
                        {
                            TextOutlineColor = new Color(0, 0, 0, 0);
                        }
                        else
                        {
                            TextOutlineColor = new Color(outline_r / 255f, outline_g / 255f, outline_b / 255f, opacityPercent / 255f);
                        }
                        Debug.Log("sizeofRemainItems=" + sizeofRemainItems +
                            ",oglwversion=" + oglwversion +
                        
                            ",intensityPercent=" + intensityPercent +
                            ",curSign=" + curSign +
                            ",key=" + key +

                            ",color_r=" + outline_r +
                            ",color_g=" + outline_g +
                            ",color_b=" + outline_b +
                            ",color_a=" + outline_a
                             + ",effectEnable=" + effectEnable
                             + ",opacityPercent=" + opacityPercent
                             + ",outLineDis="+ outLineDis
                            );
                        break;
                    case "iglw":
                        byte[] testdata5 = dataReader.ReadBytes(47);
                        //effectStr += "\n" + printbytes(testdata5, "iglw");

                        //dataReader.ReadBytes(4);
                        //dataReader.ReadBytes(4);
                        //dataReader.ReadBytes(4);
                        //dataReader.ReadBytes(4);
                        //dataReader.ReadBytes(10);
                        //dataReader.ReadBytes(8);
                        //dataReader.ReadBytes(1);
                        //dataReader.ReadBytes(1);
                        //dataReader.ReadBytes(1);
                        //dataReader.ReadBytes(10);
                        break;
                    case "bevl":

                        int bevelSizeofRemain = dataReader.ReadInt32();//.ReadBytes(4);
                        int bevelversion = dataReader.ReadInt32();
                        //dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);
                        dataReader.ReadBytes(4);

                        dataReader.ReadBytes(8);
                        dataReader.ReadBytes(8);

                        dataReader.ReadBytes(10);
                        dataReader.ReadBytes(10);

                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);
                        dataReader.ReadBytes(1);

                        if (bevelversion == 2)
                        {
                            dataReader.ReadBytes(10);
                            dataReader.ReadBytes(10);
                        }
                        
                        break;
                        //case "sofi":
                        //    int solidSize = dataReader.ReadInt32();//.ReadBytes(4);
                        //    int solidVersion = dataReader.ReadInt32();// (4);
                        //    string solidBlendmode = dataReader.readStringNew(4);//.ReadBytes(4);

                        //    byte[] solidColor = dataReader.ReadBytes(10);
                        //    effectStr += printbytes(solidColor, "sofi solidColor");

                        //    byte opacity = dataReader.ReadByte();

                        //    byte solidenable = dataReader.ReadByte();

                        //    //dataReader.ReadBytes(1);
                        //    //dataReader.ReadBytes(1);

                        //    dataReader.ReadBytes(10);

                        //    Debug.Log("sofi  solidSize=" + solidSize
                        //        + ",solidVersion=" + solidVersion
                        //        + ",solidBlendmode=" + solidBlendmode
                        //        + ",opacity=" + opacity
                        //        + ",solidenable=" + solidenable
                        //        );
                        //    break;
                }
            }

            Debug.Log("effectStr=" + effectStr);
        }


        private string printbytes(byte[] data, string infoHead = "", bool print = false)
        {
            string info = "subinfo, " + infoHead + "=\n";
            for (int index = 0; index < data.Length; index++)
            {
                info += "data[" + index + "]=" + data[index] + "\n";
            }
            if (print)
            {
                Debug.Log(info);
            }
            return info;
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
        /// 文本描边的颜色,透明度=0f表示没有描边
        /// </summary>
        public Color TextOutlineColor { get; private set; }
        public int outLineDis = 1;//描边宽度

        /// <summary>
        /// 对于图片 :图层透明图<1f按图层透明度来，否则，按照填充透明度的值来
        /// </summary>
        public float imageTransparent = 1f;

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

        public static Regex ZH_REG = new Regex(@"[\u4E00-\u9FBF]");


        /// <summary>
        /// Gets or sets the name of the layer.
        /// </summary>
        private string _name = "";
        private static int _readIndex=0;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
            }
        }

        private static string defaultLayerName
        {
            get
            {
                _readIndex++;
                return PsdImporter.NO_NAME_HEAD + _readIndex;
            }
        }

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
        private List<AdjustmentLayerInfo> AdjustmentInfo { get; set; }

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
            byte[] temp = dataReader.ReadBytes(4);//注意：解析的起点是对的，但是终点不对
            //Debug.Log("text layer temp[0]=" + temp[0] + ",temp[1]=" + temp[1] + ",temp[2]=" + temp[2]+",temp[3]=" + temp[3]);
            Text = dataReader.ReadString();// ( true);

            //  read the text justification
            dataReader.Seek("/Justification");
            int justification = dataReader.ReadByte();// - 48;
            Justification = TextJustification.Left;
            if (justification == 1) 
            {
                Justification = TextJustification.Right;
            }
            else if (justification == 2)
            {
                Justification = TextJustification.Center;
            }
            //Debug.Log("text layer justification=" + justification);
            // read the font size
            dataReader.Seek("/FontSize ");
            FontSize = dataReader.ReadFloat();
            //Debug.Log("text layer FontSize=" + FontSize);

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
            FillColor = new Color(red  , green  , blue  , alpha  );
            //Debug.Log("text text="+ Text + ",red=" + red + ",green=" + green + ",blue=" + blue+ ",alpha="+ alpha+",position="+dataReader.BaseStream.Position+ ", byte.MaxValue=" + byte.MaxValue);
             
            //  read the font name
            dataReader.Seek("/FontSet ");

            dataReader.Seek("/Name");
            
            FontName = dataReader.ReadString();
            //Debug.Log("text layer FontName=" + FontName);

            // read the warp style
            dataReader.Seek("warpStyle");
            dataReader.Seek("warpStyle");
            byte [] wrapBytes=  dataReader.ReadBytes(3);

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