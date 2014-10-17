using System;
using System.IO;

namespace PhotoshopFile
{
    /// <summary>
    /// Summary description for ImageResource.
    /// </summary>
    public class ImageResource
    {
        private string m_name = string.Empty;
        private string m_osType = string.Empty;
        private short m_id;
        private byte[] m_data;

        public short ID
        {
            get
            {
                return m_id;
            }
            set
            {
                m_id = value;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }

        public byte[] Data
        {
            get
            {
                return m_data;
            }
            set
            {
                m_data = value;
            }
        }

        public string OSType
        {
            get
            {
                return m_osType;
            }
            set
            {
                m_osType = value;
            }
        }

        public BinaryReverseReader DataReader
        {
            get
            {
                return new BinaryReverseReader(new MemoryStream(m_data));
            }
        }

        public ImageResource()
        {
        }

        public ImageResource(short id)
        {
            m_id = id;
        }

        public ImageResource(ImageResource imgRes)
        {
            m_id = imgRes.m_id;
            m_name = imgRes.m_name;
            m_data = new byte[imgRes.m_data.Length];
            imgRes.m_data.CopyTo(m_data, 0);
        }

        public ImageResource(BinaryReverseReader reader)
        {
            m_osType = new string(reader.ReadChars(4));
            if (m_osType != "8BIM" && m_osType != "MeSa")
                throw new InvalidOperationException("Could not read an image resource");
            m_id = reader.ReadInt16();
            m_name = reader.ReadPascalString();
            uint num1 = reader.ReadUInt32();
            m_data = reader.ReadBytes((int)num1);
            if (reader.BaseStream.Position % 2L != 1L)
                return;
            reader.ReadByte();
        }

        protected virtual void StoreData()
        {
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", (ResourceIDs)m_id, m_name);
        }
    }
}
