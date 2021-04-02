using IRM.SignalRTZ.DataAccess.Repositories;
using IRM.SignalRTZ.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace IRM.SignalRTZ.Server.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISystemReportRepository _reportRepository;

        public HomeController(ILogger<HomeController> logger, ISystemReportRepository reportRepository)
        {
            _logger = logger;
            _reportRepository = reportRepository;
        }

        public async Task<IActionResult> Index()
        {
            var delay = await _reportRepository.GetDelay();

            var model = new IndexViewModel
            {
                Delay = delay.HasValue ? delay.Value : 30
            };
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
