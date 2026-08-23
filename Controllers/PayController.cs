using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;
using System;
using System.Threading.Tasks;

namespace MyPersonalWebsite.Controllers
{
 [Route("Pay")]
    public class PayController : Controller
    {
        private readonly DataSyncService _dataSync;

        public PayController(DataSyncService dataSync)
        {
            _dataSync = dataSync;
        }

        /// <summary>
        /// 支付页面 - /Pay/Index/{id}
        /// </summary>
       
        [HttpGet]
        public async Task<IActionResult> Index(int? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!id.HasValue)
            {
                TempData["Error"] = "无效的支付请求";
                return RedirectToAction("History", "Resource");
            }

            // 从数据库获取申请信息
            var request = await _dataSync.GetResourceRequestByIdAsync(id.Value);
            if (request == null || request.UserId != userId.Value)
            {
                return NotFound();
            }

            if (request.IsPaid)
            {
                TempData["Message"] = "该订单已支付";
                return RedirectToAction("History", "Resource");
            }

            // 设置二维码图片
            ViewBag.QRCodeUrl = "/images/wechat_pay_qr.png";
            ViewBag.OrderId = request.OrderId;
            ViewBag.Amount = request.Amount;

            // 返回 Views/Pay/Index.cshtml
            return View(request);
        }

        /// <summary>
        /// 查询支付状态 (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckPayment(int requestId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { paid = false });
            }

            var request = await _dataSync.GetResourceRequestByIdAsync(requestId);
            if (request == null || request.UserId != userId.Value)
            {
                return Json(new { paid = false });
            }

            return Json(new { paid = request.IsPaid });
        }
    }
}
