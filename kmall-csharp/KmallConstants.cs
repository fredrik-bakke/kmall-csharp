using System;

namespace Freidrich.Kmall
{
    /// <summary>
    /// KMALL related constants.
    /// </summary>
    /// <remarks>
    /// some of the constants may be typed wrong, as they're not typed in Kongsberg's documentation.
    /// </remarks>
    public static class KmallConstants
    {
        /*
          Revision History:

          01  01 SEP 2016  Rev A.
          02  01 MAR 2017  Rev B.
          03  05 JUL 2017  Rev C.
          04  08 DES 2017  Rev D.
          05  25 MAY 2018  Rev E.
          06  16 NOV 2018  Rev F.
          07  01 NOV 2019  Rev G.
         */

        public const string EM_DGM_FORMAT_VERSION = "Rev G 2019-11-01";

        public const Int32 MAX_NUM_BEAMS = 1024;
        public const Int32 MAX_EXTRA_DET = 1024;
        public const Int32 MAX_EXTRA_DET_CLASSES = 11;
        public const Int32 MAX_SIDESCAN_SAMP = 60000;
        public const Int32 MAX_SIDESCAN_EXTRA_SAMP = 15000;
        public const Int32 MAX_NUM_TX_PULSES = 9;
        public const Int32 MAX_ATT_SAMPLES = 148;
        public const Int32 MAX_SVP_POINTS = 2000;
        public const Int32 MAX_SVT_SAMPLES = 1;

        public const Int32 MAX_DGM_SIZE = 64000;

        public const Int32 MAX_NUM_MST_DGMS = 256;
        public const Int32 MAX_NUM_MWC_DGMS = 256;
        public const Int32 MAX_NUM_MRZ_DGMS = 32;
        public const Int32 MAX_NUM_FCF_DGMS = 1;

        public const Int32 MAX_SPO_DATALENGTH = 250;
        public const Int32 MAX_ATT_DATALENGTH = 250;
        public const Int32 MAX_SVT_DATALENGTH = 64;
        public const Int32 MAX_SCL_DATALENGTH = 64;
        public const Int32 MAX_SDE_DATALENGTH = 32;
        public const Int32 MAX_SHI_DATALENGTH = 32;
        public const Int32 MAX_CPO_DATALENGTH = 250;
        public const Int32 MAX_CHE_DATALENGTH = 64;

        public const Int32 MAX_F_FILENAME_LENGTH = 64;
        public const Int32 MAX_F_FILE_SIZE = 63000;

        public const UInt16 UNAVAILABLE_POSFIX = 0xffff;
        public const float UNAVAILABLE_LATITUDE = 200.0f;
        public const float UNAVAILABLE_LONGITUDE = 200.0f;
        public const float UNAVAILABLE_SPEED = -1.0f;
        public const float UNAVAILABLE_COURSE = -4.0f;
        public const float UNAVAILABLE_ELLIPSOIDHEIGHT = -999.0f;

        /*********************************************
                        Datagram names
        *********************************************/

        /* I-datagrams */
        public const string EM_DGM_I_INSTALLATION_PARAM = "#IIP";
        public const string EM_DGM_I_OP_RUNTIME = "#IOP";
        /* S-datagrams */
        public const string EM_DGM_S_POSITION = "#SPO";
        public const string EM_DGM_S_KM_BINARY = "#SKM";
        public const string EM_DGM_S_SOUND_VELOCITY_PROFILE = "#SVP";
        public const string EM_DGM_S_SOUND_VELOCITY_TRANSDUCER = "#SVT";
        public const string EM_DGM_S_CLOCK = "#SCL";
        public const string EM_DGM_S_DEPTH = "#SDE";
        public const string EM_DGM_S_HEIGHT = "#SHI";

        /* M-datagrams */
        public const string EM_DGM_M_RANGE_AND_DEPTH = "#MRZ";
        public const string EM_DGM_M_WATER_COLUMN = "#MWC";

        /* C-datagrams */
        public const string EM_DGM_C_POSITION = "#CPO";
        public const string EM_DGM_C_HEAVE = "#CHE";

        /* F-datagrams */
        public const string EM_DGM_F_CALIBRATION_FILE = "#FCF";



        /******** Datagram versions *********/
        public const byte SPO_VERSION = 0;
        public const byte SKM_VERSION = 1;
        public const byte SVP_VERSION = 1;
        public const byte SVT_VERSION = 0;
        public const byte SCL_VERSION = 0;
        public const byte SDE_VERSION = 0;
        public const byte SHI_VERSION = 0;
        public const byte MRZ_VERSION = 1;
        public const byte MWC_VERSION = 1;
        public const byte CPO_VERSION = 0;
        public const byte CHE_VERSION = 0;
        public const byte FCF_VERSION = 0;
        public const byte IIP_VERSION = 0;
        public const byte IOP_VERSION = 0;
        public const byte BIST_VERSION = 0;


        public const string CRLF = "\r\n";
    }
}

