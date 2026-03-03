namespace PhotoshopFile
{
    /// <summary>
    /// Represents the resolution information.
    /// </summary>
    public class ResolutionInfo : ImageResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolutionInfo"/> class using the <see cref="ImageResource"/>.
        /// </summary>
        /// <param name="imgRes">The image resource to use.</param>
        public ResolutionInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryReverseReader dataReader = imgRes.DataReader;

            // read horizontal resolution
            dataReader.ReadInt16();

            // read horizontal resolution units (1=pixels per inch, 2=pixels per centimeter)
            dataReader.ReadInt32();

            // read the width units (1=inches, 2=cm, 3=pt, 4=picas, 5=columns)
            dataReader.ReadInt16();

            // read vertical resolution
            dataReader.ReadInt16();

            // read vertical resolution units (1=pixels per inch, 2=pixels per centimeter)
            dataReader.ReadInt32();

            // read the height units (1=inches, 2=cm, 3=pt, 4=picas, 5=columns)
            dataReader.ReadInt16();

            dataReader.Close();
        }
    }
}
