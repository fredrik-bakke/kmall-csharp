using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static Freidrich.Kmall.KmallConstants;
using Freidrich.Utils;


namespace Freidrich.Kmall
{
    /// <summary>
    /// Base class for KMAll Datagrams.
    /// </summary>
    public class EMdgm
    {
        /// <summary>
        /// Position of beginning of datagram on file.
        /// -1 = not set.
        /// </summary>
        public long DatagramPosition { get; internal set; } = -1L;

#pragma warning disable IDE1006 // Naming Styles
        public EMdgmHeader header { get; internal set; }
#pragma warning restore IDE1006 // Naming Styles

        public EMdgm(EMdgmHeader header) => this.header = header;

        public EMdgm(EMdgmHeader header, long fileBytePosition)
        {
            this.header = header;
            DatagramPosition = fileBytePosition;
        }

        public static EMdgm ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            switch (header.DatagramType)
            {
                case EM_DGM_C_HEAVE: return EMdgmCHE.ReadDatagramContents(reader, header);
                case EM_DGM_C_POSITION: return EMdgmCPO.ReadDatagramContents(reader, header);
                case EM_DGM_F_CALIBRATION_FILE: return EMdgmFCF.ReadDatagramContents(reader, header);
                //(#IB) BIST datagrams 
                //case "#IBE": return EMdgmIB.ReadDatagramContents(reader, header);
                //case "#IBR": return EMdgmIB.ReadDatagramContents(reader, header);
                //case "#IBS": return EMdgmIB.ReadDatagramContents(reader, header);

                case EM_DGM_I_INSTALLATION_PARAM: return EMdgmIIP.ReadDatagramContents(reader, header);
                case EM_DGM_I_OP_RUNTIME: return EMdgmIOP.ReadDatagramContents(reader, header);
                case EM_DGM_M_RANGE_AND_DEPTH: return EMdgmMRZ.ReadDatagramContents(reader, header);
                case EM_DGM_M_WATER_COLUMN: return EMdgmMWC.ReadDatagramContents(reader, header);
                case EM_DGM_S_CLOCK: return EMdgmSCL.ReadDatagramContents(reader, header);
                case EM_DGM_S_DEPTH: return EMdgmSDE.ReadDatagramContents(reader, header);
                case EM_DGM_S_HEIGHT: return EMdgmSHI.ReadDatagramContents(reader, header);
                case EM_DGM_S_KM_BINARY: return EMdgmSKM.ReadDatagramContents(reader, header);
                case EM_DGM_S_POSITION: return EMdgmSPO.ReadDatagramContents(reader, header);
                case EM_DGM_S_SOUND_VELOCITY_PROFILE: return EMdgmSVP.ReadDatagramContents(reader, header);
                case EM_DGM_S_SOUND_VELOCITY_TRANSDUCER: return EMdgmSVT.ReadDatagramContents(reader, header);
                default:
                    throw new NotSupportedException($"Datagram type \"{header.DatagramType}\" not supported.");
            }
        }

        public virtual void WriteDatagramContents(BinaryWriter writer) { }
    }

    /// <summary>
    /// General header of a kmall datagram.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmHeader
    {
        /// <summary>
        /// Datagram length in bytes. The length field at the start (4 bytes) and
        /// end of the datagram (4 bytes) are included in the length count.
        /// </summary>
        public UInt32 numBytesDgm;
        /// <summary>
        /// Multibeam datagram type. Always 4 characters long. E.g. "#ABC".
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dgmType;
        /// <summary>
        /// Datagram version.
        /// </summary>
        public byte dgmVersion;
        /// <summary>
        /// System ID. Parameter used for separating datagrams from different echosounders
        /// if more than one system is connected to SIS/K-Controller.
        /// </summary>
        public byte systemID;
        /// <summary>
        /// Echo sounder ID, e.g. 122, 302, 710, 712, 2040, 2045, 850.
        /// </summary>
        public UInt16 echoSounderID;
        /// <summary>
        /// Seconds since Epoch 1970-01-01. Add time_nanosec for improved accuracy.
        /// </summary>
        public UInt32 time_sec;
        /// <summary>
        /// Nano second remainder. To be added to time_sec for improved accuracy.
        /// </summary>
        public UInt32 time_nanosec;

        public string DatagramType => Encoding.UTF8.GetString(dgmType);

        /// <summary>
        /// May suffer precission loss due to truncation to milliseconds.
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(time_sec * 1000L + time_nanosec / 1000000L).UtcDateTime;

        public static EMdgmHeader ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmHeader>();

        public static void WriteTo(BinaryWriter writer, EMdgmHeader header) => writer.WriteStructure<EMdgmHeader>(header);
    }



    #region Sensor Datagrams

    /// <summary>
    /// (S)ensor output datagram - common part for all external sensors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmScommon
    {
        /// <summary>
        /// Size in bytes of current struct. Used for denoting size of rest of
        /// datagram in cases where only one datablock is attached.
        /// </summary>
        public UInt16 numBytesCmnPart;
        /// <summary>
        /// Sensor system number, as indicated when setting up the system in K-Controller installation menu.
        /// E.g. position system 0 refers to system POSI_1 in installation datagram #IIP. Check if this sensor
        /// system is active by using #IIP datagram.
        /// 
        /// #SCL - clock datagram:
        ///     Bit:    Sensor system:
        ///     0       Time synchronization from clock data
        ///     1       Time synchronization from active position data
        ///     2       1 PPS is used
        /// </summary>
        public UInt16 sensorSystem;
        /// <summary>
        /// Sensor status. To indicate if quality of sensor data is valid or invalid. Quality may be invalid even if sensor
        /// is active and the PU receives data. Bit code vary according to type of sensor.
        /// Bits 0 - 7 common to all sensors and #MRZ sensor status:
        ///     Bit:    Sensor data:
        ///     0       0 = Data OK; 1 = Data OK and sensor is chosen as active;
        ///             #SCL only: 1 = Valid data and 1PPS OK
        ///     1       0
        ///     2       0 = Data OK; 1 = Reduced Performance;
        ///             #SCL only: 1 = Reduced Performance, no time synchronization of PU
        ///     3       0
        ///     4       0 = Data OK; 1 = Invalid data
        ///     5       0
        ///     6       0 = Velocity from sensor; 1 = Velocity calculated by PU
        ///     7       0
        ///     
        /// For #SPO (position) and CPO (position compatibility) datagrams, bit 8 - 15:
        ///     Bit:    Sensor data:
        ///     8       0
        ///     9       0 = Time from PU used (system); 1 = Time from datagram used (e.g. from GGA telegram)
        ///     10      0 = No motion correction; 1 = With motion correction
        ///     11      0 = Normal quality check; 1 = Operator quality check; Data always valid.
        ///     12      0
        ///     13      0
        ///     14      0
        ///     15      0
        /// </summary>
        public UInt16 sensorStatus;
        //TODO: this field seems to contain information as well. -Fredrik
        /// <summary>
        /// Byte alignment.
        /// </summary>
        public UInt16 padding;

        public static EMdgmScommon ReadFrom(BinaryReader reader) => reader.ReadStructureAndSeek<EMdgmScommon>(scommon => scommon.numBytesCmnPart);

        public static void WriteTo(BinaryWriter writer, EMdgmScommon scommon) => writer.WriteStructureAndPadOrTruncate<EMdgmScommon>(scommon, scommon.numBytesCmnPart);
    }

    /// <summary>
    /// Seems to be unused. May be for an upcoming revision? -Fredrik
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSdataInfo
    {
        public UInt16 numBytesInfoPart;
        public UInt16 numSamplesArray;
        public UInt16 numBytesPerSample;
        public UInt16 numBytesRawSensorData;

        public static EMdgmSdataInfo ReadFrom(BinaryReader reader) => reader.ReadStructureAndSeek<EMdgmSdataInfo>(info => info.numBytesInfoPart);

        public static void WriteTo(BinaryWriter writer, EMdgmSdataInfo info) => writer.WriteStructureAndPadOrTruncate<EMdgmSdataInfo>(info, info.numBytesInfoPart);
    }


    #region SCL - Sensor CLock datagram
    /// <summary>
    /// #SCL - Sensor CLock datagram.
    /// </summary>
    public class EMdgmSCL : EMdgm
    {
        public EMdgmScommon cmnPart;
        public EMdgmSCLdataFromSensor sensData;

        public EMdgmSCL(EMdgmHeader header, EMdgmScommon commonPart, EMdgmSCLdataFromSensor sensorData)
            : base(header)
        {
            cmnPart = commonPart;
            sensData = sensorData;
        }

        public static new EMdgmSCL ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmScommon commonPart = EMdgmScommon.ReadFrom(reader);
            EMdgmSCLdataFromSensor sensorData = EMdgmSCLdataFromSensor.ReadFrom(reader);

            return new EMdgmSCL(header, commonPart, sensorData);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmScommon.WriteTo(writer, cmnPart);
            EMdgmSCLdataFromSensor.WriteTo(writer, sensData);
        }

    }

    /// <summary>
    /// Part of clock datagram giving offsets and the raw input in text format.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSCLdataFromSensor
    {
        /// <summary>
        /// Offset in seconds from K-Controller operator input.
        /// </summary>
        public float offset_sec;
        /// <summary>
        /// Clock deviation from PU. Difference between timestamp at receive of sensor data and time in the clock source.
        /// Difference smaller than +/- 1 second if 1PPS is active and sync from ZDA.
        /// Unit nanoseconds.
        /// </summary>
        public Int32 clockDeviationPU_nanosec;
        /// <summary>
        /// Position data as received from sensor, i.e. uncorrected for motion etc.
        /// Null-terminated.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SCL_DATALENGTH)]
        public byte[] dataFromSensor;

        /// <summary>
        /// Position data as received from sensor, i.e. uncorrected for motion etc.
        /// NewLine- or null-terminated.
        /// </summary>
        public string DataFromSensor
        {
            get
            {
                string dataString = Encoding.UTF8.GetString(dataFromSensor);
                return dataString.Substring(0, dataString.IndexOf(CRLF));
            }
        }

        public static EMdgmSCLdataFromSensor ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmSCLdataFromSensor>();

        public static void WriteTo(BinaryWriter writer, EMdgmSCLdataFromSensor data) => writer.WriteStructure<EMdgmSCLdataFromSensor>(data);
    }
    #endregion SCL - Sensor CLock datagram



    #region SKM - KM binary sensor data
    /// <summary>
    /// #SKM - data from attitude and attitude velocity sensors.
    /// Datagram may contain several sensor measurements. The number of samples in datagram is listed in InfoPart.numSamplesArray.
    /// Time is given in datagram header, is time of arrival on serial line or on network. Time inside #KMB sample is time from
    /// the sensors data. If input is other than KM binary sensor input format, the data are converted to the KM binary format
    /// by the PU. All parameters are uncorrected. For processing of data, installation offsets, installation angles and attitude
    /// values are needed to correct the data for motion.
    /// </summary>
    public class EMdgmSKM : EMdgm
    {
        public EMdgmSKMinfo infoPart;
        public EMdgmSKMsample[/*MAX_ATT_SAMPLES*/] sample;

        public EMdgmSKM(EMdgmHeader header, EMdgmSKMinfo infoPart, EMdgmSKMsample[] samples)
            : base(header)
        {
            this.infoPart = infoPart;
            sample = samples;
        }

        public static new EMdgmSKM ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmSKMinfo infoPart = EMdgmSKMinfo.ReadFrom(reader);
            EMdgmSKMsample[] samples = new EMdgmSKMsample[infoPart.numSamplesArray];

            for (int sample = 0; sample < infoPart.numSamplesArray; sample++)
                samples[sample] = EMdgmSKMsample.ReadFrom(reader);

            return new EMdgmSKM(header, infoPart, samples);
        }
        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmSKMinfo.WriteTo(writer, infoPart);

            for(int i = 0; i < infoPart.numSamplesArray; i++)
                EMdgmSKMsample.WriteTo(writer, sample[i]);
        }
    }

    /// <summary>
    /// (S)ensor output datagram - info of KMB datagrams.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSKMinfo
    {
        /// <summary>
        /// Size in bytes of current struct. Used for denoting size of rest of datagram
        /// in cases where only one datablock is attached.
        /// </summary>
        public UInt16 numBytesInfoPart;
        /// <summary>
        /// Attitude system number, as numbered in installation parameters.
        /// E.g. system 0 refers to system ATTI_1 is installation datagram #IIP.
        /// </summary>
        public byte sensorSystem;
        /// <summary>
        /// Sensor status. Summarise the status fields of all KM binary samples added in this datagram (status in struct KMBinary).
        /// Only available data from input sensor format is summarised. Available data found in SensorDataContens.
        /// Bits 0 - 7 common to all sensors and #MRZ sensor status:
        ///     Bit:    Sensor status:
        ///     0       0 = Data OK; 1 = Data OK and Sensor is active
        ///     1       0
        ///     2       0 = Data OK; 1 = Data Reduced Performance
        ///     3       0
        ///     4       0 = Data OK; 1 = Invalid Data
        ///     5       0
        ///     6       0 = Velocity from Sensor; 1 = Velocity from PU
        /// </summary>
        public byte sensorStatus;
        /// <summary>
        /// Format of raw data from input sensor, given in numerical code according to table below.
        ///     Code:   Sensor Format:
        ///     1       KM Binary Sensor Format
        ///     2       EM 3000 data
        ///     3       Sagem
        ///     4       Seapath binary 11
        ///     5       Seapath binary 23
        ///     6       Seapath binary 26
        ///     7       POS/MV Group 102/103
        ///     8       Coda Octopus MCOM
        /// </summary>
        public UInt16 sensorInputFormat;
        /// <summary>
        /// Number of KM binary sensor samples added in this datagram.
        /// </summary>
        public UInt16 numSamplesArray;
        /// <summary>
        /// Length in bytes of one whole KM binary sensor sample.
        /// </summary>
        public UInt16 numBytesPerSample;
        /// <summary>
        /// Field to indicate which information is available from the input sensor, at the given sensor format.
        /// 0 = not available; 1 = data is available
        /// The bit pattern is used to determine sensorStatus from status field in #KMB samples. Only data available from
        /// sensor is checked up against invalid/reduced performance in status, and summaries in sensorStatus.
        /// E.g. the binary 23 format does not contain delayed heave. This is indicated by setting bit 6 in
        /// sensorDataContents to 0. In each sample in #KMB output from PU, the status field(struct KMBinary) for
        /// INVALID delayed heave (bit 6) is set to 1. The summaries sensorStatus in struct EMdgmSKMinfo will then
        /// be set to 0 if all available data is ok. Expected data field in sensor input:
        /// 
        ///     Indicates what data is available in the fiven sensor format
        ///     Bit:    Sensor Data:
        ///     0       Horizontal position and velocity
        ///     1       Roll and pitch
        ///     2       Heading
        ///     3       Heave and vertical velocity
        ///     4       Acceleration
        ///     5       Error fields
        ///     6       Delayed heave
        /// </summary>
        public UInt16 sensorDataContents;

        public static EMdgmSKMinfo ReadFrom(BinaryReader reader) => reader.ReadStructureAndSeek<EMdgmSKMinfo>(info => info.numBytesInfoPart);

        public static void WriteTo(BinaryWriter writer, EMdgmSKMinfo info) => writer.WriteStructureAndPadOrTruncate<EMdgmSKMinfo>(info, info.numBytesInfoPart);
    }

    /// <summary>
    /// #SKM - sensor attitude data block. Data given timestamped, not corrected.
    /// See Coordinate Systems for definition of positive angles and axis.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KMbinary
    {
        /// <summary>
        /// KMB
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] dgmType;
        /// <summary>
        /// Datagram length in bytes. The length field at the start (4 bytes)
        /// and end of the datagram (4 bytes) are included in the length count.
        /// </summary>
        public UInt16 numBytesDgm;
        /// <summary>
        /// Datagram Version.
        /// </summary>
        public UInt16 dgmVersion;
        /// <summary>
        /// UTC time from inside KM sensor data. Unit seconds. Epoch 1970-01-01 time.
        /// Nanosec part to be added for more exact time.
        /// </summary>
        public UInt32 time_sec;
        /// <summary>
        /// Nano seconds remainder. Nanosec part to be added to time_sec for more exact time.
        /// If time is unavailable from attitude sensor input, time of reception on serial port is added to this field.
        /// </summary>
        public UInt32 time_nanosec;
        /// <summary>
        /// Bit pattern for indication validity of sensor data, and reduced performance.
        /// The status word consists of 32 single bit flags numbered from 0 to 31, where 0 is the least significant bit.
        /// Bit number 0 - 7 indicate if from a sensor data is invalid: 0 = valid data, 1 = invalid data.
        /// Bit number 16 -> indicate if data from sensor has reduced performance: 0 = valid data, 1 = reduced performance.
        /// 
        ///     Invalid data:                               |   Reduced performance:
        ///     Bit:    Sensor data:                        |   Bit:    Sensor data:
        ///     0       Horizontal position and velocity    |   16      Horizontal position and velocity
        ///     1       Roll and pitch                      |   17      Roll and pitch
        ///     2       Heading                             |   18      Heading
        ///     3       Heave and vertical velocity         |   19      Heave and vertical velocity
        ///     4       Acceleration                        |   20      Acceleration
        ///     5       Error fields                        |   21      Error fields
        ///     6       Delayed heave                       |   22      Delayed heave
        /// </summary>
        public UInt32 status;

        /**************** Position ****************/
        /// <summary>
        /// Position in decimal degrees.
        /// </summary>
        public double latitude_deg;
        /// <summary>
        /// Position in decimal degrees.
        /// </summary>
        public double longitude_deg;

        public float ellipsoidHeight_m;


        /**************** Attitude ****************/
        public float roll_deg;
        public float pitch_deg;
        public float heading_deg;
        public float heave_m;

        /**************** Rates ****************/
        public float rollRate;
        public float pitchRate;
        public float yawRate;

        /**************** Velocities ****************/
        public float velNorth;
        public float velEast;
        public float velDown;

        /******** Errors in data. Sensor data quality, as standard deviations ********/
        public float latitudeError_m;
        public float longitudeError_m;
        public float ellipsoidalHeightError_m;
        public float rollError_deg;
        public float pitchError_deg;
        public float headingError_deg;
        public float heaveError_m;

        /**************** Acceleration ****************/
        public float northAcceleration;
        public float eastAcceleration;
        public float downAcceleration;


        /// <summary>
        /// KMB
        /// </summary>
        public string DatagramType => Encoding.UTF8.GetString(dgmType);

        /// <summary>
        /// Suffers some precission loss due to millisecond truncation.
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(time_sec * 1000L + time_nanosec / 1000000L).UtcDateTime;

        /// <summary>
        /// </summary>
        /// <remarks>
        /// In testing, it appears numBytesDgm = KM_Binary + KM_DelayedHeave.
        /// We will run into errors here if we use this method to skip unknown fields.
        /// </remarks>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static KMbinary ReadFrom(BinaryReader reader)
        {
            KMbinary res = reader.ReadStructure<KMbinary>();

            if (BitConverter.IsLittleEndian)
            {
                res.latitude_deg = BinaryUtils.SwapBitHalves(res.latitude_deg);
                res.longitude_deg = BinaryUtils.SwapBitHalves(res.longitude_deg);
            }

            return res;
        }

        public static void WriteTo(BinaryWriter writer, KMbinary data)
        {
            double tmpLat = data.latitude_deg, tmpLong = data.longitude_deg;
            if (BitConverter.IsLittleEndian)
            {
                data.latitude_deg = BinaryUtils.SwapBitHalves(data.latitude_deg);
                data.longitude_deg = BinaryUtils.SwapBitHalves(data.longitude_deg);
            }

            writer.WriteStructure<KMbinary>(data);

            data.latitude_deg = tmpLat;
            data.longitude_deg = tmpLong;
        }
    }

    /// <summary>
    /// #SKM - delayed heave. Included if available from sensor. 
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct KMdelayedHeave
    {
        public UInt32 time_sec;
        public UInt32 time_nanosec;
        /// <summary>
        /// Delayed heave. Unit meters.
        /// </summary>
        public float delayedHeave_m;

        /// <summary>
        /// Suffers some precission loss due to round-off to nearest millisecond.
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(time_sec * 1000L + time_nanosec / 1000000L).UtcDateTime;

        public static KMdelayedHeave ReadFrom(BinaryReader reader) => reader.ReadStructure<KMdelayedHeave>();

        public static void WriteTo(BinaryWriter writer, KMdelayedHeave heave) => writer.WriteStructure<KMdelayedHeave>(heave);
    }

    /// <summary>
    /// #SKM - all available data. An implementation of the KM Binary sensor input format.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSKMsample
    {
        public KMbinary KMdefault;
        public KMdelayedHeave delayedHeave;

        public EMdgmSKMsample(KMbinary kMDefault, KMdelayedHeave delayedHeave)
        {
            KMdefault = kMDefault;
            this.delayedHeave = delayedHeave;
        }

        /// <summary>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="infoPart">Needed for numSamplesArray.</param>
        /// <returns></returns>
        public static EMdgmSKMsample ReadFrom(BinaryReader reader)
        {
            long startPosition = reader.BaseStream.Position;

            KMbinary km_binary_data = KMbinary.ReadFrom(reader);
            KMdelayedHeave km_heave_data = KMdelayedHeave.ReadFrom(reader);

            long dgmEndPosition = startPosition + km_binary_data.numBytesDgm;
            if (dgmEndPosition != reader.BaseStream.Position)
                reader.BaseStream.Seek(dgmEndPosition, SeekOrigin.Begin);
            

            return new EMdgmSKMsample(km_binary_data, km_heave_data);
        }

        public static void WriteTo(BinaryWriter writer, EMdgmSKMsample sample)
        {
            long startPosition = writer.BaseStream.Position;

            KMbinary.WriteTo(writer, sample.KMdefault);
            KMdelayedHeave.WriteTo(writer, sample.delayedHeave);

            long dgmEndPosition = startPosition + sample.KMdefault.numBytesDgm;
            if (dgmEndPosition != writer.BaseStream.Position)
                writer.BaseStream.Seek(dgmEndPosition, SeekOrigin.Begin);
        }
    }

    #endregion SKM - KM binary sensor data



    #region SPO - Position Data

    /// <summary>
    /// #SPO - position sensor datagram. From data from active sensor will be motion corrected if
    /// indicated by operator. Motion correction is applied to latitude, longitude, speed, course
    /// and ellipsoidal height. If the sensor is inactive, the fields will be marked as unavailable,
    /// defined by parameters UNAVAILABLE_LATITUDE etc.
    /// </summary>
    public class EMdgmSPO : EMdgm
    {
        public EMdgmScommon cmnPart;
        public EMdgmSPOdataBlock sensorData;

        public EMdgmSPO(EMdgmHeader header, EMdgmScommon commonPart, EMdgmSPOdataBlock sensorData)
            : base(header)
        {
            cmnPart = commonPart;
            this.sensorData = sensorData;
        }

        public static new EMdgmSPO ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmScommon commonPart = EMdgmScommon.ReadFrom(reader);
            EMdgmSPOdataBlock sensorData = EMdgmSPOdataBlock.ReadFrom(reader);

            return new EMdgmSPO(header, commonPart, sensorData);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmScommon.WriteTo(writer, cmnPart);
            EMdgmSPOdataBlock.WriteTo(writer, sensorData);
        }
    }

    /// <summary>
    /// #SPO - Sensor position data block. Data from active sensor is corrected data for position system
    /// installation parameters. Data is also corrected for motion (roll and pitch only) if enabled by K-Controller operator.
    /// Data given both decoded and corrected (active sensors), and raw as received from sensor in text string.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSPOdataBlock
    {
        /// <summary>
        /// UTC time from position sensor. Unit seconds.
        /// Epoch 1970-01-01. Nanosec part to be added for more exact time.
        /// </summary>
        public UInt32 timeFromSensor_sec;
        /// <summary>
        /// UTC time from position sensor. Unit nano second remainder.
        /// </summary>
        public UInt32 timeFromSensor_nanosec;
        /// <summary>
        /// Only if available as input from sensor. Calculation according to format.
        /// </summary>
        public float posFixQuality_m;
        /// <summary>
        /// Motion corrected (if enabled in K-Controller) data as used in depth calculations. Referred to vessel
        /// reference point. Unit decimal degrees. Parameter is set to UNAVAILABLE_LATITUDE if sensor inactive.
        /// </summary>
        public double correctedLat_deg;
        /// <summary>
        /// Motion corrected (if enabled in K-Controller) data as used in depth calculations. Referred to vessel
        /// reference point. Unit decimal degrees. Parameter is set to UNAVAILABLE_LONGITUDE if sensor inactive.
        /// </summary>
        public double correctedLong_deg;
        /// <summary>
        /// Speed over ground. Unit m/s. Motion corrected (if enabled in K-Controller) data as used in depth calculations.
        /// If unavailable or from inactive sensor, value set to UNAVAILABLE_SPEED.
        /// </summary>
        public float speedOverGround_mPerSec;
        /// <summary>
        /// Course over ground. Unit degrees. Motion corrected (if enabled in K-Controller) data as used in depth calculations.
        /// If unavailable or from inactive sensor, value set to UNAVAILABLE_COURSE.
        /// </summary>
        public float courseOverGround_deg;
        /// <summary>
        /// Height of vessel reference point above the ellipsoid. Unit meters.
        /// Motion corrected (if enabled in K-Controller) data as used in depth calculations.
        /// If unavailable or from inactive sensor, value set to UNAVAILABLE_ELLIPSOIDHEIGHT.
        /// </summary>
        public float ellipsoidHeightReRefPoint_m;
        /// <summary>
        /// Position data as received from sensor, i.e. uncorrected for motion etc.
        /// NewLine-terminated.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SPO_DATALENGTH)]
        public byte[] posDataFromSensor;

        /// <summary>
        /// Suffers some precission loss due to millisecond truncation.
        /// </summary>
        public DateTime DateTimeFromSensor => DateTimeOffset.FromUnixTimeMilliseconds(timeFromSensor_sec * 1000L + timeFromSensor_nanosec / 1000000L).UtcDateTime;

        /// <summary>
        /// Position data as received from sensor, i.e. uncorrected for motion etc.
        /// </summary>
        public string PosDataFromSensor
        {
            get
            {
                string dataString = Encoding.UTF8.GetString(posDataFromSensor);
                return dataString.Substring(0, dataString.IndexOf(CRLF));
            }
        }

        public static EMdgmSPOdataBlock ReadFrom(BinaryReader reader)
        {
            EMdgmSPOdataBlock res = reader.ReadStructure<EMdgmSPOdataBlock>();

            if (BitConverter.IsLittleEndian)
            {
                res.correctedLat_deg = BinaryUtils.SwapBitHalves(res.correctedLat_deg);
                res.correctedLong_deg = BinaryUtils.SwapBitHalves(res.correctedLong_deg);
            }

            return res;
        }

        public static void WriteTo(BinaryWriter writer, EMdgmSPOdataBlock data)
        {
            double tmpLat = data.correctedLat_deg, tmpLong = data.correctedLong_deg;
            if (BitConverter.IsLittleEndian)
            {
                data.correctedLat_deg = BinaryUtils.SwapBitHalves(data.correctedLat_deg);
                data.correctedLong_deg = BinaryUtils.SwapBitHalves(data.correctedLong_deg);
            }

            writer.WriteStructure<EMdgmSPOdataBlock>(data);

            data.correctedLat_deg = tmpLat;
            data.correctedLong_deg = tmpLong;
        }
    }
    #endregion SPO - Position Data



    #region SVT - Sensor sound Velocity measured at Transducer
    /// <summary>
    /// Sound Velocity at Transducer.
    /// Data for sound velocity and temperature are measured directly on the sound velocity probe.
    /// </summary>
    public class EMdgmSVT : EMdgm
    {
        public EMdgmSVTinfo infoPart;
        public EMdgmSVTsample[/*MAX_SVT_SAMPLES*/] sensorData;

        public EMdgmSVT(EMdgmHeader header, EMdgmSVTinfo info, EMdgmSVTsample[] samples)
            : base(header)
        {
            infoPart = info;
            sensorData = samples;
        }

        public static new EMdgmSVT ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmSVTinfo infoPart = EMdgmSVTinfo.ReadFrom(reader);

            EMdgmSVTsample[] samples = new EMdgmSVTsample[infoPart.numSamplesArray];
            for (int i = 0; i < infoPart.numSamplesArray; i++)
                samples[i] = reader.ReadStructureAndSeek<EMdgmSVTsample>(_ => infoPart.numBytesPerSample);

            return new EMdgmSVT(header, infoPart, samples);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmSVTinfo.WriteTo(writer, infoPart);
            if (!(sensorData is null))
                for(int i = 0; i < infoPart.numSamplesArray; i++)
                    writer.WriteStructureAndPadOrTruncate<EMdgmSVTsample>(sensorData[i], infoPart.numBytesPerSample);
        }
    }

    /// <summary>
    /// Sound Velocity at Transducer. Info part.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSVTinfo
    {
        /// <summary>
        /// Size of current struct in bytes.
        /// Used for denoting size of rest of datagram in cases where only one datablock is attached.
        /// </summary>
        public UInt16 numBytesInfoPart;
        /// <summary>
        /// Sensor status. To indicate quality of sensor data is valid or invalid. Quality may be invalid even if sensor
        /// is active and the PU receives data.
        /// Bit code vary according to type of sensor.
        /// Bits 0-7 common to all sensors and #MRZ sensor status:
        ///     Bit:    Sensor data:
        ///     0       0 Data OK; 1 Data OK and sensor chosen is active
        ///     1       0
        ///     2       0 Data OK; 1 Reduced Performance
        ///     3       0
        ///     4       0 Data OK; 1 Invalid Data
        ///     5       0
        ///     6       0
        /// </summary>
        public UInt16 sensorStatus;
        /// <summary>
        /// Format of raw data from input sensor,
        /// given the numerical code according to table below.
        ///     Code:   Sensor format:
        ///     1       AML NMEA
        ///     2       AML SV
        ///     3       AML SVT
        ///     4       AML SVP
        ///     5       Micro SV
        ///     6       Micro SVT
        ///     7       Micro SVP
        ///     8       Valeport MiniSVS
        ///     9       KSSIS 80
        ///     10      KSSIS 43
        /// </summary>
        public UInt16 sensorInputFormat;
        /// <summary>
        /// Number of sensor samples added in this datagram.
        /// </summary>
        public UInt16 numSamplesArray;
        /// <summary>
        /// Length of one whole SVT sensor sample in bytes.
        /// </summary>
        public UInt16 numBytesPerSample;
        /// <summary>
        /// Field to indicate which information is available from the input sensor, at the given sensor format.
        /// 0 = not available; 1 = data is available
        /// Expected data field in sensor input:
        ///     Bit:    Sensor data:
        ///     0       Sound Velocity
        ///     1       Temperature
        ///     2       Pressure
        ///     3       salinity
        /// </summary>
        public UInt16 sensorDataContents;
        /// <summary>
        /// Time parameter for moving median filter. Unit seconds.
        /// </summary>
        public float filterTime_sec;
        /// <summary>
        /// Offset for measured sound velocity set in K-Controller. Unit m/s.
        /// </summary>
        public float soundVelocity_mPerSec_offset;

        public static EMdgmSVTinfo ReadFrom(BinaryReader reader) => reader.ReadStructureAndSeek<EMdgmSVTinfo>(info => info.numBytesInfoPart);

        public static void WriteTo(BinaryWriter writer, EMdgmSVTinfo info) => writer.WriteStructureAndPadOrTruncate<EMdgmSVTinfo>(info, info.numBytesInfoPart);
    }

    /// <summary>
    /// Sound Velocity at Transducer. Data sample.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// 
    /// Its byte size is defined in <see cref="EMdgmSVTinfo.numBytesPerSample"/>,
    /// and so it only makes sense to read/write a sample in the context of the info.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSVTsample
    {
        /// <summary>
        /// Time in seconds since Epoch 1970-01-01. Add time_nanosec for more exact time.
        /// </summary>
        public UInt32 time_sec;
        /// <summary>
        /// Nano seconds remainder. To be added to time_sec for more exact time.
        /// </summary>
        public UInt32 time_nanosec;
        /// <summary>
        /// Measured sound velocity from sound velocity probe. Unit m/s.
        /// </summary>
        public float soundVelocity_mPerSec;
        /// <summary>
        /// Water temperature from sound velocity probe. Measured in Celsius.
        /// </summary>
        public float temp_C;
        /// <summary>
        /// Pressure. Measured in pascal.
        /// </summary>
        public float pressure_Pa;
        /// <summary>
        /// Salinity of water. Measured in g salt/kg sea water.
        /// </summary>
        public float salinity;

        /// <summary>
        /// Suffers some precission loss due to millisecond truncation.
        /// </summary>
        public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(time_sec * 1000L + time_nanosec / 1000000L).UtcDateTime;
    }
    #endregion SVT - Sensor sound Velocity measured at Transducer



    #region SVP - Sound Velocity Profile
    /// <summary>
    /// #SVP - (S)ound (V)elocity (P)rofile. Data from sound velocity profile or from CTD profile.
    /// Sound velocity is measured directly or estimated, respectively.
    /// </summary>
    public class EMdgmSVP : EMdgm
    {
        public EMdgmSVPinfo cmnPart;
        public EMdgmSVPpoint[/*MAX_SVP_POINTS*/] sensorData;

        public EMdgmSVP(EMdgmHeader header, EMdgmSVPinfo cmnPart, EMdgmSVPpoint[] samples)
            : base(header)
        {
            this.cmnPart = cmnPart;
            this.sensorData = samples;
        }

        public static new EMdgmSVP ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmSVPinfo cmnPart = EMdgmSVPinfo.ReadFrom(reader);

            // There's no field for the number of samples, so this seems safe.
            // Unless we want to do something like
            //int numBytesPerSample = (header.numBytesDgm - Marshal.SizeOf<EMdgmHeader>() - cmnPart.numBytesCmnPart) / cmnPart.numSamples;
            EMdgmSVPpoint[] samples = reader.ReadStructureArray<EMdgmSVPpoint>(cmnPart.numSamples);

            return new EMdgmSVP(header, cmnPart, samples);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmSVPinfo.WriteTo(writer, cmnPart);
            // There's no field for the number of samples, so this seems safe.
            // Unless we want to do something like
            //int numBytesPerSample = (header.numBytesDgm - Marshal.SizeOf<EMdgmHeader>() - cmnPart.numBytesCmnPart) / cmnPart.numSamples;
            writer.WriteStructureArray<EMdgmSVPpoint>(sensorData);
        }
    }

    /// <summary>
    /// #SVP - Sound Velocity Profile, Info part. Data from sound velocity profile or from CTD profile.
    /// Sound velocity is measured directly or estimated, respectively.
    /// 
    /// 
    /// </summary>
    /// <remarks>
    /// This structure is not part of the official specification, but it
    /// fits very naturally and makes the implementation more homogeneous.
    ///     -Fredrik
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSVPinfo
    {
        /// <summary>
        /// Size in bytes of body part struct. Used for denoting size of rest of datagram.
        /// </summary>
        public UInt16 numBytesCmnPart;
        /// <summary>
        /// Number of sound velocity samples.
        /// </summary>
        public UInt16 numSamples;
        /// <summary>
        /// Sound velocity profile format:
        ///     'S00' = Sound velocity profile
        ///     'S01' = CTD profile
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sensorFormat;
        /// <summary>
        /// Time extracted from the Sound Velocity Profile. Parameter is set to zero if not found.
        /// </summary>
        public UInt32 time_sec;
        /// <summary>
        /// Latitude in degrees. Negative if southern hemisphere. Position extracted from the Sound Velocity Profile.
        /// Parameter is set to UNAVAILABLE_LATITUDE if not available.
        /// </summary>
        public double latitude_deg;
        /// <summary>
        /// Longitude in degrees. Negative if western hemisphere. Position extracted from the Sound Velocity Profile.
        /// Parameter is set to UNAVAILABLE_LONGITUDE if not available.
        /// </summary>
        public double longitude_deg;

        public DateTime DateTime => DateTimeOffset.FromUnixTimeSeconds(time_sec).UtcDateTime;

        public string SensorFormat => Encoding.UTF8.GetString(sensorFormat);

        /// <summary>
        /// </summary>
        /// <remarks>
        /// No seeking ahead before reading sample data.
        /// </remarks>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static EMdgmSVPinfo ReadFrom(BinaryReader reader)
        {
            EMdgmSVPinfo res = reader.ReadStructure<EMdgmSVPinfo>();

            if (BitConverter.IsLittleEndian)
            {
                res.latitude_deg = BinaryUtils.SwapBitHalves(res.latitude_deg);
                res.longitude_deg = BinaryUtils.SwapBitHalves(res.longitude_deg);
            }

            return res;
        }

        public static void WriteTo(BinaryWriter writer, EMdgmSVPinfo info)
        {
            double tmpLat = info.latitude_deg, tmpLong = info.longitude_deg;
            if (BitConverter.IsLittleEndian)
            {
                info.latitude_deg = BinaryUtils.SwapBitHalves(info.latitude_deg);
                info.longitude_deg = BinaryUtils.SwapBitHalves(info.longitude_deg);
            }

            writer.WriteStructure<EMdgmSVPinfo>(info);

            info.latitude_deg = tmpLat;
            info.longitude_deg = tmpLong;
        }
    }

    /// <summary>
    /// #SVP - Sound Velocity Profile. Data from one depth point contains information specified in this struct.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSVPpoint
    {
        /// <summary>
        /// Depth at which measurement is taken. Unit meters. Valid range from 0.00 m to 12000 m.
        /// </summary>
        public float depth_m;
        /// <summary>
        /// Measured sound velocity from profile. Unit m/s.
        /// For a CTD profile, this will be the calculated velocity.
        /// </summary>
        public float soundVelocity_mPerSec;
        /// <summary>
        /// Former absorption coefficient. Voided.
        /// </summary>
        public UInt32 padding;
        /// <summary>
        /// Water temperature at given depth. Unit degrees Celsius.
        /// For a Sound velocity profile (S00), this will be set to 0.00.
        /// </summary>
        public float temp_C;
        /// <summary>
        /// salinity of water at given depth. For a Sound velocity profile (S00), this will be set to 0.00.
        /// </summary>
        public float salinity;

        public static EMdgmSVPpoint ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmSVPpoint>();

        public static void WriteTo(BinaryWriter writer, EMdgmSVPpoint point) => writer.WriteStructure<EMdgmSVPpoint>(point);
    }
    #endregion SVP - Sound Velocity Profile



    #region SDE - Sensor DEpth data
    //Untested.
    public class EMdgmSDE : EMdgm
    {
        public EMdgmScommon cmnPart;
        public EMdgmSDEdataFromSensor sensorData;

        public EMdgmSDE(EMdgmHeader header, EMdgmScommon cmnPart, EMdgmSDEdataFromSensor sensorData)
            : base(header)
        {
            this.cmnPart = cmnPart;
            this.sensorData = sensorData;
        }

        public static new EMdgmSDE ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmScommon cmnPart = EMdgmScommon.ReadFrom(reader);
            EMdgmSDEdataFromSensor sensorData = EMdgmSDEdataFromSensor.ReadFrom(reader);

            return new EMdgmSDE(header, cmnPart, sensorData);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmScommon.WriteTo(writer, cmnPart);
            EMdgmSDEdataFromSensor.WriteTo(writer, sensorData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSDEdataFromSensor
    {
        public float depthUsed_m;
        public float offset;
        public float scale;
        public double latitude_deg;
        public double longitude_deg;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SDE_DATALENGTH)]
        public byte[] dataFromSensor;


        public string DataFromSensor
        {
            get
            {
                string dataString = Encoding.UTF8.GetString(dataFromSensor);
                return dataString.Substring(0, dataString.IndexOf(CRLF));
            }
        }

        public static EMdgmSDEdataFromSensor ReadFrom(BinaryReader reader)
        {
            EMdgmSDEdataFromSensor res = reader.ReadStructure<EMdgmSDEdataFromSensor>();

            if (BitConverter.IsLittleEndian)
            {
                res.latitude_deg = BinaryUtils.SwapBitHalves(res.latitude_deg);
                res.longitude_deg = BinaryUtils.SwapBitHalves(res.longitude_deg);
            }

            return res;
        }

        public static void WriteTo(BinaryWriter writer, EMdgmSDEdataFromSensor data)
        {
            double tmpLat = data.latitude_deg, tmpLong = data.longitude_deg;
            if (BitConverter.IsLittleEndian)
            {
                data.latitude_deg = BinaryUtils.SwapBitHalves(data.latitude_deg);
                data.longitude_deg = BinaryUtils.SwapBitHalves(data.longitude_deg);
            }

            writer.WriteStructure<EMdgmSDEdataFromSensor>(data);

            data.latitude_deg = tmpLat;
            data.longitude_deg = tmpLong;
        }
    }
    #endregion SDE - Sensor DEpth data



    #region SHI - Sensor Height data
    //Untested.

    public class EMdgmSHI : EMdgm
    {
        public EMdgmScommon cmnPart;
        public EMdgmSHIdataFromSensor sensData;

        public EMdgmSHI(EMdgmHeader header, EMdgmScommon cmnPart, EMdgmSHIdataFromSensor sensData)
            : base(header)
        {
            this.cmnPart = cmnPart;
            this.sensData = sensData;
        }

        public static new EMdgmSHI ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmScommon cmnPart = EMdgmScommon.ReadFrom(reader);
            EMdgmSHIdataFromSensor sensorData = EMdgmSHIdataFromSensor.ReadFrom(reader);

            return new EMdgmSHI(header, cmnPart, sensorData);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmScommon.WriteTo(writer, cmnPart);
            EMdgmSHIdataFromSensor.WriteTo(writer, sensData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmSHIdataFromSensor
    {
        public UInt16 sensorType;
        public float heightUsed_m;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SHI_DATALENGTH)]
        public byte[] dataFromSensor;

        public string DataFromSensor
        {
            get
            {
                string dataString = Encoding.UTF8.GetString(dataFromSensor);
                return dataString.Substring(0, dataString.IndexOf(CRLF));
            }
        }

        public static EMdgmSHIdataFromSensor ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmSHIdataFromSensor>();

        public static void WriteTo(BinaryWriter writer, EMdgmSHIdataFromSensor data) => writer.WriteStructure<EMdgmSHIdataFromSensor>(data);
    }

    #endregion SHI - Sensor Height data

    #endregion Sensor Datagrams



    #region Multibeam Datagrams


    /// <summary>
    /// (M)ultibeam datagrams - data partition info. General for all M datagrams.
    /// Kongsberg documentation: "If a multibeam depth datagram (or any other large datagram) exceeds the limit of a
    /// UDP package (64 kB), the datagram is split into several datagrams of less than 64 kB before sending from the PU.
    /// The parameters in this struct will give information of the partitioning datagrams. K-Controller/SIS merges
    /// stored in .kmall files will therefore always have numOfDgms = 1 and dgmNum = 1, and may have size greater than 64 kB.
    /// The maximum number of partitions from PU is given by MAX_NUM_MWC_DGMS and MAX_NUM_MRZ_DGMS."
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMpartition
    {
        /// <summary>
        /// Number of datagram parts to re-join to get one Multibeam datagram. E.g. 3.
        /// </summary>
        public UInt16 numOfDgms;
        /// <summary>
        /// Datagram part number. E.g. 2 (of 3), or 1 (of 1).
        /// </summary>
        public UInt16 dgmNum;

        public static EMdgmMpartition ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmMpartition>();

        public static void WriteTo(BinaryWriter writer, EMdgmMpartition mpartition) => writer.WriteStructure<EMdgmMpartition>(mpartition);
    }

    /// <summary>
    /// (M)ultibeam datagram - body part. Start of body of all M datagrams.
    /// Contains information of transmitter and receiver used to find data in datagram.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMbody
    {
        /// <summary>
        /// Size of current struct.
        /// </summary>
        public UInt16 numBytesCmnPart;
        /// <summary>
        /// A ping is made of one or more RX fans and one or more TX pulses transmitted at approximately the same time.
        /// Ping counter is incremented at every set of TX pulses
        /// (one or more pulses transmitted at approximately the same time).
        /// </summary>
        public UInt16 pingCnt;
        /// <summary>
        /// Number of RX fans per ping gives information of how many #MRZ datagrams are generated per ping.
        /// Combined with swathsPerPing, number of datagrams to join for a complete swatch can be found.
        /// </summary>
        public byte rxFansPerPing;
        /// <summary>
        /// Index 0 is the aft swath, port side.
        /// </summary>
        public byte rxFanIndex;
        /// <summary>
        /// Number of swaths per ping. A swath is a complete set of across track data.
        /// A swath may contain several transmit sectors and RX fans.
        /// </summary>
        public byte swathsPerPing;
        /// <summary>
        /// Alongship index for the location of the swath in multi swath mode. Index 0 is the aftmost swath.
        /// </summary>
        public byte swathAlongPosition;
        /// <summary>
        /// Transducer used in this TX fan. Index: 0 = TRAI_TX1; 1 = TRAI_TX2 etc.
        /// </summary>
        public byte txTransducerInd;
        /// <summary>
        /// Transducer used in this RX fan. Index: 0 = TRAI_RX1; 1 = TRAI_RX2 etc.
        /// </summary>
        public byte rxTransducerInd;
        /// <summary>
        /// Total number of receiving units.
        /// </summary>
        public byte numRxTransducers;
        /// <summary>
        /// For future use. 0 - current algorithm, > 0 - future algorithms.
        /// </summary>
        public byte algorithmType;

        public static EMdgmMbody ReadFrom(BinaryReader reader) => reader.ReadStructureAndSeek<EMdgmMbody>(mbody => mbody.numBytesCmnPart);

        public static void WriteTo(BinaryWriter writer, EMdgmMbody mbody) => writer.WriteStructureAndPadOrTruncate<EMdgmMbody>(mbody, mbody.numBytesCmnPart);
    }

    #region MRZ - Multibeam data for raw range, depth, reflectivity, seabed image(SI) etc.
    /// <summary>
    /// A full #MRZ datagram.
    /// Kongsberg documentation: "The datagram also contains seabed image data. Depth points (x,y,z) are calculated in meters,
    /// georeferred to the position of the vessel reference point at the time of the first transmitted pulse of the ping.
    /// The depth point coordinates x and y are in the surface coordinate system (SCS), and are also given as
    /// delta latitude and delta longitude, referred to the origo of the VCS/SCS,
    /// at the time of the midpoint of the first transmitted pulse of the ping (equals time used in the datagram header timestamp).
    /// See 'coordinate systems' for introduction to spatial reference points and coordinate systems.
    /// Reference points are also described in 'reference points and offsets'."
    /// </summary>
    public class EMdgmMRZ : EMdgm
    {
        public EMdgmMpartition partition;
        public EMdgmMbody cmnPart;
        public EMdgmMRZ_pingInfo pingInfo;
        public EMdgmMRZ_txSectorInfo[/*MAX_NUM_TX_PULSES*/] sectorInfo;
        public EMdgmMRZ_rxInfo rxInfo;
        public EMdgmMRZ_extraDetClassInfo[/*MAX_EXTRA_DET_CLASSES*/] extraDetClassInfo;
        public EMdgmMRZ_sounding[/*MAX_NUM_BEAMS + MAX_EXTRA_DET*/] sounding;
        /// <summary>
        /// Seabed image sample amplitude, in 0.1 dB. Actual number of seabed image samples (SIsample_desidB) to be found
        /// by summing parameter SInumSamples in struct EMdgmMRZ_sounding for all beams. Seabed image data are raw
        /// beam sample data taken from the RX beams. The data samples are selected based on the bottom detection ranges.
        /// First sample for each beam is the one with the lowest range. The centre sample from each beam is geo
        /// referenced (x, y, z data from the detections). The backscatter corrections applied at the centre sample are
        /// the same as used for reflectivity2_dB (struct EMdgmMRZ_sounding).
        /// </summary>
        public Int16[/*MAX_SIDESCAN_SAMP*/] SIsample_desidB;

        public EMdgmMRZ(EMdgmHeader header, EMdgmMpartition partition, EMdgmMbody cmnPart, EMdgmMRZ_pingInfo pingInfo, EMdgmMRZ_txSectorInfo[] sectorInfo, EMdgmMRZ_rxInfo rxInfo, EMdgmMRZ_extraDetClassInfo[] extraDetClassInfo, EMdgmMRZ_sounding[] soundings, Int16[] SIsample_desidB)
            : base(header)
        {
            this.partition = partition;
            this.cmnPart = cmnPart;
            this.pingInfo = pingInfo;
            this.sectorInfo = sectorInfo;
            this.rxInfo = rxInfo;
            this.extraDetClassInfo = extraDetClassInfo;
            sounding = soundings;
            this.SIsample_desidB = SIsample_desidB;
        }

        public static new EMdgmMRZ ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmMpartition partition = EMdgmMpartition.ReadFrom(reader);
            EMdgmMbody commonPart = EMdgmMbody.ReadFrom(reader);
            EMdgmMRZ_pingInfo pingInfo = EMdgmMRZ_pingInfo.ReadFrom(reader);

            // Read TX sector info for each sector
            EMdgmMRZ_txSectorInfo[] txSectorInfo = new EMdgmMRZ_txSectorInfo[pingInfo.numTxSectors];
            for (int i = 0; i < pingInfo.numTxSectors; i++)
                txSectorInfo[i] = reader.ReadStructureAndSeek<EMdgmMRZ_txSectorInfo>(_ => pingInfo.numBytesPerTxSector);

            // Read rxInfo
            EMdgmMRZ_rxInfo rxInfo = EMdgmMRZ_rxInfo.ReadFrom(reader);

            // Read extra detection metadata if they exist.
            EMdgmMRZ_extraDetClassInfo[] extraDetClassInfo = new EMdgmMRZ_extraDetClassInfo[rxInfo.numExtraDetectionClasses];
            for (int i = 0; i < rxInfo.numExtraDetectionClasses; i++)
                extraDetClassInfo[i] = reader.ReadStructureAndSeek<EMdgmMRZ_extraDetClassInfo>(_ => rxInfo.numBytesPerClass);

            // Read the sounding data.
            int noOfSoundings = rxInfo.numExtraDetections + rxInfo.numSoundingsMaxMain;

            EMdgmMRZ_sounding[] soundings = new EMdgmMRZ_sounding[noOfSoundings];
            int noOfSeabedImageSamples = 0;
            for (int i = 0; i < noOfSoundings; i++)
            {
                soundings[i] = reader.ReadStructureAndSeek<EMdgmMRZ_sounding>(_ => rxInfo.numBytesPerSounding);
                noOfSeabedImageSamples += soundings[i].SInumSamples;
            }

            // Read the seabed imagery.
            Int16[] SISample_desidB = reader.ReadStructureArray<Int16>(noOfSeabedImageSamples);

            return new EMdgmMRZ(header, partition, commonPart, pingInfo, txSectorInfo, rxInfo, extraDetClassInfo, soundings, SISample_desidB);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmMpartition.WriteTo(writer, partition);
            EMdgmMbody.WriteTo(writer, cmnPart);
            EMdgmMRZ_pingInfo.WriteTo(writer, pingInfo);

            // Write TX sector info for each sector
            if (!(sectorInfo is null))
                for(int i = 0; i < pingInfo.numTxSectors; i++)
                    writer.WriteStructureAndPadOrTruncate<EMdgmMRZ_txSectorInfo>(sectorInfo[i], pingInfo.numBytesPerTxSector);

            // Write RXInfo
            EMdgmMRZ_rxInfo.WriteTo(writer, rxInfo);

            // Write extra detection metadata if they exist.
            if (!(extraDetClassInfo is null))
                for(int i = 0; i < rxInfo.numExtraDetectionClasses; i++)
                    writer.WriteStructureAndPadOrTruncate<EMdgmMRZ_extraDetClassInfo>(extraDetClassInfo[i], rxInfo.numBytesPerClass);

            int noOfSoundings = rxInfo.numExtraDetections + rxInfo.numSoundingsMaxMain;

            // Write the sounding data.
            if (!(sounding is null))
                for(int i = 0; i < noOfSoundings; i++)
                    writer.WriteStructureAndPadOrTruncate<EMdgmMRZ_sounding>(sounding[i], rxInfo.numBytesPerSounding);

            // Write the seabed imagery.
            if (!(SIsample_desidB is null))
                writer.WriteStructureArray<Int16>(SIsample_desidB);
        }
    }

    /// <summary>
    /// #MRZ - ping info. Information on vessel/system level,
    /// i.e. information common to all beams in the current ping.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMRZ_pingInfo
    {
        /// <summary>
        /// Number of bytes in current struct.
        /// </summary>
        public UInt16 numBytesInfoData;
        /// <summary>
        /// Byte alignment.
        /// </summary>
        public UInt16 padding0;

        /**************** Ping Info ****************/
        /// <summary>
        /// Ping rate. Filtered/average.
        /// </summary>
        public float pingRate_Hz;
        /// <summary>
        /// 0 = Equidistance; 1 = Equiangle; 2 = High density
        /// </summary>
        public byte beamSpacing;
        /// <summary>
        /// Depth mode. Describes setting of depth in K-Controller.
        /// Depth mode influences the PUs choice of pulse length and pulse type.
        /// If operator has manually chosen the depth mode to use, this is flagged by adding 100 to the mode index.
        /// 0 = Very Shallow; 1 = Shallow; 2 = Medium; 3 = Deep; 4 = Deeper; 5 = Very Deep; 6 = Extra Deep; 7 = Extreme Deep
        /// </summary>
        public byte depthMode;
        /// <summary>
        /// For advanced use when depth mode is set manually.
        /// 0 = Sub depth mode is not used (when depth mode is auto).
        /// </summary>
        public byte subDepthMode;
        /// <summary>
        /// Achieved distance between swaths, in percent relative to required swath distance.
        /// 0 = function not used; 100 = achieved swath distance equals required swath distance.
        /// </summary>
        public byte distanceBtwSwath;
        /// <summary>
        /// Detection mode. Bottom detection algorithm used.
        /// 0 = Normal; 1 = Waterway; 2 = Tracking; 3 = Minimum depth;
        /// If system running in simulation mode: detection mode + 100 = simulator.
        /// </summary>
        public byte detectionMode;
        /// <summary>
        /// Pulse forms used for current swath. 0 = CW; 1 = Mix; 2 = FM
        /// </summary>
        public byte pulseForm;
        /// <summary>
        /// TODO: Kongsberg documentation lists padding1 as "Ping rate. Filtered/averaged." This appears to be incorrect.
        /// In testing, padding1 prints all zeros. Assuming this is for byte alignment, as with other 'padding' cases.
        /// Byte alignment.
        /// </summary>
        public UInt16 padding1;
        /// <summary>
        /// Ping frequency in hertz. E.G. for EM 2040: 200 000 Hz, 300 000 Hz or 400 000 Hz.
        /// If values is less than 100, it refers to a code defined below:
        ///     Value:  Code:
        ///     -1      Not used
        ///     0       40 - 100 kHz, EM 710, EM 712
        ///     1       50 - 100 kHz, EM 710, EM 712
        ///     2       70 - 100 kHz, EM 710, EM 712
        ///     3       50 kHz, EM 710, EM 712
        ///     4       40 kHz, EM 710, EM 712
        /// 180 000 - 400 000 = 180 - 400 kHz EM 2040C (10kHz steps)
        /// 200 000 = 200 kHz, EM 2040
        /// 300 000 = 300 kHz, EM 2040
        /// 400 000 = 400 kHz, EM 2040
        /// </summary>
        public float frequencyMode_Hz;
        /// <summary>
        /// Lowest centre frequency of all sectors in this swath. Unit hertz. E.g. for EM 2040: 260 000 Hz.
        /// </summary>
        public float frequencyRangeLowLim_Hz;
        /// <summary>
        /// Highest centre frequency of all sectors in this swath. Unit hertz. E.g. for EM 2040: 320 000 Hz.
        /// </summary>
        public float frequencyRangeHighLim_Hz;
        /// <summary>
        /// Total signal length of the sector with longest TX pulse. Unit seconds.
        /// </summary>
        public float maxTotalTxPulseLength_sec;
        /// <summary>
        /// Effective signal length (-3dB envelope) of the sector with longest effective TX pulse. Unit seconds.
        /// </summary>
        public float maxEffTxPulseLength_sec;
        /// <summary>
        /// Effective bandwidth (-3dB envelope) of the sector with highest bandwidth.
        /// </summary>
        public float maxEffTxBandWidth_Hz;
        /// <summary>
        /// Average absorption coefficient, in dB/km, for vertical beam at current depth. Not currently in use.
        /// </summary>
        public float absCoeff_dBPerkm;
        /// <summary>
        /// Port sector edge, used by beamformer. Coverage is refered to Z of SCS. Unit degrees.
        /// </summary>
        public float portSectorEdge_deg;
        /// <summary>
        /// Starboard sector edge, used by beamformer. Coverage is refered to Z of SCS. Unit degrees.
        /// </summary>
        public float starbSectorEdge_deg;
        /// <summary>
        /// Coverage achieved, corrected for raybending. Coverage is refered to Z of SCS. Unit degrees.
        /// </summary>
        public float portMeanCov_deg;
        /// <summary>
        /// Coverage achieved, corrected for raybending. Coverage is refered to Z of SCS. Unit degrees.
        /// </summary>
        public float starbMeanCov_deg;
        /// <summary>
        /// Coverage achieved, corrected for raybending. Coverage is refered to Z of SCS. Unit meters.
        /// </summary>
        public Int16 portMeanCov_m;
        /// <summary>
        /// Coverage achieved, corrected for raybending. Coverage is refered to Z of SCS. Unit meters.
        /// </summary>
        public Int16 starbMeanCov_m;
        /// <summary>
        /// Mode and stabilisation settings as chosen by operator. Each bit refers to one setting in K-Controller.
        /// Unless otherwise stated, default: 0 = off, 1 = on/auto.
        ///     Bit:    Mode:
        ///     1       Pitch stabilisation
        ///     2       Yaw stabilisation
        ///     3       Sonar mode
        ///     4       Angular coverage mode
        ///     5       Sector mode
        ///     6       Swath along position (0 = fixed, 1 = dynamic)
        ///     7-8     Future use
        /// </summary>
        public byte modeAndStabilisation;
        /// <summary>
        /// Filter settings as chosen by operator. Refers to settings in runtime display of K-controller.
        /// Each bit refers to one filter setting. 0 = off, 1 = on/auto.
        ///     Bit:    Filter:
        ///     1       Slope filter
        ///     2       Aeration filter
        ///     3       Sector filter
        ///     4       Interference filter
        ///     5       Amplitude detect
        ///     6-8     Future use
        /// </summary>
        public byte runtimeFilter1;
        /// <summary>
        /// Filter settings as chosen by operator. Refers to settings in runtime display of K-Controller.
        /// 4 bits used per filter.
        ///     Bits:   Filter:
        ///     1-4     Range gate size: 0 = small, 1 = normal, 2 = large
        ///     5-8     Spike filter strength: 0 = off, 1 = weak, 2 = medium, 3 = strong
        ///     9-12    Penetration filter: 0 = off, 1 = weak, 2 = medium, 3 = strong
        ///     13-16   Phase ramp: 0 = short, 1 = normal, 2 = long
        /// </summary>
        public UInt16 runtimeFilter2;
        /// <summary>
        /// Pipe tracking status. Describes how angle and range of top of pipe is determined.
        /// 0 = for future use; 1 = PU uses guidance from SIS.
        /// </summary>
        public UInt32 pipeTrackingStatus;
        /// <summary>
        /// Transmit array size used. Direction along ship. Unit degrees.
        /// </summary>
        public float transmitArraySizeUsed_deg;
        /// <summary>
        /// Receive array size used. Direction across ship. Unit degrees.
        /// </summary>
        public float receiveArraySizeUsed_deg;
        /// <summary>
        /// Operator selected TX power level re maximum. Unit dB. E.g. 0 dB, -10 dB, -20 dB.
        /// </summary>
        public float transmitPower_dB;
        /// <summary>
        /// For marine mammal protection.
        /// The parameters describes time remaining until max source level (SL) is achieved.
        /// Unit %.
        /// </summary>
        public UInt16 SLrampUpTimeRemaining;
        /// <summary>
        /// Padding for byte alignment.
        /// </summary>
        public UInt16 padding2;
        /// <summary>
        /// Yaw correction angle applied. Unit degrees.
        /// </summary>
        public float yawAngle_deg;

        /**************** Info of TX Sector Data Block ****************/
        /// <summary>
        /// Number of transmit sectors. Also called Ntx in documentation.
        /// Denotes how many times the struct EMdgmMRZ_txSectorInfo is repeated in the datagram.
        /// </summary>
        public UInt16 numTxSectors;
        /// <summary>
        /// Number of bytes in the struct EMdgmMRZ_txSectorInfo containing TX sector specific information.
        /// The struct is repeated numTxSectors times.
        /// </summary>
        public UInt16 numBytesPerTxSector;

        /**************** Info at time of midpoint of first TX pulse ****************/
        /// <summary>
        /// Heading of vessel at time of midpoint of first TX pulse. From active heading sensor.
        /// Unit degrees.
        /// </summary>
        public float headingVessel_deg;
        /// <summary>
        /// At time of midpoint of first TX pulse. Value as used in depth calculations.
        /// Source of sound speed defined by user in K-Controller.
        /// Unit meters per second.
        /// </summary>
        public float soundSpeedAtTxDepth_mPerSec;
        /// <summary>
        /// TX transducer depth in meters below waterline, at time of midpoint of first TX pulse.
        /// For the TX array (head) used by this RX fan. Use depth of TX1 to move depth point (XYZ)
        /// from water line to transducer (reference point of old datagram format).
        /// Unit meters.
        /// </summary>
        public float tzTransducerDepth_m;
        /// <summary>
        /// Distance between water line and vessel reference point in meters. At time of midpoint of first TX pulse.
        /// Measured in the surface coordinate system (SCS). See 'Coordinate systems' for definition.
        /// Used this to move depth point (XYZ) from vessel reference point to waterline.
        /// Unit meters.
        /// </summary>
        public float z_waterLevelReRefPoint_m;
        /// <summary>
        /// Distance between *.all reference point and *.kmall reference point (vessel reference point) in meters,
        /// in the surface coordinate system, at time of midpoint of first TX pulse. Used this to move depth point (XYZ)
        /// from vessel reference point to the horizontal location (XY) of the active position sensor's reference point
        /// (old datagram format).
        /// Unit meters.
        /// </summary>
        public float x_kmallToall_m;
        /// <summary>
        /// Distance between *.all reference point and *.kmall reference point (vessel reference point) in meters,
        /// in the surface coordinate system, at time of midpoint of first TX pulse. Used this to move depth point (XYZ)
        /// from vessel reference point to the horizontal location (XY) of the active position sensor's reference point
        /// (old datagram format).
        /// Unit meters.
        /// </summary>
        public float y_kmallToall_m;
        /// <summary>
        /// Method of position determination from position sensor data:
        /// 0 = last position received; 1 = interpolated; 2 = processed.
        /// </summary>
        public byte latLongInfo;
        /// <summary>
        /// Status/quality for data from active position sensor. 0 = valid data, 1 = invalid data, 2 = reduced performance.
        /// </summary>
        public byte posSensorStatus;
        /// <summary>
        /// Status/quality for data from active attitude sensor. 0 = valid data, 1 = invalid data, 2 = reduced performance.
        /// </summary>
        public byte attitudeSensorStatus;
        /// <summary>
        /// Padding for byte alignment.
        /// </summary>
        public byte padding3;

        /// <summary>
        /// Latitude (decimal degrees) of vessel reference point at time of midpoint of first TX pulse.
        /// Negative on southern hemisphere. Parameter is set to UNAVAILABLE_LATITUDE if not available.
        /// </summary>
        public double latitude_deg;
        /// <summary>
        /// Longitude (decimal degrees) of vessel reference point at time of midpoint of first TX pulse.
        /// Negative on western hemisphere. Parameter is set to UNAVAILABLE_LONGITUDE if not available.
        /// </summary>
        public double longitude_deg;
        /// <summary>
        /// Height of vessel reference point above the ellipsoid, derived from active GGA sensor.
        /// ellipsoidHeightReRefPoint_m is GGA height corrected for motion and installation offsets of the position sensor.
        /// </summary>
        public float ellipsoidHeightReRefPoint_m;

        //Not in python implementation:
        public float bsCorrectionOffset_dB;
        public byte lambertsLawApplied;
        public byte iceWindow;
        public UInt16 padding4;

        public static EMdgmMRZ_pingInfo ReadFrom(BinaryReader stream)
        {
            EMdgmMRZ_pingInfo res = stream.ReadStructureAndSeek<EMdgmMRZ_pingInfo>(pingInfo => pingInfo.numBytesInfoData);

            if (BitConverter.IsLittleEndian)
            {
                res.latitude_deg = BinaryUtils.SwapBitHalves(res.latitude_deg);
                res.longitude_deg = BinaryUtils.SwapBitHalves(res.longitude_deg);
            }

            return res;
        }

        public static void WriteTo(BinaryWriter writer, EMdgmMRZ_pingInfo pingInfo)
        {
            double tmpLat = pingInfo.latitude_deg, tmpLong = pingInfo.longitude_deg;
            if (BitConverter.IsLittleEndian)
            {
                pingInfo.latitude_deg = BinaryUtils.SwapBitHalves(pingInfo.latitude_deg);
                pingInfo.longitude_deg = BinaryUtils.SwapBitHalves(pingInfo.longitude_deg);
            }

            writer.WriteStructureAndPadOrTruncate<EMdgmMRZ_pingInfo>(pingInfo, pingInfo.numBytesInfoData);

            pingInfo.latitude_deg = tmpLat;
            pingInfo.longitude_deg = tmpLong;
        }
    }

    /// <summary>
    ///  #MRZ - sector info.
    ///  Information specific to each transmitting sector.
    ///  sector info is repeated numTxSectors(Ntx)-times in datagram.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMRZ_txSectorInfo
    {
        /// <summary>
        /// Sector index number, used in the sounding section. Starts at 0.
        /// </summary>
        public byte txSectorNumb;
        /// <summary>
        /// TX array number. Single TX, txArrayNumber = 0.
        /// </summary>
        public byte txArrNumber;
        /// <summary>
        /// Default = 0. E.g. for EM2040, the transmitted pulse consists of three sectors, each transmitted from separate
        /// txSubArrays. Orientation and numbers are relative to the array coordinate system. Sub array installation offsets
        /// can be found in the installation datagram, #IIP.
        /// 0 = Port subarray; 1 = middle subarray; 2 = starboard subarray
        /// </summary>
        public byte txSubArray;
        /// <summary>
        /// Byte alignment.
        /// </summary>
        public byte padding0;
        /// <summary>
        /// Transmit delay of the current sector/subarray. Delay is the time from the midpoint of the current transmission
        /// to midpoint of the first transmitted pulse of the ping, i.e. relative to the time used in the datagram header.
        /// Unit seconds.
        /// </summary>
        public float sectorTransmitDelay_sec;
        /// <summary>
        /// Along ship steering angle of the TX beam (main lobe of transmitted pulse),
        /// angle reerred to transducer array coordinate system. Unit degrees.
        /// </summary>
        public float tiltAngleReTx_deg;
        /// <summary>
        /// Unit dB re 1 microPascal.
        /// </summary>
        public float txNominalSourceLevel_dB;
        /// <summary>
        /// 0 = no focusing applied.
        /// </summary>
        public float txFocusRange_m;
        /// <summary>
        /// Centre frequency. Unit Hertz.
        /// </summary>
        public float centreFreq_Hz;
        /// <summary>
        /// FM mode: effective bandwidth; CW mode: 1 / (effective TX pulse length)
        /// </summary>
        public float signalBandWidth_Hz;
        /// <summary>
        /// Also called pulse length. Unit seconds.
        /// </summary>
        public float totalSignalLength_sec;
        /// <summary>
        /// Transmit pulse is shaded in time (tapering). Amplitude shading in %.
        /// cos2- function used for shading the TX pulse in time.
        /// </summary>
        public byte pulseShading;
        /// <summary>
        /// Transmit signal wave form. 0 = CW; 1 = FM upsweep; 2 = FM downsweep.
        /// </summary>
        public byte signalWaveForm;
        /// <summary>
        /// Byte alignment.
        /// </summary>
        public UInt16 padding1;

        //Not in python implementation:
        public float highVoltageLevel_dB;
        public float sectorTrackingCorr_dB;
        public float effectiveSignalLength_sec;
    }

    /// <summary>
    /// #MRZ - receiver specific information.
    /// Information specific to the receiver unit used in this swath.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMRZ_rxInfo
    {
        /// <summary>
        /// Bytes in current struct.
        /// </summary>
        public UInt16 numBytesRxInfo;
        /// <summary>
        /// Maximum number of main soundings (bottom soundings) in this datagram,
        /// extra detections (soundings in water column) excluded. Also referred to as Nrx.
        /// Denotes how many bottom points (or loops) given in the struct EMdgmMRZ_sounding.
        /// </summary>
        public UInt16 numSoundingsMaxMain;
        /// <summary>
        /// Number of main soundings of valid quality. Extra detections not included.
        /// </summary>
        public UInt16 numSoundingsValidMain;
        /// <summary>
        /// Bytes per loop of sounding (per depth point), i.e. bytes per loops of the struct EMdgmMRZ_sounding
        /// </summary>
        public UInt16 numBytesPerSounding;
        /// <summary>
        /// Sample frequency divided by water column decimation factor. Unit Hertz.
        /// </summary>
        public float WCSampleRate;
        /// <summary>
        /// Sample frequency divided by seabed image decimation factor. Unit Hertz.
        /// </summary>
        public float seabedImageSampleRate;
        /// <summary>
        /// Backscatter level, normal incidence. Unit dB.
        /// </summary>
        public float BSnormal_dB;
        /// <summary>
        /// Backscatter level - oblique incidence. Unit dB.
        /// </summary>
        public float BSoblique_dB;
        /// <summary>
        /// Sum of alarm flags. Range 0 - 10.
        /// </summary>
        public UInt16 extraDetectionAlarmFlag;
        /// <summary>
        /// Sum of extra detection from all classes. Also refered to as Nd.
        /// </summary>
        public UInt16 numExtraDetections;
        /// <summary>
        /// Range 0 - 10.
        /// </summary>
        public UInt16 numExtraDetectionClasses;
        /// <summary>
        /// Number of bytes in the struct ExtraDetClassInfo
        /// </summary>
        public UInt16 numBytesPerClass;

        public static EMdgmMRZ_rxInfo ReadFrom(BinaryReader reader) => reader.ReadStructureAndSeek<EMdgmMRZ_rxInfo>(rxinfo => rxinfo.numBytesRxInfo);

        public static void WriteTo(BinaryWriter writer, EMdgmMRZ_rxInfo rxinfo) => writer.WriteStructureAndPadOrTruncate<EMdgmMRZ_rxInfo>(rxinfo, rxinfo.numBytesRxInfo);
    }

    /// <summary>
    /// #MRZ - Extra detection class information. To be entered in loop NoOfExtraDetClasses (see EMdgmMRZ_rxInfo) times.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// 
    /// The byte size is declared in <see cref="EMdgmMRZ_rxInfo.numBytesPerClass"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMRZ_extraDetClassInfo
    {
        /// <summary>
        /// Number of extra detection in this class.
        /// </summary>
        public UInt16 numExtraDetInClass;
        /// <summary>
        /// Byte alignment.
        /// </summary>
        public sbyte padding;
        /// <summary>
        /// 0 = no alarm; 1 = alarm
        /// </summary>
        public byte alarmFlag;
    }

    /// <summary>
    /// #MRZ - data for each sounding, e.g. XYZ, reflectivity, two way travel time etc.
    /// Also contains information necessary to read seabed image following this datablock (number of samples in SI etc.).
    /// To be entered in loop (numSoundingsMaxMain + numExtraDetections) times.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// 
    /// The byte size is declared in <see cref="EMdgmMRZ_rxInfo.numBytesPerSounding"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMRZ_sounding
    {
        /// <summary>
        /// Sounding index. Cross reference for seabed image.
        /// Valid range: 0 to (numSoundingsMaxMain + numExtraDetections) - 1, i.e. 0 to (Nrx + Nd) - 1.
        /// </summary>
        public UInt16 SoundingIndex;
        /// <summary>
        /// Transmitting sector number. Valid range: 0 to (Ntx - 1), where Ntx is numTxSectors.
        /// </summary>
        public byte txSectorNumb;

        /**************** Detection Info ****************/
        /// <summary>
        /// Bottom detection type. Normal bottom detection, extra detection or rejected.
        /// 0 = normal detection; 1 = extra detection; 2 = rejected detection
        /// In case 2, the estimated range has been used to fill in amplitude samples in the seabed image datagram.
        /// </summary>
        public byte detectionType;
        /// <summary>
        /// Method for determining bottom detection, e.g. amplitude or phase.
        /// 0 = no valid detection; 1 = amplitude detection; 2 = phase detection; 3-15 for future use.
        /// </summary>
        public byte detectionMethod;
        /// <summary>
        /// For Kongsberg use.
        /// </summary>
        public byte rejectionInfo1;
        /// <summary>
        /// For Kongsberg use.
        /// </summary>
        public byte rejectionInfo2;
        /// <summary>
        /// For Kongsberg use.
        /// </summary>
        public byte postProcessingInfo;
        /// <summary>
        /// Only used by extra detections. Detection class based on detected range.
        /// Detection class 1 to 7 corresponds to value 0 to 6. If the value is between 100 and 106,
        /// the class is disabled by the operator. If the value is 107, the detections are outside the threshold limits.
        /// </summary>
        public byte detectionClass;
        /// <summary>
        /// Detection confidence level.
        /// </summary>
        public byte detectionConfidenceLevel;
        /// <summary>
        /// Byte alignment.
        /// </summary>
        public UInt16 padding;
        /// <summary>
        /// Unit %. rangeFactor = 100 if main detection.
        /// </summary>
        public float rangeFactor;
        /// <summary>
        /// Estimated standard deviation as % of detected depth. Quality Factor (QF) is
        /// calculated from IFREMER Quality Factor (IQF): QF = Est(dz) / z = 100 * 10^(-IQF)
        /// </summary>
        public float qualityFactor;
        /// <summary>
        /// Vertical uncertainty, based on quality factor (QF, QualityFactor).
        /// </summary>
        public float detectionUncertaintyVer_m;
        /// <summary>
        /// Horizontal uncertainty, based on quality factor (QF, QualityFactor).
        /// </summary>
        public float detectionUncertaintyHor_m;
        /// <summary>
        /// Detection window legth. Unit seconds. Sample data range used in final detection.
        /// </summary>
        public float detectionWindowLength_s;
        /// <summary>
        /// Measured echo length. Unit seconds.
        /// </summary>
        public float echoLength_s;

        /**************** Water Column Parameters ****************/
        /// <summary>
        /// Water column beam number. Info for plotting soundings together with water column data.
        /// </summary>
        public UInt16 WCBeamNumb;
        /// <summary>
        /// Water column range. Range of bottom detection, in samples.
        /// </summary>
        public UInt16 WCrange_samples;
        /// <summary>
        /// Water column nominal bean angle across. Re vertical.
        /// </summary>
        public float WCNomBeamAngleAcross_deg;

        /**************** Reflective Data (BackScatter (BS) Data) ****************/
        /// <summary>
        /// Mean absorption coefficient, alpha. Used for TVG calculations. Value as used. Unit dB/km.
        /// </summary>
        public float meanAbsCoeff_dBPerkm;
        /// <summary>
        /// Beam intensity, using the traditional KM special TVG.
        /// </summary>
        public float reflectivity1_dB;
        /// <summary>
        /// Beam intensity (BS). Using TVG = X log R + 2 alpha R. X (operator selected is common to all beams in
        /// datagram. Alpha (variable meanAbsCoeff_dBPerkm) is given for each beam (current struct).
        /// BS = EL - SL - M + TVG + BScorr, where EL = detected Echo Level (not recorded in datagram),
        /// and the rest of the parameters are found below.
        /// </summary>
        public float reflectivity2_dB;
        /// <summary>
        /// Receiver sensitivity (M), in dB, compensated for RX beam pattern
        /// at actual transmit frequency at current vessel attitude.
        /// </summary>
        public float receiverSensitivityApplied_dB;
        /// <summary>
        /// Source level (SL) applied (dB): SL = SLnom + SLcorr, where SLnom = Nominal maximum SL,
        /// recorded per TX sector (variable txNominalSourceLevel_dB in struct MRZ_TXSectorInfo) and
        /// SLcorr = SL correction relative to nominal TX power based on measured high voltage power leven and
        /// any use of digital power control. SL is corrected for TX beampattern along and across at actual transmit
        /// frequency at current vessel attitude.
        /// </summary>
        public float sourceLevelApplied_dB;
        /// <summary>
        /// Backscatter (BScorr) calibration offset applied (default = 0dB).
        /// </summary>
        public float BScalibration_dB;
        /// <summary>
        /// Time Varying Gain (TVG) used when correcting reflectivity.
        /// </summary>
        public float TVG_dB;

        /**************** Range and Angle Data ****************/
        /// <summary>
        /// Angle relative to the RX transducer array, except for ME70,
        /// where the angles are relative to the horizontal plane.
        /// </summary>
        public float beamAngleReRx_deg;
        /// <summary>
        /// Applied beam pointing angle correction.
        /// </summary>
        public float beamAngleCorrection_deg;
        /// <summary>
        /// Two way travel time (also called range). Unit seconds.
        /// </summary>
        public float twoWayTravelTime_sec;
        /// <summary>
        /// Applied two way travel time correction. Unit seconds.
        /// </summary>
        public float twoWayTravelTimeCorrection_sec;

        /**************** Georeferenced Depth Points ****************/
        /// <summary>
        /// Distance from vessel reference point at time of first TX pulse in ping, to depth point.
        /// Measured in the surface coordinate system (SCS), see 'coordinate systems' for definition. Unit decimal degrees.
        /// </summary>
        public float deltaLatitude_deg;
        /// <summary>
        /// Distance from vessel reference point at time of first TX pulse in ping, to depth point.
        /// Measured in the surface coordinate system (SCS), see 'coordinate systems' for definition. Unit decimal degrees.
        /// </summary>
        public float deltaLongitude_deg;
        /// <summary>
        /// Vertical distance z. Distance from vessel reference point at time of first TX pulse in ping, to depth point.
        /// Measured in the surface coordinate system (SCS), see 'coordinate systems' for definition. Unit meters.
        /// </summary>
        public float z_reRefPoint_m;
        /// <summary>
        /// Horizontal distance y. Distance from vessel reference point at time of first TX pulse in ping, to depth point.
        /// Measured in the surface coordinate system (SCS), see 'coordinate systems' for definition. Unit meters.
        /// </summary>
        public float y_reRefPoint_m;
        /// <summary>
        /// Vertical distance x. Distance from vessel reference point at time of first TX pulse in ping, to depth point.
        /// Measured in the surface coordinate system (SCS), see 'coordinate systems' for definition. Unit meters.
        /// </summary>
        public float x_reRefPoint_m;
        /// <summary>
        /// Beam incidence angle adjustment (IBA). Unit degrees.
        /// </summary>
        public float beamIncAngleAdj_deg;
        /// <summary>
        /// For future use.
        /// </summary>
        public UInt16 realTimeCleanInfo;

        /**************** Seabed Image ****************/
        /// <summary>
        /// Seabed image start range, in sample number from transducer. Valid only for the current beam.
        /// </summary>
        public UInt16 SIstartRange_samples;
        /// <summary>
        /// Seabed image. Number of the centre seabed image sample for the current beam.
        /// </summary>
        public UInt16 SIcentreSample;
        /// <summary>
        /// Seabed image. Number of range samples from the current beam, used to form the seabed image.
        /// </summary>
        public UInt16 SInumSamples;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMRZ_extraSI
    {
        public UInt16 portStartRange_samples;
        public UInt16 numPortSamples;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SIDESCAN_EXTRA_SAMP)]
        public Int16[] portSIsample_desidB;
        public UInt16 starbStartRange_samples;
        public UInt16 numStarbSamples;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_SIDESCAN_EXTRA_SAMP)]
        public Int16[] starbSIsample_desidB;

        public static EMdgmMRZ_extraSI ReadFrom(BinaryReader stream) => stream.ReadStructure<EMdgmMRZ_extraSI>();

        public static void WriteTo(BinaryWriter stream, EMdgmMRZ_extraSI data) => stream.WriteStructure<EMdgmMRZ_extraSI>(data);
    }
    #endregion MRZ - Multibeam data for raw range, depth, reflectivity, seabed image(SI) etc.



    #region MWC - Water Column Datagram
    //Untested.
    public class EMdgmMWC : EMdgm
    {
        public EMdgmMpartition partition;
        public EMdgmMbody cmnPart;
        public EMdgmMWCtxInfo txInfo;
        public EMdgmMWCtxSectorData[/*MAX_NUM_TX_PULSES*/] sectorData;
        public EMdgmMWCrxInfo rxInfo;
        public EMdgmMWCrxBeamData[] beamData;

        public EMdgmMWC(EMdgmHeader header, EMdgmMpartition partition, EMdgmMbody cmnPart, EMdgmMWCtxInfo txInfo, EMdgmMWCtxSectorData[] sectorData, EMdgmMWCrxInfo rxInfo, EMdgmMWCrxBeamData[] beamData)
            : base(header)
        {
            this.partition = partition;
            this.cmnPart = cmnPart;
            this.txInfo = txInfo;
            this.sectorData = sectorData;
            this.rxInfo = rxInfo;
            this.beamData = beamData;
        }

        public static new EMdgmMWC ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmMpartition partition = EMdgmMpartition.ReadFrom(reader);
            EMdgmMbody cmnPart = EMdgmMbody.ReadFrom(reader);
            EMdgmMWCtxInfo txInfo = EMdgmMWCtxInfo.ReadFrom(reader);

            EMdgmMWCtxSectorData[] sectorData = new EMdgmMWCtxSectorData[txInfo.numTxSectors];
            for (int i = 0; i < txInfo.numTxSectors; i++)
                sectorData[i] = reader.ReadStructureAndSeek<EMdgmMWCtxSectorData>(_ => txInfo.numBytesPerTxSector);

            EMdgmMWCrxInfo rxInfo = EMdgmMWCrxInfo.ReadFrom(reader);
            EMdgmMWCrxBeamData[] beamData = new EMdgmMWCrxBeamData[rxInfo.numBeams];
            for (int i = 0; i < rxInfo.numBeams; i++)
                beamData[i] = EMdgmMWCrxBeamData.ReadFromAndSeek(reader, rxInfo.numBytesPerBeamEntry);

            return new EMdgmMWC(header, partition, cmnPart, txInfo, sectorData, rxInfo, beamData);
        }

        public override void WriteDatagramContents(BinaryWriter reader)
        {
            EMdgmMpartition.WriteTo(reader, partition);
            EMdgmMbody.WriteTo(reader, cmnPart);
            EMdgmMWCtxInfo.WriteTo(reader, txInfo);

            if (!(sectorData is null))
                for (int i = 0; i < txInfo.numTxSectors; i++)
                    reader.WriteStructureAndPadOrTruncate<EMdgmMWCtxSectorData>(sectorData[i], txInfo.numBytesPerTxSector);

            EMdgmMWCrxInfo.WriteTo(reader, rxInfo);

            if (!(beamData is null))
                for (int i = 0; i < rxInfo.numBeams; i++)
                    EMdgmMWCrxBeamData.WriteToAndSeek(reader, beamData[i], rxInfo.numBytesPerBeamEntry);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMWCtxInfo
    {
        public UInt16 numBytesTxInfo;
        public UInt16 numTxSectors;
        public UInt16 numBytesPerTxSector;
        public Int16 padding;
        public float heave_m;

        public static EMdgmMWCtxInfo ReadFrom(BinaryReader stream) => stream.ReadStructureAndSeek<EMdgmMWCtxInfo>(info => info.numBytesTxInfo);

        public static void WriteTo(BinaryWriter stream, EMdgmMWCtxInfo info) => stream.WriteStructureAndPadOrTruncate<EMdgmMWCtxInfo>(info, info.numBytesTxInfo);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The byte size of this struct is declared in <see cref="EMdgmMWCtxInfo.numBytesPerTxSector"/>.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMWCtxSectorData
    {
        public float tiltAngleReTx_deg;
        public float centreFreq_Hz;
        public float txBeamWidthAlong_deg;
        public UInt16 txSectorNum;
        public Int16 padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMWCrxInfo
    {
        public UInt16 numBytesRxInfo;
        public UInt16 numBeams;
        public byte numBytesPerBeamEntry;
        public byte phaseFlag;
        public byte TVGfunctionApplied;
        public sbyte TVGoffset_dB;
        public float sampleFreq_Hz;
        public float soundVelocity_mPerSec;

        public static EMdgmMWCrxInfo ReadFrom(BinaryReader stream) => stream.ReadStructureAndSeek<EMdgmMWCrxInfo>(info => info.numBytesRxInfo);

        public static void WriteTo(BinaryWriter stream, EMdgmMWCrxInfo info) => stream.WriteStructureAndPadOrTruncate<EMdgmMWCrxInfo>(info, info.numBytesRxInfo);

    }

    /// <summary>
    /// This is not part of the official specification. But this is the easiest way I found of allowing a variable length array.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMWCrxBeamDataInfo
    {
        public float beamPointAngReVertical_deg;
        public UInt16 startRangeSampleNum;
        public UInt16 detectedRangeInSamples;
        public UInt16 beamTxSectorNum;
        public UInt16 numSampleData;
        public float detectedRangeInSamplesHighResolution;


        public static EMdgmMWCrxBeamDataInfo ReadFrom(BinaryReader stream) => stream.ReadStructure<EMdgmMWCrxBeamDataInfo>();
        public static void WriteTo(BinaryWriter stream, EMdgmMWCrxBeamDataInfo info) => stream.WriteStructure<EMdgmMWCrxBeamDataInfo>(info);
    }

    public struct EMdgmMWCrxBeamData
    {
        /// <summary>
        /// This is not part of the official specification. But this is the easiest way I found of allowing a variable length array.
        /// </summary>
        EMdgmMWCrxBeamDataInfo infoPart;
        /// <summary>
        /// Pointer to beam related information. Struct defines information about data for a beam. Beam information
        /// is followed by sample amplitudes in 0.5 dB resolution. Amplitude array is followed by phase information
        /// if phaseFlag > 0. These data defined by struct EMdgmMWCrxBeamPhase1_def (int8_t) or struct
        /// EMdgmMWCrxBeamPhase2 (int16_t) if indicated in the field phaseFlag in struct EMdgmMWCrxInfo.
        /// Lenght of data block for each beam depends on the operators choise of phase information(see table).
        ///     phaseFlag:  Beam block size:
        ///     0	        numBytesPerBeamEntry + numSampleData* size(sampleAmplitude05dB_p)
        ///     1	        numBytesPerBeamEntry + numSampleData* size(sampleAmplitude05dB_p) + numSampleData* size(EMdgmMWCrxBeamPhase1_def)
        ///     2	        numBytesPerBeamEntry + numSampleData* size(sampleAmplitude05dB_p) + numSampleData* size(EMdgmMWCrxBeamPhase2_def)
        /// </summary>
        public sbyte[] sampleAmplitude05dB;

        public static EMdgmMWCrxBeamData ReadFromAndSeek(BinaryReader reader, int dataLength)
        {
            //TODO: Move seek to caller.
            long endPosition = reader.BaseStream.Position + dataLength;

            EMdgmMWCrxBeamDataInfo infoPart = EMdgmMWCrxBeamDataInfo.ReadFrom(reader);
            sbyte[] sampleAmplitude05dB = reader.ReadStructureArray<sbyte>(infoPart.numSampleData);

            if (endPosition != reader.BaseStream.Position)
                reader.BaseStream.Seek(endPosition, SeekOrigin.Begin);

            return new EMdgmMWCrxBeamData
            {
                infoPart = infoPart,
                sampleAmplitude05dB = sampleAmplitude05dB
            };
        }

        public static void WriteToAndSeek(BinaryWriter writer, EMdgmMWCrxBeamData data, int dataLength)
        {
            //TODO: Move seek to caller.
            long endPosition = writer.BaseStream.Position + dataLength;

            EMdgmMWCrxBeamDataInfo.WriteTo(writer, data.infoPart);
            writer.WriteStructureArray<sbyte>(data.sampleAmplitude05dB, 0, data.infoPart.numSampleData);

            if (endPosition != writer.BaseStream.Position)
                writer.BaseStream.Seek(endPosition, SeekOrigin.Begin);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmMWCrxBeamPhase1
    {
        public sbyte rxBeamPhase;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct EMdgmMWCrxBeamPhase2
    {
        public Int16 rxBeamPhase;
    }

    #endregion MWC - Water Column Datagram

    #endregion Multibeam Datagrams



    #region Compatibility Datagrams for .all to .kmall conversion support

    #region CHE

    /// <summary>
    /// #CHE - Struct of compatibility heave sensor datagram.
    /// Used for backward compatibility with .all datagram format.
    /// Sent before #MWC (water column datagram) datagram if compatibility mode is enabled.
    /// The multibeam datagram body is common with the #MWC datagram.
    /// </summary>
    public class EMdgmCHE : EMdgm
    {
        public EMdgmMbody cmnPart;
        public EMdgmCHEdata data;

        public EMdgmCHE(EMdgmHeader header, EMdgmMbody commonPart, EMdgmCHEdata data)
            : base(header)
        {
            cmnPart = commonPart;
            this.data = data;
        }

        public static new EMdgmCHE ReadDatagramContents(BinaryReader stream, EMdgmHeader header)
        {
            EMdgmMbody commonPart = EMdgmMbody.ReadFrom(stream);
            EMdgmCHEdata data = EMdgmCHEdata.ReadFrom(stream);

            return new EMdgmCHE(header, commonPart, data);
        }

        public override void WriteDatagramContents(BinaryWriter stream)
        {
            EMdgmMbody.WriteTo(stream, cmnPart);
            EMdgmCHEdata.WriteTo(stream, data);
        }
    }

    /// <summary>
    /// #CHE - Heave compatibility data part.
    /// Heave reference point is at transducer instead of at vessel reference point.
    /// </summary>
    /// <remarks>There's no field for the number of bytes in this record.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmCHEdata
    {
        public float heave_m;

        public static EMdgmCHEdata ReadFrom(BinaryReader stream) => stream.ReadStructure<EMdgmCHEdata>();
        public static void WriteTo(BinaryWriter stream, EMdgmCHEdata data) => stream.WriteStructure<EMdgmCHEdata>(data);
    }
    #endregion CHE



    #region CPO
    /// <summary>
    /// #CPO - Struct of compatibilty position sensor datagram. Data from active sensor will be motion corrected
    /// if indicated by operator. Motion correction is applied to latitude, longitude, speed, course and ellipsoidal height.
    /// If the sensor is inactive, the fields will be marked as unavailable, defined by the parameters UNAVAILABLE_LATITUDE etc.
    /// </summary>
    public class EMdgmCPO : EMdgm
    {
        public EMdgmScommon cmnPart;
        public EMdgmCPOdataBlock sensorData;

        public EMdgmCPO(EMdgmHeader header, EMdgmScommon commonPart, EMdgmCPOdataBlock sensorData)
            : base(header)
        {
            cmnPart = commonPart;
            this.sensorData = sensorData;
        }

        public static new EMdgmCPO ReadDatagramContents(BinaryReader stream, EMdgmHeader header)
        {
            EMdgmScommon commonPart = EMdgmScommon.ReadFrom(stream);
            EMdgmCPOdataBlock sensorData = EMdgmCPOdataBlock.ReadFrom(stream);

            return new EMdgmCPO(header, commonPart, sensorData);
        }

        public override void WriteDatagramContents(BinaryWriter stream)
        {
            EMdgmScommon.WriteTo(stream, cmnPart);
            EMdgmCPOdataBlock.WriteTo(stream, sensorData);
        }
    }


    /// <summary>
    /// #CPO - Compatibility sensor position compatibility data block. Data from active sensor is referenced to position at antenna footprint at water level.
    /// Data is corrected for motion (roll and pitch only) if enabled by K-Controller operator.
    /// Data given both decoded and corrected (active sensors) and raw as received from sensor in text string.
    /// </summary>
    /// <remarks>
    /// There's no field for the number of bytes in this record.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmCPOdataBlock
    {
        /// <summary>
        /// UTC time from position sensor. Unit seconds.
        /// Epoch 1970-01-01. Nanosec part to be added for more exact time.
        /// </summary>
        public UInt32 timeFromSensor_sec;
        /// <summary>
        /// UTC time from position sensor. Unit nano second remainder.
        /// </summary>
        public UInt32 timeFromSensor_nanosec;
        /// <summary>
        /// Only if available as input from sensor. Calculation according to format.
        /// </summary>
        public float posFixQuality;
        /// <summary>
        /// Motion corrected (if enabled in K-Controller) data as used in depth calculations. Referred to vessel
        /// reference point. Unit decimal degrees. Parameter is set to UNAVAILABLE_LATITUDE if sensor inactive.
        /// </summary>
        public double correctedLat_deg;
        /// <summary>
        /// Motion corrected (if enabled in K-Controller) data as used in depth calculations. Referred to vessel
        /// reference point. Unit decimal degrees. Parameter is set to UNAVAILABLE_LONGITUDE if sensor inactive.
        /// </summary>
        public double correctedLong_deg;
        /// <summary>
        /// Speed over ground. Unit m/s. Motion corrected (if enabled in K-Controller) data as used in depth calculations.
        /// If unavailable or from inactive sensor, value set to UNAVAILABLE_SPEED.
        /// </summary>
        public float speedOverGround_mPerSec;
        /// <summary>
        /// Course over ground. Unit degrees. Motion corrected (if enabled in K-Controller) data as used in depth calculations.
        /// If unavailable or from inactive sensor, value set to UNAVAILABLE_COURSE.
        /// </summary>
        public float courseOverGround_deg;
        /// <summary>
        /// Height of vessel reference point above the ellipsoid. Unit meters.
        /// Motion corrected (if enabled in K-Controller) data as used in depth calculations.
        /// If unavailable or from inactive sensor, value set to UNAVAILABLE_ELLIPSOIDHEIGHT.
        /// </summary>
        public float ellipsoidHeightReRefPoint_m;

        /// <summary>
        /// Position data as received from sensor, i.e. uncorrected for motion etc.
        /// NewLine-terminated.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_CPO_DATALENGTH)]
        public byte[] posDataFromSensor;

        /// <summary>
        /// Suffers some precission loss due to round-off to nearest millisecond.
        /// </summary>
        public DateTime DateTimeFromSensor => DateTimeOffset.FromUnixTimeMilliseconds(timeFromSensor_sec * 1000L + timeFromSensor_nanosec / 1000000L).UtcDateTime;

        /// <summary>
        /// Position data as received from sensor, i.e. uncorrected for motion etc.
        /// </summary>
        public string PosDataFromSensor
        {
            get
            {
                string dataString = Encoding.UTF8.GetString(posDataFromSensor);
                return dataString.Substring(0, dataString.IndexOf(CRLF));
            }
        }

        public static EMdgmCPOdataBlock ReadFrom(BinaryReader stream)
        {
            EMdgmCPOdataBlock res = stream.ReadStructure<EMdgmCPOdataBlock>();

            if (BitConverter.IsLittleEndian)
            {
                res.correctedLat_deg = BinaryUtils.SwapBitHalves(res.correctedLat_deg);
                res.correctedLong_deg = BinaryUtils.SwapBitHalves(res.correctedLong_deg);
            }

            return res;
        }

        public static void WriteTo(BinaryWriter stream, EMdgmCPOdataBlock data)
        {
            double tmpLat = data.correctedLat_deg, tmpLong = data.correctedLong_deg;
            if (BitConverter.IsLittleEndian)
            {
                data.correctedLat_deg = BinaryUtils.SwapBitHalves(data.correctedLat_deg);
                data.correctedLong_deg = BinaryUtils.SwapBitHalves(data.correctedLong_deg);
            }

            stream.WriteStructure<EMdgmCPOdataBlock>(data);

            data.correctedLat_deg = tmpLat;
            data.correctedLong_deg = tmpLong;
        }
    }
    #endregion CPO

    #endregion Compatibility Datagrams for .all to .kmall conversion support



    #region File Datagrams


    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmFcommon
    {
        public UInt16 numBytesCmnPart;
        public sbyte fileStatus;
        public byte padding1;
        public UInt32 numBytesFile;
        /// <summary>
        /// Name of file.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_F_FILENAME_LENGTH)]
        public byte[] fileName;

        public string FileName => Encoding.UTF8.GetString(fileName);

        public static EMdgmFcommon ReadFrom(BinaryReader stream) => stream.ReadStructureAndSeek<EMdgmFcommon>(info => info.numBytesCmnPart);
        public static void WriteTo(BinaryWriter stream, EMdgmFcommon info) => stream.WriteStructureAndPadOrTruncate<EMdgmFcommon>(info, info.numBytesCmnPart);
    }


    #region FCF - Backscatter calibration file datagram
    // Untested.
    public class EMdgmFCF : EMdgm
    {
        public EMdgmMpartition partition;
        public EMdgmFcommon cmnPart;
        public byte[/*MAX_F_FILE_SIZE*/] bsCalibrationFile;

        public EMdgmFCF(EMdgmHeader header, EMdgmMpartition partition, EMdgmFcommon cmnPart, byte[] bsCalibrationFile)
            : base(header)
        {
            this.partition = partition;
            this.cmnPart = cmnPart;
            this.bsCalibrationFile = bsCalibrationFile;
        }

        public static new EMdgmFCF ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmMpartition partition = EMdgmMpartition.ReadFrom(reader);
            EMdgmFcommon cmnPart = EMdgmFcommon.ReadFrom(reader);
            byte[] bsCalibrationFile = reader.ReadBytes((int)cmnPart.numBytesFile);

            return new EMdgmFCF(header, partition, cmnPart, bsCalibrationFile);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmMpartition.WriteTo(writer, partition);
            EMdgmFcommon.WriteTo(writer, cmnPart);

            if(!(bsCalibrationFile is null))
                writer.Write(bsCalibrationFile, 0, (int)cmnPart.numBytesFile);

        }
    }

    #endregion FCF - Backscatter calibration file datagram

    #endregion File Datagrams



    #region Installation and Runtime Datagrams

    #region IIP - Info Installation PU
    /// <summary>
    /// #IIP - installation parameters and sensor format settings.
    /// </summary>
    public class EMdgmIIP : EMdgm
    {
        public EMdgmIIPinfo infoPart;
        /// <summary>
        /// Installation settings as text format. Parameters separated by ';' and lines separated by , delimiter.
        /// </summary>
        public string install_txt;

        public EMdgmIIP(EMdgmHeader header, EMdgmIIPinfo infoPart, string installText)
            : base(header)
        {
            this.infoPart = infoPart;
            install_txt = installText;
        }

        public static new EMdgmIIP ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmIIPinfo infoPart = EMdgmIIPinfo.ReadFrom(reader);

            char[] installChars = reader.ReadChars(infoPart.numBytesCmnPart - Marshal.SizeOf<EMdgmIIPinfo>());
            string installText = new string(installChars);

            return new EMdgmIIP(header, infoPart, installText);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmIIPinfo.WriteTo(writer, infoPart);
            char[] installChars = install_txt.ToCharArray();
            writer.Write(installChars);
        }
    }

    /// <summary>
    /// #IIP - installation parameters and sensor format settings. Info part.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmIIPinfo
    {
        /// <summary>
        /// Size in bytes of body part of struct. Used for denoting size of rest of the datagram.
        /// </summary>
        public UInt16 numBytesCmnPart;
        /// <summary>
        /// Information. For future use.
        /// </summary>
        public UInt16 info;
        /// <summary>
        /// Status. For future use.
        /// </summary>
        public UInt16 status;

        //public byte padding;

        public static EMdgmIIPinfo ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmIIPinfo>();

        public static void WriteTo(BinaryWriter writer, EMdgmIIPinfo info) => writer.WriteStructure<EMdgmIIPinfo>(info);
    }
    #endregion IIP - Info Installation PU



    #region IOP - Runtime Datagram
    /// <summary>
    /// #IOP - runtime parameters, exactly as chosen by operator in K-Controller/SIS menus.
    /// </summary>
    public class EMdgmIOP : EMdgm
    {
        public EMdgmIOPinfo infoPart;
        public string runtime_txt;

        public EMdgmIOP(EMdgmHeader header, EMdgmIOPinfo infoPart, string runtimeText)
            : base(header)
        {
            this.infoPart = infoPart;
            runtime_txt = runtimeText;
        }

        public static new EMdgmIOP ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmIOPinfo infoPart = EMdgmIOPinfo.ReadFrom(reader);

            char[] runtimeChars = reader.ReadChars(infoPart.numBytesCmnPart - Marshal.SizeOf<EMdgmIOPinfo>());
            string runtimeText = new string(runtimeChars);

            return new EMdgmIOP(header, infoPart, runtimeText);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmIOPinfo.WriteTo(writer, infoPart);
            char[] runtimeChars = runtime_txt.ToCharArray();
            writer.Write(runtimeChars);
        }
    }

    /// <summary>
    /// #IOP - runtime parameters, exactly as chosen by operator in K-Controller/SIS menus.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EMdgmIOPinfo
    {
        /// <summary>
        /// Size in bytes of body part of struct. Used for denoting size of rest of the datagram.
        /// </summary>
        public UInt16 numBytesCmnPart;
        /// <summary>
        /// Information. For future use.
        /// </summary>
        public UInt16 info;
        /// <summary>
        /// Status. For future use.
        /// </summary>
        public UInt16 status;

        public static EMdgmIOPinfo ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmIOPinfo>();

        public static void WriteTo(BinaryWriter writer, EMdgmIOPinfo info) => writer.WriteStructure<EMdgmIOPinfo>(info);
    }
    #endregion IOP - Runtime Datagram



    #region IB - BIST Error Datagrams

    public class EMdgmIB : EMdgm
    {
        public EMdgmIBinfo infoPart;
        public string BISTText;

        public EMdgmIB(EMdgmHeader header, EMdgmIBinfo infoPart, string bISTText)
            : base(header)
        {
            this.infoPart = infoPart;
            BISTText = bISTText;
        }

        public static new EMdgmIB ReadDatagramContents(BinaryReader reader, EMdgmHeader header)
        {
            EMdgmIBinfo infoPart = EMdgmIBinfo.ReadFrom(reader);

            char[] BISTChars = reader.ReadChars(infoPart.numBytesCmnPart - Marshal.SizeOf<EMdgmIBinfo>());
            string BISTText = new string(BISTChars);

            return new EMdgmIB(header, infoPart, BISTText);
        }

        public override void WriteDatagramContents(BinaryWriter writer)
        {
            EMdgmIBinfo.WriteTo(writer, infoPart);
            char[] BISTChars = BISTText.ToCharArray();
            writer.Write(BISTChars);
        }
    }

    public struct EMdgmIBinfo
    {
        public UInt16 numBytesCmnPart;
        public byte BISTInfo;
        public byte BISTStyle;
        public byte  BISTNumber;
        public sbyte BISTStatus;

        public static EMdgmIBinfo ReadFrom(BinaryReader reader) => reader.ReadStructure<EMdgmIBinfo>();

        public static void WriteTo(BinaryWriter writer, EMdgmIBinfo info) => writer.WriteStructure<EMdgmIBinfo>(info);
    }

    #endregion IB - BIST Error Datagrams

    #endregion Installation and Runtime Datagrams

}
