using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Archive_Extractor
{
    public partial class Form1 : Form
    {
        // Field to hold file 
        private FileStream fsCurrentFile = null;
        // Field to hold the current path
        private string strCurrentPath = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void miQuit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void miOpen_Click(object sender, EventArgs e)
        {
            // Open the open file dialog and store the result it returns
            DialogResult drResult = ofdOpenFile.ShowDialog();
            // Open and display file information if the dialog result is "OK"
            if (drResult == DialogResult.OK)
            {
                // Open the file
                fsCurrentFile = File.Open(ofdOpenFile.FileName, FileMode.Open, FileAccess.Read);
                // Save the file path on the current path variable
                strCurrentPath = ofdOpenFile.FileName;
                // Set the file position pointer to the beginning of the offset (4 bytes from end)
                fsCurrentFile.Seek(-4, SeekOrigin.End);
                // Get the offset
                int iOffset = FourLEBytesToInt(fsCurrentFile);
                // The offset section is 4 bytes long. Add this to the bytes read
                int iBytesRead = 4;
                // Make a list of FileInfo objects
                List<FileInfo> lstFiles = new List<FileInfo>();
                // Set the file position pointer to the beginning of the index
                fsCurrentFile.Seek(-iOffset, SeekOrigin.End);
                // Create variables for file properties
                int iFileNameLength = 0;
                // Loop through the index to get each file information
                while (iBytesRead < iOffset)
                {
                    // Increment the bytes by the file's name length (1 byte), the files offset (4 bytes),
                    // and the length of the files data (4 bytes)
                    iBytesRead += 9;
                    FileInfo fiFile = new FileInfo();
                    (fiFile.FileName, iFileNameLength) = GetFileName(fsCurrentFile);
                    // Increment iBytesRead by iFileNameLength
                    iBytesRead += iFileNameLength;
                    // The next four bytes are the files offset and are formatted in little endian
                    fiFile.FileOffest = FourLEBytesToInt(fsCurrentFile);
                    // The next four bytes are the files length and are also formatte in little endian
                    fiFile.FileLength = FourLEBytesToInt(fsCurrentFile);
                    lstFiles.Add(fiFile);
                }

                // All files are stored in list, display info in data grid view
                DisplayInfo(lstFiles);
                // Enable the extract button
                btnExtract.Enabled = true;
                // Close the file
                fsCurrentFile.Close();
            }
        }
        // Function to convert 4 little endian bytes to int
        private int FourLEBytesToInt(FileStream fsFile)
        {
            byte[] byBuffer = new byte[4];
            fsFile.Read(byBuffer, 0, 4);
            int iValue = byBuffer[0];
            int iNextByte = byBuffer[1];
            iValue = iValue | (iNextByte << 8);
            iNextByte = byBuffer[2];
            iValue = iValue | (iNextByte << 16);
            iNextByte = byBuffer[3];
            iValue = iValue | (iNextByte << 24);
            return iValue;
        }
        // Function to get the file name from index
        private (string, int) GetFileName(FileStream fsFile)
        {
            byte[] byLength = new byte[1];
            // Get the length of the file name
            fsFile.Read(byLength, 0, 1);
            // Store the length of the file to make a bet byte array for the name
            int iFileNameLength = byLength[0];
            byte[] byBuffer = new byte[iFileNameLength];
            // Read the bytes into the array
            fsFile.Read(byBuffer, 0, iFileNameLength);
            // Make ASCII encoder
            Encoding encASCII = Encoding.ASCII;
            // Use encoder to get the file name
            string strFileName = encASCII.GetString(byBuffer, 0, iFileNameLength);
            return (strFileName, iFileNameLength);
        }

        // Function to display info in datagrid view
        private void DisplayInfo(List<FileInfo> lstFiles)
        {
            // Loop through the files in the list
            foreach(FileInfo fiFile in lstFiles)
            {
                // Make a new row and add the files information into it
                int iNewRowNum = dgvContent.Rows.Add();
                DataGridViewRow dgvrNewRow = dgvContent.Rows[iNewRowNum];
                dgvrNewRow.Cells["FileName"].Value = fiFile.FileName;
                dgvrNewRow.Cells["FileOffset"].Value = fiFile.FileOffest;
                dgvrNewRow.Cells["FileLength"].Value = fiFile.FileLength;
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            // Pop up the save file dialog so the user can choose the path to save to
            // Get the selected row from the datagrid view
            int iSelectedRow = dgvContent.SelectedCells[1].RowIndex;
            DataGridViewRow dgvrSelectedRow = dgvContent.Rows[iSelectedRow];
            Console.WriteLine(iSelectedRow);
            // Seperate the directory name from the file name
            string strDirName = Path.GetDirectoryName(strCurrentPath);
            // Get the file name from the datagridview
            string strFileName = (string)dgvrSelectedRow.Cells["FileName"].Value;
            // Set the initial path equal to the current path if there a valid one
            sfdSaveFile.InitialDirectory = strDirName;
            sfdSaveFile.FileName = strFileName;
            
            //Display the dialog and only proceed if it returns OK
            if (sfdSaveFile.ShowDialog() == DialogResult.OK)
            {
                // Open the archive file to read the data from
                fsCurrentFile = File.Open(strCurrentPath, FileMode.Open, FileAccess.Read);
                // Open/create the extrcted file
                using (FileStream fsExtractedFile = File.Open(strDirName + "\\" + strFileName, FileMode.Create, FileAccess.Write))
                {
                    int iBytesRead;
                    byte[] byBuffer = new byte[4096];
                    int iFileLength = (int)dgvrSelectedRow.Cells["FileLength"].Value;
                    // Set the file position pointer to the beginning of the file
                    int iFileOffset = (int)dgvrSelectedRow.Cells["FileOffset"].Value;
                    fsCurrentFile.Seek(iFileOffset, SeekOrigin.Begin);
                    if (iFileLength <= 4096)
                    {
                        iBytesRead = fsCurrentFile.Read(byBuffer, 0, iFileLength);
                        fsExtractedFile.Write(byBuffer, 0, iBytesRead);
                    }
                    else
                    {
                        int iTotalBytesRead = 0;
                        while (iTotalBytesRead < iFileLength)
                        {
                            iBytesRead = fsCurrentFile.Read(byBuffer, 0, 4096);
                            if (iFileLength - iTotalBytesRead < 4096)
                            {
                                fsExtractedFile.Write(byBuffer, 0, iFileLength - iTotalBytesRead);
                            }
                            else
                            {
                                iTotalBytesRead += iBytesRead;
                                fsExtractedFile.Write(byBuffer, 0, iBytesRead);
                            }
                            iTotalBytesRead += iBytesRead;
                        }
                    }
                }
                // Close the current file
                fsCurrentFile.Close();
                MessageBox.Show("File has been Extracted.");
            }
        }
    }
}
