using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TagLib;

namespace AsciiConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = "/Volumes/music";
            var logFile = "/Users/yury/Documents/music-update-log.txt";
            var processor = new TagProcessor(folder, logFile);
            processor.Process();
        }
    }
}