using System.IO;

namespace ImageResize
{
    public class OutputImageParameters
    {
        public OutputImageParameters(int originalWidth, int originalHeight, int originalSize)
        {
            OriginalWidth = originalWidth;
            OriginalHeight = originalHeight;
            OriginalSize = originalSize;

            Resized = false;
        }
        public OutputImageParameters(int originalWidth, int originalHeight, int originalSize, Stream outputStream, int resultWidth, int resultHeight, int resultSize)
            :this(originalWidth, originalHeight, originalSize)
        {
            OutputStream = outputStream;
            ResultWidth = resultWidth;
            ResultHeight = resultHeight;
            ResultSize = resultSize;

            Resized = true;
        }
        public Stream OutputStream { get; private set; }
        public int OriginalWidth { get; private set; }
        public int OriginalHeight { get; private set; }
        public int OriginalSize { get; private set; }
        public int ResultWidth { get; private set; }
        public int ResultHeight { get; private set; }
        public int ResultSize { get; private set; }
        public bool Resized { get; private set; }
    }
}
