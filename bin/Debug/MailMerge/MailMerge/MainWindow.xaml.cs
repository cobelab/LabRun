using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using Microsoft.Win32;
using Microsoft.Office.Interop.Word;
using Application = System.Windows.Application;
using Window = System.Windows.Window;
using System.IO;
using System.Reflection;

namespace MailMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        List<Participant> adat = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        Microsoft.Office.Interop.Word.Application wrdApp;
        Microsoft.Office.Interop.Word._Document wrdDoc;
        Object oMissing = System.Reflection.Missing.Value;
        Object oFalse = false;



        private void InsertLines(int LineNum)
        {
            int iCount;
            for (iCount = 1; iCount <= LineNum; iCount++)
            {
                wrdApp.Selection.TypeParagraph();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "ZTree Pay Files (*.pay)|*.pay|All Files|*.*";      
            openFileDialog1.Multiselect = true;
            bool? userClickedOK = openFileDialog1.ShowDialog();

            if (userClickedOK == true)
            {
                readPayFile(openFileDialog1.FileName);
                Microsoft.Office.Interop.Word.Selection wrdSelection;
                Microsoft.Office.Interop.Word.MailMerge wrdMailMerge;

                string StrToAdd;

                wrdApp = new Microsoft.Office.Interop.Word.Application();
                wrdApp.Visible = true;

                wrdDoc = wrdApp.Documents.Add(ref oMissing, ref oMissing,
                    ref oMissing, ref oMissing);
                wrdDoc.Select();

                wrdSelection = wrdApp.Selection;



                foreach (Participant partip in this.adat)
                {

                    wrdSelection.ParagraphFormat.Alignment =
                        Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphRight;
                    wrdSelection.Font.Size = 26;
                    wrdSelection.Font.Name = "Arial";
                    wrdSelection.TypeText("Receipt \r\n Seat number: " + partip.boothno);
                    wrdSelection.Font.Size = 10;
                    wrdSelection.Font.Name = "Times New Roman";

                    InsertLines(1);
                    wrdSelection.ParagraphFormat.Alignment =
                       Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphLeft;

                    StrToAdd = "\r\n COGNITION AND BEHAVIOR LAB";

                    try
                    {                  
                        string imgLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "1.png");
                        wrdSelection.InlineShapes.AddPicture(imgLocation);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Image file (1.png) not found!");
                    }
                    wrdSelection.TypeText(StrToAdd);
     
                    wrdSelection.TypeText("\r\nName: " + partip.name);
                    wrdSelection.TypeText("\r\nCPR number: " + partip.cpr);
                    wrdSelection.TypeText("\r\nYour earnings in today’s experiment: " + partip.profit + " kr. \r\n");

                    wrdSelection.ParagraphFormat.Alignment =
                        Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;

                    wrdSelection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;
                    StrToAdd = "Aarhus University will automatically transfer the amount you earn into your NemKonto (for this we need your CPR number). This is simply your existing bank account, into which all payments from the public sector flow (e.g. tax refunds or SU student grants). Alexander Koch and his team will start registering the payments with the administration of Aarhus University this week. Then the administrative process might take between 2-6 weeks. You can contact Alexander Koch by email (akoch@econ.au.dk) if you want information on the payment process.";
                    wrdSelection.TypeText(StrToAdd);
                    wrdSelection.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphJustify;
                    InsertLines(1);
                    StrToAdd = "According to Danish law, Aarhus University reports payments to the tax authorities. Please note that, depending on your personal income tax rate, taxes will be deducted from the amount of money you earn in this study. That is, the amount you will receive might be lower than the pre-tax earnings stated above.";
                    wrdSelection.TypeText(StrToAdd);
                    InsertLines(1);

                    wrdSelection.ParagraphFormat.Alignment =
                        Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphRight;

                    Object objDate = "dddd, MMMM dd, yyyy";
                    wrdSelection.InsertDateTime(ref objDate, ref oFalse, ref oMissing,
                        ref oMissing, ref oMissing);

                    wrdSelection.InlineShapes.AddHorizontalLineStandard();
                    wrdSelection.InsertBreak(Microsoft.Office.Interop.Word.WdBreakType.wdPageBreak);
                }


            }
        }

        private void readPayFile(string path)
        {
            string line = "";
            int subject = -1;
            string name = "";
            string cpr = "";
            decimal pay;
            this.adat = new List<Participant>();

            using (System.IO.StreamReader file = new System.IO.StreamReader(path))
            {
                while ((line = file.ReadLine()) != null)
                {
                    string[] temp = line.Split('	');
                    try
                    {
                        subject = Int32.Parse(temp[0]);
                        string[] temp2 = temp[3].Split(',');
                        name = temp2[0];
                        cpr = temp2[1];
                        pay = decimal.Parse(temp[4], CultureInfo.InvariantCulture);
                        Participant particip = new Participant(name, cpr, subject, pay);
                        adat.Add(particip);


                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}
