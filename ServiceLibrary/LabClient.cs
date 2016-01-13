using System.ComponentModel;

namespace ServiceLibrary
{
    public class LabClient : INotifyPropertyChanged
    {
        private bool active;

        private bool psychoPy;


        public LabClient(int RoomNo, string computerName, int? boothNo, string mac)
        {
            this.RoomNo = RoomNo;
            ComputerName = computerName;
            BoothNo = boothNo;

            BoothName = GetDisplayBoothName(boothNo);

            Mac = mac;
            active = false;
            PsychoPy = false;
            EPrime = false;
            ZTree = false;
            Custom = false;
            chrome = false;
            web = false;
            input = false;
            shareScr = false;
            notify = false;
        }

        public bool Active
        {
            get { return active; }
            set
            {
                if (value != active)
                {
                    active = value;
                    OnPropertyChanged("Active");
                }
            }
        }

        public bool PsychoPy
        {
            get { return psychoPy; }
            set
            {
                if (value != psychoPy)
                {
                    psychoPy = value;
                    OnPropertyChanged("PsychoPy");
                }
            }
        }

        private bool eprime { get; set; }

        public bool EPrime
        {
            get { return eprime; }
            set
            {
                if (value != eprime)
                {
                    eprime = value;
                    OnPropertyChanged("EPrime");
                }
            }
        }

        private bool ztree { get; set; }

        public bool ZTree
        {
            get { return ztree; }
            set
            {
                if (value != ztree)
                {
                    ztree = value;
                    OnPropertyChanged("ZTree");
                }
            }
        }

        private bool custom { get; set; }

        public bool Custom
        {
            get { return custom; }
            set
            {
                if (value != custom)
                {
                    custom = value;
                    OnPropertyChanged("Custom");
                }
            }
        }

        private bool chrome { get; set; }

        public bool Chrome
        {
            get { return chrome; }
            set
            {
                if (value != chrome)
                {
                    chrome = value;
                    OnPropertyChanged("Chrome");
                }
            }
        }

        private bool web { get; set; }

        public bool Web
        {
            get { return web; }
            set
            {
                if (value != web)
                {
                    web = value;
                    OnPropertyChanged("Web");
                }
            }
        }

        private bool shareScr { get; set; }

        public bool ShareScr
        {
            get { return shareScr; }
            set
            {
                if (value != shareScr)
                {
                    shareScr = value;
                    OnPropertyChanged("ShareScr");
                }
            }
        }

        public bool notify { get; set; }
        
        public bool Notify
        {
            get { return notify; }
            set
            {
                if (value != notify)
                {
                    notify = value;
                    OnPropertyChanged("Notify");
                }
            }
        }


        private bool input { get; set; }

        public bool Input
        {
            get { return input; }
            set
            {
                if (value != input)
                {
                    input = value;
                    OnPropertyChanged("Input");
                }
            }
        }

        public int RoomNo { get; set; }
        public string ComputerName { get; set; }
        public int? BoothNo { get; set; }

        public string BoothName { get; set; }

        public string Mac { get; set; }
        public string Ip { get; set; }
        public string CurrentlyLoggedInUser { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
        }


        //converts booth no from bridge cfg value to expected booth name value
        private string GetDisplayBoothName(int? boothNo)
        {
            var name = "";

            if (BoothNo == null)
            {
                return name;
            }

            if (RoomNo == 1)
            {
                name = ((int) BoothNo) + "";
            }
            else
            {
                //start at letter before A (A is 65), so any subsequent numbers will be at range [A-*]
                var startCharValue = 64;
                var numericValue = startCharValue + ((int) BoothNo);
                name = ((char) numericValue).ToString();
            }

            return name;
        }

        public override string ToString()
        {
            return ComputerName;
        }
    }
}