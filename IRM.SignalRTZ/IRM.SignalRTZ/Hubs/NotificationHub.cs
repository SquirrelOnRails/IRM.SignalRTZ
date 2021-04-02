using IRM.SignalRTZ.Common.Dto;
using IRM.SignalRTZ.DataAccess.Repositories;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IRM.SignalRTZ.Server.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ISystemReportRepository _reportRepository;

        public NotificationHub(ISystemReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        /*public Task SendMessage(string message) 
        {
            return Clients.Others.SendAsync("Send", message);
        }*/

        public async Task SendSystemReport(SystemReportDto report)
        {
            // клиент прислал данные, сохраняем в БД
            var ctx = Context.GetHttpContext();
            var clientAddress = $"{ctx.Connection.RemoteIpAddress.MapToIPv4()}:{ctx.Connection.RemotePort}";

            await _reportRepository.StoreSystemReport(clientAddress, report.RAMInfo, report.CPUInfo, report.DisksInfo);
        }
        
        public async Task GetCurrentRecords()
        {
            var currentReports = await _reportRepository.GetCurrentReports();
            await Clients.Caller.SendAsync("ReceiveReports", currentReports);
        }

        public async Task UpdateDelay(int seconds)
        {
            await _reportRepository.UpdateDelay(seconds);
            await Clients.All.SendAsync("DelayUpdated", seconds);
        }

        public async Task GetDelay()
        {
            var delay = await _reportRepository.GetDelay();
            await Clients.Caller.SendAsync("DelayUpdated", delay.Value);
        }
    }
}
