using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace modprocessor.libs
{
    public class BigEndianBinaryWriter : BinaryWriter
    {

        public BigEndianBinaryWriter(MemoryStream memoryStream) : base(memoryStream)
        {

        }


        public void WriteAscii(string text, int length)
        {
            if (text.Length < length)
            {
                text = text.PadRight(length);
            }
            var trimmedText = text.Substring(0, length);
            var convertedText = Encoding.ASCII.GetBytes(trimmedText);
            convertedText[length - 1] = (byte)0;
            Write(convertedText);
        }


        public void WriteInt16(Int16 value)
        {
            var bytes = BitConverter.GetBytes(value);
            Write(bytes[1]);
            Write(bytes[0]);
        }


        public void WriteInt32(Int32 value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            Write(bytes);
        }


        public void ClearBytes(int length)
        {
            for (int i = 0; i < length; i++)
            {
                Write((byte)0);
            }
        }

        public byte[] ToArray()
        {
            return ((MemoryStream)BaseStream).ToArray();
        }


        public long Position => BaseStream.Position;


    }
}
