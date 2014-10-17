namespace PhotoshopFile
{
    /// <summary>
    /// Summary description for ResolutionInfo.
    /// </summary>
    public class ResolutionInfo : ImageResource
    {
        public short HRes { get; set; }

        public short VRes { get; set; }

        public ResUnit HResUnit { get; set; }

        public ResUnit VResUnit { get; set; }

        public Unit WidthUnit { get; set; }

        public Unit HeightUnit { get; set; }

        public ResolutionInfo()
        {
            ID = 1005;
        }

        public ResolutionInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryReverseReader dataReader = imgRes.DataReader;
            HRes = dataReader.ReadInt16();
            HResUnit = (ResUnit)dataReader.ReadInt32();
            WidthUnit = (Unit)dataReader.ReadInt16();
            VRes = dataReader.ReadInt16();
            VResUnit = (ResUnit)dataReader.ReadInt32();
            HeightUnit = (Unit)dataReader.ReadInt16();
            dataReader.Close();
        }

        /// <summary>
        /// 1=pixels per inch, 2=pixels per centimeter
        /// </summary>
        public enum ResUnit
        {
            PxPerInch = 1,
            PxPerCent = 2,
        }

        /// <summary>
        /// 1=in, 2=cm, 3=pt, 4=picas, 5=columns
        /// </summary>
        public enum Unit
        {
            In = 1,
            Cm = 2,
            Pt = 3,
            Picas = 4,
            Columns = 5,
        }
    }
}
