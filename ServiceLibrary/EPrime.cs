namespace ServiceLibrary
{
    public class EPrime : TestApp
    {
        public EPrime() : base("EPrime")
        {
            ApplicationExecutableName = service.Config.EPrime;
            Extension = "ebs2";
            ExtensionDescription = "E-Prime Test Files (*.ebs2)|*.ebs2";

            resultExts.Add("edat2");
            resultExts.Add("xml");
            resultExts.Add("txt");
        }
    }
}