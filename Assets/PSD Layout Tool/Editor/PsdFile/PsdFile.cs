using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PhotoshopFile
{

    /// <summary>
    /// A class that represents a loaded PSD file.
    /// </summary>
    public class PsdFile
    {
        /// <summary>
        /// The channels.
        /// </summary>
        private short channels;

        /// <summary>
        /// The height of the entire PSD file, in pixels.
        /// </summary>
        private int height;

        /// <summary>
        /// The width of the entire PSD file, in pixels.
        /// </summary>
        private int width;

        /// <summary>
        /// The depth of the PSD file.
        /// </summary>
        private int depth;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsdFile"/> class.
        /// </summary>
        /// <param name="fileName">The filepath of the PSD file to open.</param>
        public PsdFile(string fileName)
        {
            Category = string.Empty;
            Version = 1;
            Layers = new List<Layer>();
            ImageResources = new List<ImageResource>();

            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BinaryReverseReader reader = new BinaryReverseReader(fileStream);
                LoadHeader(reader);
                LoadColorModeData(reader);
                LoadImageResources(reader);
                LoadLayerAndMaskInfo(reader);
                LoadImage(reader);
            }
        }

        /// <summary>
        /// Gets the color mode data.
        /// If ColorMode is ColorModes.Indexed, the following 768 bytes will contain
        /// a 256-color palette. If the ColorMode is ColorModes.Duotone, the data
        /// following presumably consists of screen parameters and other related information.
        /// Unfortunately, it is intentionally not documented by Adobe, and non-Photoshop
        /// readers are advised to treat duotone images as gray-scale images.
        /// </summary>
        public byte[] ColorModeData { get; private set; }

        /// <summary>
        /// Gets the height of the image in pixels.
        /// </summary>
        public int Height
        {
            get { return height; }
        }

        /// <summary>
        /// Gets the width of the image in pixels.
        /// </summary>
        public int Width
        {
            get { return width; }
        }

        /// <summary>
        /// Gets the number of bits per channel. Supported values are 1, 8, and 16.
        /// </summary>
        public int Depth
        {
            get { return depth; }
        }

        /// <summary>
        /// Gets the color mode of the file.
        /// </summary>
        public ColorModes ColorMode { get; private set; }

        /// <summary>
        /// Gets a list of all layers contained within the PSD file
        /// </summary>
        public List<Layer> Layers { get; private set; }

        /// <summary>
        /// Gets or sets the meta-data of the PSD file in XML format
        /// </summary>
        private XDocument MetaData { get; set; }

        /// <summary>
        /// Gets or sets the category setting of the PSD file from the meta-data.
        /// </summary>
        private string Category { get; set; }

        /// <summary>
        /// Gets or sets the version of the PSD file.  It should ALWAYS be 1.
        /// </summary>
        private short Version { get; set; }

        /// <summary>
        /// Gets or sets the Image resource blocks for the file
        /// </summary>
        private List<ImageResource> ImageResources { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first alpha channel contains transparency data for the merged result.
        /// If True, then number of layers is absolute value, and the first alpha channel contains the transparency data for the merged result.
        /// Otherwise, it is False
        /// </summary>
        private bool AbsoluteAlpha { get; set; }

        /// <summary>
        /// Gets or sets the 2D array containing the ImageData
        /// </summary>
        private byte[][] ImageData { get; set; }

        /// <summary>
        /// Gets or sets the ImageCompression
        /// </summary>
        private ImageCompression ImageCompression { get; set; }

        /// <summary>
        /// Loads the header data from a PSD file
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param>
        private void LoadHeader(BinaryReverseReader reader)
        {
            if (new string(reader.ReadChars(4)) != "8BPS")
            {
                UnityEngine.Debug.LogError("The given stream is not a valid PSD file");
                throw new IOException("The given stream is not a valid PSD file");
            }

            Version = reader.ReadInt16();
            if (Version != 1)
            {
                UnityEngine.Debug.LogError("The PSD file has an invalid version");
                throw new IOException("The PSD file has an invalid version");
            }

            reader.BaseStream.Position += 6L;
            channels = reader.ReadInt16();
            height = reader.ReadInt32();
            width = reader.ReadInt32();
            depth = reader.ReadInt16();
            ColorMode = (ColorModes)reader.ReadInt16();
        }

        /// <summary>
        /// Reads the color mode data from the reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the color mode data.</param>
        private void LoadColorModeData(BinaryReverseReader reader)
        {
            uint num = reader.ReadUInt32();
            if (num <= 0U)
            {
                return;
            }

            ColorModeData = reader.ReadBytes((int)num);
        }

        /// <summary>
        /// Reads the image resources from the reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the image resources.</param>
        private void LoadImageResources(BinaryReverseReader reader)
        {
            ImageResources.Clear();
            uint num = reader.ReadUInt32();
            if (num <= 0U)
            {
                return;
            }

            long position = reader.BaseStream.Position;
            while (reader.BaseStream.Position - position < num)
            {
                ImageResource imgRes = new ImageResource(reader);
                switch ((ResourceIDs)imgRes.ID)
                {
                    case ResourceIDs.XMLInfo:
                        MetaData = XDocument.Load(XmlReader.Create(new MemoryStream(imgRes.Data)));
                        IEnumerable<XElement> source = MetaData.Descendants(XName.Get("Category", "http://ns.adobe.com/photoshop/1.0/"));
                        if (source.Any())
                        {
                            Category = source.First().Value;
                        }

                        break;

                    case ResourceIDs.ResolutionInfo:
                        imgRes = new ResolutionInfo(imgRes);
                        break;

                    case ResourceIDs.AlphaChannelNames:
                        imgRes = new AlphaChannels(imgRes);
                        break;
                }

                ImageResources.Add(imgRes);
            }

            reader.BaseStream.Position = position + num;
        }

        /// <summary>
        /// Reads the layer and mask information from the reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the layer and mask info.</param>
        private void LoadLayerAndMaskInfo(BinaryReverseReader reader)
        {
            uint num = reader.ReadUInt32();
            if (num <= 0U)
            {
                return;
            }

            long position = reader.BaseStream.Position;
            LoadLayers(reader);
            LoadGlobalLayerMask(reader);
            reader.BaseStream.Position = position + num;
        }

        /// <summary>
        /// Reads all of the layers from the reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the layers.</param>
        private void LoadLayers(BinaryReverseReader reader)
        {
            uint num1 = reader.ReadUInt32();
            if (num1 <= 0U)
            {
                return;
            }

            long position = reader.BaseStream.Position;
            short num2 = reader.ReadInt16();
            if (num2 < 0)
            {
                AbsoluteAlpha = true;
                num2 = Math.Abs(num2);
            }

            Layers.Clear();
            if (num2 == 0)
            {
                return;
            }

            for (int index = 0; index < (int)num2; ++index)
            {
                Layers.Add(new Layer(reader, this));
            }

            foreach (Layer layer in Layers)
            {
                foreach (Channel channel in layer.Channels)
                {
                    if (channel.ID != -2)
                    {
                        channel.LoadPixelData(reader);
                    }
                }

                layer.MaskData.LoadPixelData(reader);
            }

            if (reader.BaseStream.Position % 2L == 1L)
            {
                reader.ReadByte();
            }

            reader.BaseStream.Position = position + num1;
        }

        /// <summary>
        /// Reads the global layer mask from the reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the global layer mask.</param>
        private void LoadGlobalLayerMask(BinaryReverseReader reader)
        {
            uint num = reader.ReadUInt32();
            if (num <= 0U)
            {
                return;
            }

            // read the global mask data
            reader.ReadBytes((int)num);
        }

        /// <summary>
        /// Reads an image from the reader.
        /// </summary>
        /// <param name="reader">The reader to use to read the image.</param>
        private void LoadImage(BinaryReverseReader reader)
        {
            ImageCompression = (ImageCompression)reader.ReadInt16();
            ImageData = new byte[channels][];
            if (ImageCompression == ImageCompression.Rle)
            {
                reader.BaseStream.Position += height * channels * 2;
            }

            int columns = 0;
            switch (depth)
            {
                case 1:
                    columns = width;
                    break;
                case 8:
                    columns = width;
                    break;
                case 16:
                    columns = width * 2;
                    break;
            }

            for (int index1 = 0; index1 < (int)channels; ++index1)
            {
                ImageData[index1] = new byte[height * columns];
                switch (ImageCompression)
                {
                    case ImageCompression.Raw:
                        reader.Read(ImageData[index1], 0, ImageData[index1].Length);
                        break;
                    case ImageCompression.Rle:
                        for (int index2 = 0; index2 < height; ++index2)
                        {
                            int startIdx = index2 * width;
                            RleHelper.DecodedRow(reader.BaseStream, ImageData[index1], startIdx, columns);
                        }

                        break;
                }
            }
        }
    }
}
