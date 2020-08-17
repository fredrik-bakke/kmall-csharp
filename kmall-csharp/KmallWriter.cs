using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Freidrich.Kmall
{
    public class KmallWriter : BinaryWriter
    {
        /**************** Constructors ****************/

        /// <summary>
        /// Constructs a <c>KmallWriter</c> by specifying an output stream.
        /// </summary>
        /// <param name="output">Stream to write to.</param>
        public KmallWriter(Stream output)
            : base(output, System.Text.Encoding.UTF8)
        { }

        /// <summary>
        /// Constructs a <c>KmallWriter</c> by specifying an output stream.
        /// </summary>
        /// <param name="output">Stream to write to.</param>
        /// <param name="leaveOpen">Pass <c>true</c> to leave the stream open after this object is disposed; otherwise, <c>false</c>.</param>
        public KmallWriter(Stream output, bool leaveOpen)
            : base(output, System.Text.Encoding.UTF8, leaveOpen)
        { }

        /// <summary>
        /// Construct a KmallWriter by opening a <see cref="System.IO.FileStream"/> to a specified file.
        /// This will open a <c>System.IO.FileStream</c> to the specified file immediately.
        /// </summary>
        /// <param name="filePath">File path to a .kmall file.</param>
        /// <param name="fileMode"></param>
        public KmallWriter(string filePath, FileMode fileMode = FileMode.OpenOrCreate)
            : this(new FileStream(filePath, fileMode, FileAccess.Write), false)
        { }





        /**************** Write Methods ****************/

        /// <summary>
        /// Writes a datagram(<c>EMdgm</c>) to the underlying stream at the current position and seeks to the end of the datagram.
        /// </summary>
        protected void InternalWriteDatagram(EMdgm datagram)
        {
            long startPosition = OutStream.Position;

            bool isNullHeader = datagram.header.DatagramType == "\0\0\0\0";

            EMdgmHeader.WriteTo(this, datagram.header);
            datagram.WriteDatagramContents(this);

            // Seek to end of datagram.
            long dgmEndPosition = startPosition + (isNullHeader ? (uint)Marshal.SizeOf<EMdgmHeader>() : datagram.header.numBytesDgm);
            if (dgmEndPosition != OutStream.Position)
                OutStream.Seek(dgmEndPosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Pads the stream so that its length is at least as big as its current position.
        /// </summary>
        protected void CheckPadStreamToPosition()
        {
            if (OutStream.Length >= OutStream.Position) return;

            OutStream.Seek(OutStream.Position - 1, SeekOrigin.Begin); //OutStream.Seek(-1, SeekOrigin.Current);
            OutStream.Write(new byte[1] { 0 }, 0, 1); // Write a zero-byte.
        }

        /// <summary>
        /// Writes a datagram(<c>EMdgm</c>) to the underlying stream at the current position and seeks to the end of the datagram.
        /// </summary>
        public void WriteDatagram(EMdgm datagram)
        {
            InternalWriteDatagram(datagram);

            CheckPadStreamToPosition();
        }

        /// <summary>
        /// Writes a datagram(<c>EMdgm</c>) to the underlying stream at the position specified by <c>EMdgm.DatagramPosition</c> and seeks to the end of the datagram.
        /// </summary>
        public void WriteDatagramAtDatagramPosition(EMdgm datagram)
        {
            OutStream.Seek(datagram.DatagramPosition, SeekOrigin.Begin);
            WriteDatagram(datagram);
            // Must check for every datagram as we may be seeking back and forth.
            CheckPadStreamToPosition();
        }

        /// <summary>
        /// Writes datagrams to the underlying stream at its current position.
        /// </summary>
        /// <param name="datagrams">Datagrams to write.</param>
        public void WriteDatagrams(IEnumerable<EMdgm> datagrams)
        {
            foreach (EMdgm datagram in datagrams)
                InternalWriteDatagram(datagram);

            CheckPadStreamToPosition();
        }

        /// <summary>
        /// Writes datagrams to the underlying stream at its current position.
        /// </summary>
        /// <param name="datagrams">Datagrams to write.</param>
        public void WriteDatagrams(params EMdgm[] datagrams) => WriteDatagrams((IEnumerable<EMdgm>)datagrams);

        /// <summary>
        /// Writes datagrams to the underlying stream at the positions specified by <c>EMdgm.DatagramPosition</c>.
        /// </summary>
        /// <param name="datagrams">Datagrams to write.</param>
        public void WriteDatagramsAtDatagramPositions(IEnumerable<EMdgm> datagrams)
        {
            foreach (EMdgm datagram in datagrams)
                WriteDatagramAtDatagramPosition(datagram);
        }

        /// <summary>
        /// Writes datagrams to the underlying stream at the positions specified by <c>EMdgm.DatagramPosition</c>.
        /// </summary>
        /// <param name="datagrams">Datagrams to write.</param>
        public void WriteDatagramsAtDatagramPositions(params EMdgm[] datagrams) => WriteDatagramsAtDatagramPositions((IEnumerable<EMdgm>)datagrams);
    }
}
