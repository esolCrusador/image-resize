namespace ImageResize.Contract
{
    public class ImageResizeResultModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Size { get; private set; }
        public int Quality { get; private set; }

        public ImageResizeResultModel(int width, int height, int size, int quality)
        {
            Width = width;
            Height = height;
            Size = size;
            Quality = quality;
        }
    }
}
