namespace ServiceLibrary
{
    public class WindowSize
    {
        public WindowSize(string name, int? width, int? height)
        {
            Name = name;
            Width = width;
            Height = height;
        }

        public string Name { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? XPos { get; set; }
        public int? YPos { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}