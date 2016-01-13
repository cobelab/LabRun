using System.Collections.Generic;
using System.IO;
using RDPCOMAPILib;

namespace ServiceLibrary
{
    internal class ScreenShare
    {
        private static ScreenShare screenShare;
        private bool firstCall = true;
        private readonly RDPSession iRDSession = new RDPSession();

        private ScreenShare()
        {
        }

        public static ScreenShare getInstance()
        {
            if (screenShare == null)
                screenShare = new ScreenShare();
            return screenShare;
        }

        public void Start(List<LabClient> clients, string rdsKeyLocation)
        {
            if (firstCall)
            {
                Service.GetInstance().TransferAndRun(clients);


                iRDSession.OnAttendeeConnected += Incoming;

                iRDSession.Open();
                iRDSession.Resume();
                IRDPSRAPIInvitation pInvitation =
                    iRDSession.Invitations.CreateInvitation("WinPresenter", "PresentationGroup", "", 500);
                var invitationString = pInvitation.ConnectionString;
                var directoryName = Path.GetDirectoryName(rdsKeyLocation);
                if ((directoryName.Length > 0) && (!Directory.Exists(directoryName)))
                {
                    Directory.CreateDirectory(directoryName);
                }
                var file = new StreamWriter(rdsKeyLocation);

                file.WriteLine(invitationString);
                file.Close();
                firstCall = false;
            }
            else
            {
                Service.GetInstance().TransferAndRun(clients);
                iRDSession.Resume();
            }
        }

        public void Stop(List<LabClient> clients)
        {
            iRDSession.Pause();

            foreach (var client in clients)
            {
                Service.GetInstance().KillRemoteProcess(client.ComputerName, "scr-viewer.exe");
            }
        }

        private void Incoming(object Guest)
        {
            var MyGuest = (IRDPSRAPIAttendee) Guest;
            MyGuest.ControlLevel = CTRL_LEVEL.CTRL_LEVEL_VIEW;
        }
    }
}