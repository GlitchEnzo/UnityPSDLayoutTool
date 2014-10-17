namespace PhotoshopFile
{
    /// <summary>
    /// The blending ranges for a layer
    /// </summary>
    public class BlendingRanges
    {
        /// <summary>
        /// The layer to which this channel belongs
        /// </summary>
        public Layer Layer { get; private set; }

        /// <summary>
        /// The actual data for the blending ranges
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Constructor that builds an empty set of blending ranges
        /// </summary>
        /// <param name="layer">The layer that this set of blending ranges belongs to</param>
        public BlendingRanges(Layer layer)
        {
            Layer = layer;
            Layer.BlendingRangesData = this;
            Data = new byte[0];
        }

        /// <summary>
        /// Constructor that builds a set of blending ranges from a PSD file reader
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param><param name="layer">The layer that this set of blending ranges belongs to</param>
        public BlendingRanges(BinaryReverseReader reader, Layer layer)
        {
            Layer = layer;
            int count = reader.ReadInt32();
            if (count <= 0)
                return;
            Data = reader.ReadBytes(count);
            Data = new byte[0];
        }
    }
}
