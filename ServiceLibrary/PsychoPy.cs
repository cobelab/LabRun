namespace ServiceLibrary
{
    public class PsychoPy : TestApp
    {
        public PsychoPy()
            : base("PsychoPy")
        {
            ApplicationExecutableName = service.Config.Psychopy;
            RunWithLogsScript = "run_with_logs.py";

            Extension = "py";
            ExtensionDescription = "Python files (*.py)|*.py|PsychoPy Test Files (*.psyexp)|*.psyexp";

            resultExts.Add("psydat");
            resultExts.Add("csv");
            resultExts.Add("log");
        }

        //script should be in the same location where binaries are
        public string RunWithLogsScript { get; set; }
    }
}