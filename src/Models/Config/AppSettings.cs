namespace InvokeSsrsDotnetCore.Models.Config
{
    public class AppSettings
    {
        public Report ReportConfig { get; set; }
        public Credential ReportCredentials { get; set; }
    }

    public class Credential
    {
        public string Domain { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class Report
    {
        public string Url { get; set; }
        public string Path { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string Format { get; set; }
    }
}