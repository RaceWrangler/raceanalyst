using System;
using System.Collections.Generic;
using System.Text;

namespace raceanalyst.Data.Models
{
    public class LineCrossing
    {
        public int ID { get; set; }
        public string FileName { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Number { get; set; }
        public string ClassName { get; set; }
    }
}
