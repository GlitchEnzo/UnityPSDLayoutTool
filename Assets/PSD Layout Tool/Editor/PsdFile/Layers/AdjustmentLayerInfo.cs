using System.IO;

namespace PhotoshopFile
{
    /// <summary>
    /// The adjustment information for a layer
    /// </summary>
    public class AdjustmentLayerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdjustmentLayerInfo"/> class.
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param>
        /// <param name="layer">The layer that this adjustment info belongs to</param>
        public AdjustmentLayerInfo(BinaryReverseReader reader, Layer layer)
        {
            if (new string(reader.ReadChars(4)) != "8BIM")
            {
                throw new IOException("Could not read an image resource");
            }

            Key = new string(reader.ReadChars(4));
            if (Key == "lfx2" || Key == "lrFX")
            {
                layer.HasEffects = true;
            }

            uint length = reader.ReadUInt32();
            Data = reader.ReadBytes((int)length);
        }

        /// <summary>
        /// Gets the key for the adjustment info - this is usually a 4 character code
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets a reader setup to read the actual data of this adjustment info
        /// </summary>
        public BinaryReverseReader DataReader
        {
            get { return new BinaryReverseReader(new MemoryStream(Data)); }
        }

        /// <summary>
        /// Gets or sets the actual data contained within the adjustment info
        /// </summary>
        private byte[] Data { get; set; }
    }
}
