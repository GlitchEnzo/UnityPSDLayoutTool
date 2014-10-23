using System.Collections.Generic;

namespace PhotoshopFile
{
    /// <summary>
    /// The names of the alpha channels
    /// </summary>
    public class AlphaChannels : ImageResource
    {
        private List<string> channelNames = new List<string>();

        public AlphaChannels(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryReverseReader dataReader = imgRes.DataReader;
            while (dataReader.BaseStream.Length - dataReader.BaseStream.Position > 0L)
            {
                byte num = dataReader.ReadByte();
                string str = new string(dataReader.ReadChars(num));
                if (str.Length > 0)
                {
                    channelNames.Add(str);
                }
            }
            dataReader.Close();
        }
    }
}
