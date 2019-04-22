using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCS_Utilities
{
    class MCSConstants
    {
        public const int LATEST_MAJOR_VERSION = 2;
        public const int LATEST_MINOR_VERSION = 0;
        public const int MR_MAGIC_NUMBER = 1618775168; //used to uniquely identify .mr files

        //the packed file contains the header, which we'll parse, and the body after, we don't have to read the body, we can fseek the file
        public const string HEAD_STOP = "\u0002M3DResourceBody\0002";
        public const int MAX_HEADER_LENGTH = 1024 * 1024 * 10; //up to X bytes for a header
        public const int MAX_BYTES_PER_CHUNK = 8096;//1024;

        public const int SIZE_OF_FLOAT = sizeof(float);
        public const int SIZE_OF_INT = sizeof(int);

    }
}
