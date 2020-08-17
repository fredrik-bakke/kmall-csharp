# kmall-csharp
A C# class library for reading and writing Kongsberg .kmall files.


**DISCLAIMER:**
This library is largely untested. It has been used to read and write a handful of .kmall files, but with some errors.
As of now there are a few identified issues. See [known issues](#known-issues).


### Example Usages
The following namespaces are used.
```c#
using System.Collections.Generic;
using System.IO;
using Freidrich.Kmall;
```

#### Reading all datagrams from a file
```c#
string filePath = @"C:\myKmallFiles\myKmallFile.kmall";

List<EMdgm> datagrams;
using (KmallReader r = new KmallReader(filePath))
{
  datagrams = r.ReadAllDatagrams();
}
// All datagrams found in file are now stored in the datagrams-variable.
```

#### Reading datagrams of a specific type in a loop
```c#
string filePath = @"C:\myKmallFiles\myKmallFile.kmall";
string datagramType = KmallConstants.EM_DGM_M_RANGE_AND_DEPTH;

using (FileStream fs = new FileStream(filePath))
using (KmallReader r = new KmallReader(fs))
{
  while (true)
  {
    EMdgmMRZ dgm = r.ReadDatagram(datagramType) as EMdgmMRZ;
    if (dgm is null) break; //End of file reached.
    
    /* do stuff here */
  }
}
```

#### Writing a single datagram to a new file
```c#
EMdgm myDatagram;

/* generate datagram */

string filePath = @"C:\myKmallFiles\myKmallFile.kmall";

using (KmallWriter w = new KmallWriter(filePath, FileMode.Create))
{
  w.WriteDatagram(myDatagram);
}
```

#### Making a copy, modifying datagrams of a specific type
```c#
string readPath = @"C:\myKmallFiles\myKmallFile1.kmall";
string writePath = @"C:\myKmallFiles\myKmallFile2.kmall";
string datagramType = KmallConstants.EM_DGM_S_POSITION;

File.Copy(readPath, writePath);

using (KmallReader r = new KmallReader(readPath))
using (KmallWriter w = new KmallWriter(writePath, FileMode.Open))
{
  while (true)
  {
    EMdgmSPO dgm = r.ReadDatagram(datagramType) as EMdgmSPO;
    if (dgm is null) break; //End of file reached.
    
    /* modify datagram here */
    
    //Writes the datagram at the same position as it was read.
    //This functionality is achieved by storing the datagram's file position in the EMdgm.DatagramPosition field.
    w.WriteDatagramAtDatagramPosition(dgm);
  }
}
```

#### Modifying specific datagrams in place
```c#
string filePath = @"C:\myKmallFiles\myKmallFile.kmall";
string datagramType = KmallConstants.EM_DGM_S_CLOCK;

using (FileStream fs = new FileStream(filePath, FileAccess.ReadWrite))
using (KmallReader r = new KmallReader(fs))
using (KmallWriter w = new KmallWriter(fs))
{
  while (true)
  {
    EMdgmSCL dgm = r.ReadDatagram(datagramType) as EMdgmSCL;
    if (dgm is null) break; //End of file reached.
    
    /* modify datagram here */
    
    //Writes the datagram at the same position as it was read.
    //This functionality is achieved by storing the datagram's file position in the EMdgm.DatagramPosition field.
    w.WriteDatagramAtDatagramPosition(dgm);
  }
}
```

### Known Issues
- Doubles are (sometimes) read wrong. I'm suspecting this has something to do with big- vs. little-endian encodings.
- Doubles are sometimes written wrong as well, even though this shouldn't be a problem with endianness.
- KMbinary datagrams are not read properly, but the implemented field order matches the format specification.
- KMbinary datagrams are not writing properly. Some fields are disappearing.
- The sec and nanosec fields are swapped in KMdelayedHeave, but the implemented field order matches the format specification.
- EMdgmSPOdataBlock is not read correctly, but the implemented field order matches the format specification. E.g. ellipsoidHeightReRefPoint_m seems to be stored in courseOverGround_deg instead, while ellipsoidHeightReRefPoint_m is holding a nonsensical value.
- DateTimes are truncated to milliseconds.
