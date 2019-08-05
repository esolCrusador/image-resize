using Newtonsoft.Json;
using System.IO;

namespace ImageResize.Logic
{
    public class OutputImageParameters
    {
        [JsonIgnore]
        private MemoryStream _outputStream;
        private OutputImageParameters(bool resized)
        {
            Resized = resized;
        }
        public OutputImageParameters(int resultWidth, int resultHeight, int quality)
            : this(true)
        {
            Width = resultWidth;
            Height = resultHeight;
            Quality = quality;
        }

        [JsonIgnore]
        public MemoryStream OutputStream
        {
            get { return _outputStream; }
            set
            {
                _outputStream = value;

                Size = (int)_outputStream.Length;
                _outputStream.Position = 0;
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Size { get; private set; }
        public int Quality { get; private set; }
        public bool Resized { get; private set; }

        public static readonly OutputImageParameters NotResized = new OutputImageParameters(false);
    }
}
