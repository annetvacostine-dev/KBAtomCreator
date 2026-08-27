using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBAtomCreator.FileProcess
{
    internal class FileItem
    {
            public string Name { get; set; }
            public string Size { get; set; }
            public string Modified { get; set; }
            public string FullPath { get; set; }
            public FileInfo FileInfo { get; set; }

    }
}
