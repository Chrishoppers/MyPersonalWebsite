using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    public class PayController : Controller
    {
        private readonly ILogger<PayController> _logger;
        private readonly DataSyncService _dataSync;

        public PayController(ILogger<PayController> logger, DataSyncService dataSync)
        {
            _logger = logger;
            _dataSync = dataSync;
        }

        /// <summary>
        /// 支付页面 - 显示二维码和订单信息
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "无效的支付请求";
                return RedirectToAction("History", "Resource");
            }

            // 查找订单
            ResourceRequest? request = null;
            if (int.TryParse(id, out var requestId))
            {
                request = await _dataSync.GetResourceRequestByIdAsync(requestId);
            }
            else
            {
                request = await _dataSync.GetResourceRequestByOrderIdAsync(id);
            }

            if (request == null)
            {
                TempData["Error"] = "订单不存在";
                return RedirectToAction("History", "Resource");
            }

            if (request.UserId != userId.Value)
            {
                TempData["Error"] = "该订单不属于您";
                return RedirectToAction("History", "Resource");
            }

            if (request.IsPaid)
            {
                TempData["Message"] = "该订单已支付";
                return RedirectToAction("History", "Resource");
            }

            ViewBag.OrderId = request.OrderId;
            ViewBag.Amount = request.Amount;
            ViewBag.Description = request.ResourceName;
            ViewBag.CharacterName = request.CharacterName;
            ViewBag.UserName = request.UserName;
            ViewBag.RequestId = request.Id;
            
            // ⭐ 微信收款二维码（固定图片）
            ViewBag.QRCodeUrl = "/images/payment/wechat_qr.jpg";

            return View();
        }

        /// <summary>
        /// 查询支付状态（用户端 - 只读）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckStatus(string orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { paid = false, message = "请先登录" });
            }

            ResourceRequest? request = null;
            if (int.TryParse(orderId, out var requestId))
            {
                request = await _dataSync.GetResourceRequestByIdAsync(requestId);
            }
            else
            {
                request = await _dataSync.GetResourceRequestByOrderIdAsync(orderId);
            }

            if (request == null)
            {
                return Json(new { paid = false, message = "订单不存在" });
            }

            if (request.UserId != userId.Value)
            {
                return Json(new { paid = false, message = "该订单不属于您" });
            }

            // ⭐ 只返回支付状态，不修改任何数据
            return Json(new { paid = request.IsPaid });
        }

        /// <summary>
        /// 支付成功页面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Success(string? orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction("Index");
            }

            ResourceRequest? request = null;
            if (int.TryParse(orderId, out var requestId))
            {
                request = await _dataSync.GetResourceRequestByIdAsync(requestId);
            }
            else
            {
                request = await _dataSync.GetResourceRequestByOrderIdAsync(orderId);
            }

            ViewBag.OrderId = orderId;
            ViewBag.PayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            if (request != null)
            {
                ViewBag.Amount = request.Amount;
                ViewBag.Description = request.ResourceName;
            }

            return View();
        }

        /// <summary>
        /// 支付失败页面
        /// </summary>
        [HttpGet]
        public IActionResult Failure(string? errorCode)
        {
            ViewBag.ErrorCode = errorCode ?? "未知错误";
            ViewBag.Message = errorCode switch
            {
                "PAY_CANCEL" => "您已取消支付",
                "PAY_TIMEOUT" => "支付超时，请重试",
                _ => "支付遇到问题，请稍后重试"
            };
            return View();
        }
    }
}
