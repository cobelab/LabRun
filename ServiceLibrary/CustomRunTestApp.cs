using System.Collections.Generic;

namespace ServiceLibrary
{
    public class CustomRunTestApp : TestApp
    {
        public CustomRunTestApp(List<string> extensions)
            : base("Custom Run")
        {
            foreach (var ext in extensions)
            {
                resultExts.Add(ext);
            }
        }
    }
}