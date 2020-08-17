using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Freidrich.Kmall
{
    /// <summary>
    /// A reader class for .kmall files.
    /// 
    /// The EM datagram on *.kmall format specification is available at
    /// <see href="https://www.kongsberg.com/maritime/support/document-and-downloads/software-downloads/"/>
    /// under "KMALL - Datagram description". This implementation is based on revision G (posted 10. January 2020).
    /// </summary>
    public class KmallReader : BinaryReader
    {
        /**************** Constructors ****************/


        /// <summary>
        /// Constructs a <c>KmallReader</c> by specifying an input stream.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        public KmallReader(Stream input)
            : base(input, System.Text.Encoding.UTF8)
        { }

        /// <summary>
        /// Constructs a <c>KmallReader</c> by specifying an input stream.
        /// </summary>
        /// <param name="input">The stream to read from.</param>
        /// <param name="leaveOpen">Pass <c>true</c> to leave the stream open after this object is disposed; otherwise, <c>false</c>.</param>
        public KmallReader(Stream input, bool leaveOpen)
            : base(input, System.Text.Encoding.UTF8, leaveOpen)
        { }

        /// <summary>
        /// Constructs a <c>KmallReader</c> by opening a <c>System.IO.FileStream</c> to a specified file.
        /// </summary>
        /// <param name="filePath">File path to a .kmall file.</param>
        public KmallReader(string filePath)
            : this(new FileStream(filePath, FileMode.Open, FileAccess.Read), false)
        { }

        /**************** Read Methods ****************/

        /// <summary>
        /// Reads the next datagram(<c>EMdgm</c>) from stream.
        /// If <paramref name="datagramTypes"/> is specified, will ignore all datagrams which do not match a type in <paramref name="datagramTypes"/>.
        /// </summary>
        /// <param name="datagramTypes">
        /// Optional collection of datagram types to read.
        /// E.g. <c>new HashSet<string>{"#MRZ", "#SKM"}</c>
        /// </param>
        /// <returns>The next datagram. Unless the end of the underlying stream is reached, then returns <c>null</c>.</returns>
        public EMdgm ReadDatagram(ICollection<string> datagramTypes = null)
        {
            EMdgm datagram = null;
            long startPosition;
            try
            {
                do
                {
                    startPosition = BaseStream.Position;
                    if (startPosition >= BaseStream.Length) return null;

                    EMdgmHeader header = EMdgmHeader.ReadFrom(this);
                    //TODO: this is just a work-around to continue a search for datagrams if we encounter empty ones.
                    //I don't know why empty headers appear, and this is probably not a good work-around.
                    bool isNullHeader = header.DatagramType == "\0\0\0\0";

                    //This way, we allow the caller to specify that they want null-headers.

                    if ((!(datagramTypes is null) && datagramTypes.Contains(header.DatagramType)) || ((datagramTypes is null) && !isNullHeader))
                        datagram = EMdgm.ReadDatagramContents(this, header); // Read entire datagram



                    // Seek to end of datagram.
                    long dgmEndPosition = startPosition + (isNullHeader ? (uint)Marshal.SizeOf<EMdgmHeader>() : header.numBytesDgm);
                    if (dgmEndPosition != BaseStream.Position) BaseStream.Seek(dgmEndPosition, SeekOrigin.Begin);

                } while (datagram is null);
            }
            catch (IndexOutOfRangeException iore) //unexpectedly reached end of file while reading datagram.
            {
                throw new EndOfStreamException("Assuming unexpected end of file.", iore);
            }

            datagram.DatagramPosition = startPosition;
            return datagram;
        }

        /// <summary>
        /// Reads the next datagram(<c>EMdgm</c>) from stream which matches a type in <paramref name="datagramTypes"/>.
        /// </summary>
        /// <param name="datagramTypes"></c>
        /// </param>
        /// <returns>The next datagram. Unless the end of the underlying stream is reached, then returns <c>null</c>.</returns>
        public EMdgm ReadDatagram(params string[] datagramTypes) => ReadDatagram((ICollection<string>)datagramTypes);

        /// <summary>
        /// Reads all remaining datagrams.
        /// Optionally only the datagrams which have one of the specified types.
        /// </summary>
        /// <param name="datagramTypes"></param>
        /// <returns></returns>
        public List<EMdgm> ReadAllDatagrams(ICollection<string> datagramTypes = null)
        {
            List<EMdgm> datagrams = new List<EMdgm>();

            try
            {
                while (BaseStream.Position < BaseStream.Length)
                {
                    EMdgm datagram = ReadDatagram(datagramTypes);

                    if (datagram is null) break;
                    datagrams.Add(datagram);
                }
            }
            catch (IOException) {/* End of Stream reached. */}

            return datagrams;
        }

        /// <summary>
        /// Reads all remaining datagrams which have one of the specified types.
        /// </summary>
        /// <param name="datagramTypes"></param>
        /// <returns></returns>
        public List<EMdgm> ReadAllDatagrams(params string[] datagramTypes) => ReadAllDatagrams((ICollection<string>)datagramTypes);

        /// <summary>
        /// Reads the next header as an empty datagram(<c>EMdgm</c>) with DatagramPosition specified.
        /// </summary>
        /// <returns></returns>
        public EMdgm ReadHeader()
        {

            long startPosition = BaseStream.Position;

            EMdgmHeader header = EMdgmHeader.ReadFrom(this);

            bool isNullHeader = header.DatagramType == "\0\0\0\0";
            long dgmEndPosition = startPosition + (isNullHeader ? (uint)Marshal.SizeOf<EMdgmHeader>() : header.numBytesDgm);
            if (dgmEndPosition != BaseStream.Position) BaseStream.Seek(dgmEndPosition, SeekOrigin.Begin);

            return new EMdgm(header, startPosition);
        }

        /// <summary>
        /// Reads all remaining headers as empty datagrams(<c>EMdgm</c>) with DatagramPosition specified.
        /// </summary>
        /// <returns></returns>
        public List<EMdgm> ReadAllHeaders()
        {
            List<EMdgm> headers = new List<EMdgm>();

            while (BaseStream.Position < BaseStream.Length)
                headers.Add(ReadHeader());

            return headers;
        }
    }
}
