using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archive_Extractor
{
    public class FileInfo
    {
        private string strFileName;
        private int iFileOffset;
        private int iFileLength;
        public string FileName
        {
            get
            {
                return strFileName;
            }
            set
            {
                strFileName = value;
            }
        }
        public int FileOffest
        {
            get
            {
                return iFileOffset;
            }
            set
            {
                iFileOffset = value;
            }
        }
        public int FileLength
        {
            get
            {
                return iFileLength;
            }
            set
            {
                iFileLength = value;
            }
        }

    }
}
