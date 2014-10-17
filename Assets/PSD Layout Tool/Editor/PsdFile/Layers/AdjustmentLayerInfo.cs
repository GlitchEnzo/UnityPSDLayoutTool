using System.IO;

namespace PhotoshopFile
{
    /// <summary>
    /// The adjustment information for a layer
    /// </summary>
    public class AdjusmentLayerInfo
    {
        /// <summary>
        /// The layer to which this info belongs
        /// </summary>
        internal Layer Layer { get; private set; }

        /// <summary>
        /// Gets/Sets the key for the adjustment info - this is usually a 4 character code
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets/Sets the actual data contained within the adjustment info
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets a reader setup to read the actual data of this adjustment info
        /// </summary>
        public BinaryReverseReader DataReader
        {
            get { return new BinaryReverseReader(new MemoryStream(Data)); }
        }

        /// <summary>
        /// Constructor that builds an empty adjustment info
        /// </summary>
        /// <param name="key">The key code assigned to this adjustment info</param><param name="layer">The layer that this adjustment info belongs to</param>
        public AdjusmentLayerInfo(string key, Layer layer)
        {
            Key = key;
            Layer = layer;
            layer.AdjustmentInfo.Add(this);
        }

        /// <summary>
        /// Constructor that builds an adjustment info from a PSD file reader
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param><param name="layer">The layer that this adjustment info belongs to</param>
        public AdjusmentLayerInfo(BinaryReverseReader reader, Layer layer)
        {
            Layer = layer;
            if (new string(reader.ReadChars(4)) != "8BIM")
                throw new IOException("Could not read an image resource");
            Key = new string(reader.ReadChars(4));
            if (Key == "lfx2" || Key == "lrFX")
                layer.HasEffects = true;
            uint num = reader.ReadUInt32();
            Data = reader.ReadBytes((int)num);
        }
    }
}
