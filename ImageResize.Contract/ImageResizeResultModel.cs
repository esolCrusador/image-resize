namespace ImageResize.Contract
{
    public class ImageResizeResultModel
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Size { get; set; }
        public int Quality { get; set; }
        public ImageResizeResultModel()
        {

        }
        public ImageResizeResultModel(int width, int height, int size, int quality)
        {
            Width = width;
            Height = height;
            Size = size;
            Quality = quality;
        }
    }
}
