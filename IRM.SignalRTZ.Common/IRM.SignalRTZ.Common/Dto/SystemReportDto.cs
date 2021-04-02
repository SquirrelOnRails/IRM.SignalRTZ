using IRM.SignalRTZ.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRM.SignalRTZ.Common.Dto
{
    public class SystemReportDto
    {
        public DisksInfo DisksInfo { get; set; }

        public RAMInfo RAMInfo { get; set; }

        public CPUInfo CPUInfo { get; set; }
    }
}
