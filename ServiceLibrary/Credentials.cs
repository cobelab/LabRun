namespace ServiceLibrary
{
    public class Credentials
    {
        public Credentials(string domainName, string userName, string password)
        {
            DomainName = domainName;
            UserName = userName;
            Password = password;
            if (domainName != "")
            {
                DomainSlashUser = domainName + @"\" + userName;
            }
            else
            {
                DomainSlashUser = userName;
            }

            UserAtDomain = userName + @"@" + domainName;
        }

        public string DomainName { get; }
        public string UserName { get; }
        public string Password { get; }
        public string DomainSlashUser { get; }
        public string UserAtDomain { get; }
    }
}