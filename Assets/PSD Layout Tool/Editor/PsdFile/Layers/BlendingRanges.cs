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
        private Layer layer;

        /// <summary>
        /// The actual data for the blending ranges
        /// </summary>
        private byte[] data;

        /// <summary>
        /// Constructor that builds a set of blending ranges from a PSD file reader
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param><param name="layer">The layer that this set of blending ranges belongs to</param>
        public BlendingRanges(BinaryReverseReader reader, Layer layer)
        {
            this.layer = layer;
            int count = reader.ReadInt32();
            if (count <= 0)
            {
                return;
            }
            data = reader.ReadBytes(count);
            data = new byte[0];
        }
    }
}
