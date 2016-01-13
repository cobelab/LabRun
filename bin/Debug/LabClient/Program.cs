using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Messaging;
using System.Threading;
using System.Windows.Forms;

namespace LabClientService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        private static MessageQueue msMqRequestDelete;
        private static MessageQueue msMqReply;

        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static void StartMSMQService()
        {
            msMqRequestDelete = new MessageQueue("FormatName:Direct=OS:" + Environment.MachineName.ToLower() + "\\private$\\requestdeletequeue");
            if (System.Messaging.MessageQueue.Exists(".\\private$\\requestdeletequeue"))
            {
                msMqRequestDelete.Purge();
                if (msMqRequestDelete != null)
                {
                    msMqRequestDelete.BeginReceive();
                }
                msMqRequestDelete.ReceiveCompleted += new ReceiveCompletedEventHandler(mq_ReceiveCompletedRequestTaskKill);
            }
            else
            {
                msMqRequestDelete = System.Messaging.MessageQueue.Create(".\\private$\\requestdeletequeue", true);
            }


            msMqReply = new MessageQueue("FormatName:Direct=OS:" + Environment.MachineName.ToLower() + "\\private$\\requestqueue");
            if (System.Messaging.MessageQueue.Exists(".\\private$\\requestqueue"))
            {
                {
                    msMqReply.Purge();
                    if (msMqReply != null)
                    {
                        msMqReply.BeginReceive();
                    }
                    msMqReply.ReceiveCompleted += new ReceiveCompletedEventHandler(mq_ReceiveCompletedReply);
                }
            }
            else
            {
                msMqReply = System.Messaging.MessageQueue.Create(".\\private$\\requestqueue", true);
            }
        }

        public static void mq_ReceiveCompletedRequestTaskKill(object sender, ReceiveCompletedEventArgs e)
        {
            //queue that have received a message
            MessageQueue cmq = (MessageQueue)sender;
            try
            {
                //a message we have received (it is already removed from queue)
                System.Messaging.Message msg = cmq.EndReceive(e.AsyncResult);

                var messageLabel = msg.Label;
                var procID = UInt32.Parse(messageLabel);
                KillAllProcessesSpawnedBy(procID);

            }
            catch
            {
            }
            //refresh queue just in case any changes occurred (optional)
            cmq.Refresh();
            //tell MessageQueue to receive next message when it arrives
            cmq.BeginReceive();
        }
        public static void mq_ReceiveCompletedReply(object sender, ReceiveCompletedEventArgs e)
        {
            MessageQueue cmq = (MessageQueue)sender;
            try
            {
                System.Messaging.Message msg = cmq.EndReceive(e.AsyncResult);
                var messageLabel = msg.Label;
                var procID = UInt32.Parse(messageLabel);
                GetProcess(procID);
            }
            catch
            {
            }
            cmq.Refresh();
            cmq.BeginReceive();
        }


        public static void KillAllProcessesSpawnedBy(UInt32 parentProcessId)
        {
            Debug.WriteLine(("Finding processes spawned by process with Id [" + parentProcessId + "]"));

            // NOTE: Process Ids are reused!
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + parentProcessId);

           
            ManagementObjectCollection collection = searcher.Get();
            Thread.Sleep(200);
            if (collection.Count > 0)
            {
                Debug.WriteLine("Killing [" + collection.Count + "] processes spawned by process with Id [" + parentProcessId + "]");
                foreach (var item in collection)
                {
                    UInt32 childProcessId = (UInt32)item["ProcessId"];
                    if ((int)childProcessId != Process.GetCurrentProcess().Id)
                    {
                        KillAllProcessesSpawnedBy(childProcessId);

                        Process childProcess = Process.GetProcessById((int)childProcessId);
                        Debug.WriteLine("Killing child process [" + childProcess.ProcessName + "] with Id [" + childProcessId + "]");
                        childProcess.Kill();
                    }
                }
            }
        } 

        public static void GetProcess(UInt32 parentProcessId)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + parentProcessId);
            Thread.Sleep(200);
            MessageQueue msQ = new MessageQueue
                                           ("FormatName:Direct=OS:ylgw036487\\private$\\replyqueue");
            ManagementObjectCollection collection = searcher.Get();
            Debug.WriteLine("test");
            if (collection.Count > 0)
            {
                // The command was successfull ran on the remote computer (The process exist)
                // Dont send a error message
                SendMessage("The command with pID " + parentProcessId + " was exicted succesful", msQ);
                Debug.WriteLine("Succes");
            }
            else
            {
                Debug.WriteLine("Fail");
                SendMessage("The proces failed op open" + parentProcessId , msQ);
            }
        }

        public static void SendMessage(string message, MessageQueue msgQueue)
        {
            MessageQueue msgLocal = msgQueue;

            System.Messaging.Message msg = new System.Messaging.Message(message);

            MessageQueueTransaction transaction = new MessageQueueTransaction();
            try
            {
                transaction.Begin();
                msgLocal.Send(msg, Environment.MachineName, transaction);
                transaction.Commit();

            }

            catch (System.Exception e)
            {
                // cancel the transaction.
                transaction.Abort();

                // propagate the exception.
                throw e;
            }
            finally
            {
                // dispose of the transaction object.
                transaction.Dispose();
            }
        }
    }
}
