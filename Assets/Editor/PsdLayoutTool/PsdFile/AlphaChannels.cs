namespace PhotoshopFile
{
    /// <summary>
    /// The names of the alpha channels
    /// </summary>
    public class AlphaChannels : ImageResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaChannels" /> class.
        /// </summary>
        /// <param name="imgRes">The image resource.</param>
        public AlphaChannels(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryReverseReader dataReader = imgRes.DataReader;
            while (dataReader.BaseStream.Length - dataReader.BaseStream.Position > 0L)
            {
                // read the length of the string
                byte length = dataReader.ReadByte();

                // read the string
                dataReader.ReadChars(length);
            }

            dataReader.Close();
        }
    }
}
