using IRM.SignalRTZ.Common.Dto;
using IRM.SignalRTZ.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace IRM.SignalRTZ.Client
{
    class Program
    {
        private static HubConnection _HubConnection;
        private static int _delaySeconds = 10;

        static async Task Main(string[] args)
        {
            _HubConnection = new HubConnectionBuilder()
                .WithUrl(GetHubAddress())
                .Build();

            _HubConnection.On<int>("DelayUpdated", seconds => OnDelayUpdated(seconds));

            await _HubConnection.StartAsync();

            await _HubConnection.InvokeAsync("GetDelay");

            var reportThread = new Thread(SendReportLoop);
            reportThread.Start();
            
            Console.ReadLine();
        }

        private static async void SendReportLoop()
        {
            while (true)
            {
                // собираем данные
                // диски
                List<DriveInfo> drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
                var totalDiskSpace = drives.Sum(d => d.TotalSize / 1024 / 1024);
                var freeDiskSpace = drives.Sum(d => d.AvailableFreeSpace / 1024 / 1024);
                var disksInfo = new DisksInfo { TotalMb = (int)totalDiskSpace, FreeMb = (int)freeDiskSpace };
                // ОЗУ
                var ramInfo = new RAMInfo();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    ramInfo = GetUnixMetrics();
                else
                    ramInfo = GetWindowsMetrics();
                // ЦП
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                var value = cpuCounter.NextValue();
                if (Math.Abs(value) <= 0.00)
                    value = cpuCounter.NextValue();
                var cpuInfo = new CPUInfo { UsedPercent = (int)value };

                // подготавливаем данные
                var data = new SystemReportDto { DisksInfo = disksInfo, RAMInfo = ramInfo, CPUInfo = cpuInfo };

                // отправляем данные на сервер
                Console.WriteLine($"{DateTime.Now}: отправка данных на сервер.");
                if (_HubConnection.State != HubConnectionState.Connected)
                {
                    await _HubConnection.StartAsync();
                }
                await _HubConnection.SendAsync("SendSystemReport", data);

                Thread.Sleep(_delaySeconds * 1000);
            }
        }

        private static RAMInfo GetWindowsMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new RAMInfo();
            metrics.TotalMb = (int)Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            metrics.FreeMb = (int)Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);

            return metrics;
        }

        private static RAMInfo GetUnixMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo("free -m");
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new RAMInfo();
            metrics.TotalMb = (int)double.Parse(memory[1]);
            metrics.FreeMb = (int)double.Parse(memory[3]);

            return metrics;
        }

        private static void OnDelayUpdated(int seconds)
        {
            _delaySeconds = seconds;
        }

        private static string GetHubAddress()
        {
            using (StreamReader file = File.OpenText(@"clientConfig.json"))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject cfg = (JObject)JToken.ReadFrom(reader);
                var address = cfg.GetValue("hubAddress");
                return address.ToString();
            }
        }
    }

    class ClientConfig
    {
        public string hubConnection { get; set; }
    }
}
