using System;
using System.Collections.Generic;
using System.Text;

namespace IRM.SignalRTZ.DataAccess
{
    public class ReportModel
    {
        // 'IP', 'RAM TOTAL', 'RAM FREE', 'DISK TOTAL', 'DISK FREE', 'CPU USAGE %', 'LAST UPDATE'
        public string Ip { get; set; }

        public int RamTotal { get; set; }

        public int RamFree { get; set; }

        public int DiskTotal { get; set; }

        public int DiskFree { get; set; }

        public int CpuUsage { get; set; }

        public DateTime UpdateDate { get; set; }
    }
}
