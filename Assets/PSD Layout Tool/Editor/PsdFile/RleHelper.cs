using System.IO;

namespace PhotoshopFile
{
    internal class RleHelper
    {
        public static int EncodedRow(Stream stream, byte[] imgData, int startIdx, int columns)
        {
            long position = stream.Position;
            RlePacketStateMachine packetStateMachine = new RlePacketStateMachine(stream);
            for (int index = 0; index < columns; ++index)
                packetStateMachine.Push(imgData[index + startIdx]);
            packetStateMachine.Flush();
            return (int)(stream.Position - position);
        }

        public static void DecodedRow(Stream stream, byte[] imgData, int startIdx, int columns)
        {
            int num1 = 0;
        label_11:
            while (num1 < columns)
            {
                int num2 = (byte)stream.ReadByte();
                if (num2 < 128)
                {
                    int num3 = num2 + 1;
                    while (true)
                    {
                        if (num3 != 0 && startIdx + num1 < imgData.Length)
                        {
                            byte num4 = (byte)stream.ReadByte();
                            imgData[startIdx + num1] = num4;
                            ++num1;
                            --num3;
                        }
                        else
                            goto label_11;
                    }
                }
                else if (num2 > 128)
                {
                    int num3 = (num2 ^ byte.MaxValue) + 2;
                    byte num4 = (byte)stream.ReadByte();
                    while (true)
                    {
                        if (num3 != 0 && startIdx + num1 < imgData.Length)
                        {
                            imgData[startIdx + num1] = num4;
                            ++num1;
                            --num3;
                        }
                        else
                            goto label_11;
                    }
                }
            }
        }

        private class RlePacketStateMachine
        {
            private readonly byte[] m_packetValues = new byte[128];
            private bool m_rlePacket;
            private int packetLength;
            private readonly Stream m_stream;

            internal RlePacketStateMachine(Stream stream)
            {
                m_stream = stream;
            }

            internal void Flush()
            {
                m_stream.WriteByte(!m_rlePacket ? (byte)(packetLength - 1) : (byte)-(packetLength - 1));
                m_stream.Write(m_packetValues, 0, m_rlePacket ? 1 : packetLength);
                packetLength = 0;
            }

            internal void Push(byte color)
            {
                if (packetLength == 0)
                {
                    m_rlePacket = false;
                    m_packetValues[0] = color;
                    packetLength = 1;
                }
                else if (packetLength == 1)
                {
                    m_rlePacket = color == m_packetValues[0];
                    m_packetValues[1] = color;
                    packetLength = 2;
                }
                else if (packetLength == m_packetValues.Length)
                {
                    Flush();
                    Push(color);
                }
                else if (packetLength >= 2 && m_rlePacket && color != m_packetValues[packetLength - 1])
                {
                    Flush();
                    Push(color);
                }
                else if (packetLength >= 2 && m_rlePacket && color == m_packetValues[packetLength - 1])
                {
                    ++packetLength;
                    m_packetValues[packetLength - 1] = color;
                }
                else if (packetLength >= 2 && !m_rlePacket && color != m_packetValues[packetLength - 1])
                {
                    ++packetLength;
                    m_packetValues[packetLength - 1] = color;
                }
                else
                {
                    if (packetLength < 2 || m_rlePacket || color != m_packetValues[packetLength - 1])
                        return;
                    --packetLength;
                    Flush();
                    Push(color);
                    Push(color);
                }
            }
        }
    }
}
