namespace PhotoshopFile
{
    /// <summary>
    /// The blending ranges for a layer
    /// </summary>
    public class BlendingRanges
    {
        /// <summary>
        /// Constructor that builds a set of blending ranges from a PSD file reader
        /// </summary>
        /// <param name="reader">The reader containing the PSD file data</param>
        public BlendingRanges(BinaryReverseReader reader)
        {
            int count = reader.ReadInt32();
            if (count <= 0)
            {
                return;
            }

            // read the data
            reader.ReadBytes(count);
        }
    }
}
