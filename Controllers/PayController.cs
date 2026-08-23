using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
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
        /// 支付页面 - 访问 /Pay 或 /Pay/{id}
        /// </summary>
        [HttpGet]
        public IActionResult Index(string? id)
        {
            try
            {
                // 如果您的视图在 Views/Pay/Index.cshtml，使用：
                return View("~/Views/Pay/Index.cshtml");
                
                // 如果您的视图在 Views/Resource/Pay.cshtml，改用：
                // return View("~/Views/Resource/Pay.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "支付页面加载失败");
                return Content($"❌ 错误：{ex.Message}");
            }
        }

        /// <summary>
        /// 处理支付请求 (AJAX)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.OrderId))
                {
                    return Json(new { success = false, message = "订单信息无效" });
                }

                // 模拟支付处理
                await Task.Delay(500);
                
                // 模拟 90% 成功率
                if (new Random().Next(1, 101) <= 90)
                {
                    return Json(new { success = true, message = "支付成功", redirect = "/Pay/Success?orderId=" + request.OrderId });
                }
                else
                {
                    return Json(new { success = false, message = "支付失败，请重试" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理支付异常");
                return Json(new { success = false, message = "系统异常" });
            }
        }

        /// <summary>
        /// 支付成功页面
        /// </summary>
        [HttpGet]
        public IActionResult Success(string? orderId)
        {
            ViewBag.OrderId = orderId ?? "未知订单";
            ViewBag.PayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return View("~/Views/Pay/Success.cshtml");
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
                "PAY_TIMEOUT" => "支付超时",
                _ => "支付失败，请稍后重试"
            };
            return View("~/Views/Pay/Failure.cshtml");
        }
    }

    // ============================================================
    // 请求模型
    // ============================================================

    public class PaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
