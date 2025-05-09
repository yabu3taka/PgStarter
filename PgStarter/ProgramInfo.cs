using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PgStarter
{
    class ProgramInfo
    {
        public ProgramInfo()
        {
        }

        public ProgramInfo(int p)
        {
            ProgramName = "Program " + p;
        }

        public string ProgramName { get; set; }
        public string ProgramPath { get; set; }
        public string MyDocPath { get; set; }
    }

    class ProgramInfoLoader
    {
        private const string FILENAME = "setting.xml";
        private readonly string _file;

        public ProgramInfoLoader(string dir)
        {
            _file = Path.Combine(dir, FILENAME);
        }

        public List<ProgramInfo> Load()
        {
            var ret = new List<ProgramInfo>();
            if (!File.Exists(_file))
            {
                return ret;
            }

            var xml = XElement.Load(_file);
            var tags = from item in xml.Elements("program") select item;
            foreach (XElement tag in tags)
            {
                ProgramInfo info = new ProgramInfo()
                {
                    ProgramName = tag.Element(nameof(ProgramInfo.ProgramName))?.Value ?? "",
                    ProgramPath = tag.Element(nameof(ProgramInfo.ProgramPath))?.Value ?? "",
                    MyDocPath = tag.Element(nameof(ProgramInfo.MyDocPath))?.Value ?? "",
                };
                ret.Add(info);
            }
            return ret;
        }

        public void Save(IEnumerable<ProgramInfo> infos)
        {
            var xml = new XElement("programs");
            foreach (ProgramInfo info in infos)
            {
                XElement tag = new XElement("program",
                    new XElement(nameof(info.ProgramName), info.ProgramName),
                    new XElement(nameof(info.ProgramPath), info.ProgramPath),
                    new XElement(nameof(info.MyDocPath), info.MyDocPath)
                    );
                xml.Add(tag);
            }
            xml.Save(_file);
        }
    }
}
