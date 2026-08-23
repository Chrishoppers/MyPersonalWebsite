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
        public async Task<IActionResult> Index(string? id)
        {
            try
            {
                // 获取用户信息（如果有）
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    var user = await _dataSync.GetUserByIdAsync(userId.Value);
                    ViewBag.UserName = user?.Username;
                }

                // 获取订单信息（如果有）
                if (!string.IsNullOrEmpty(id))
                {
                    // 这里可以查询真实订单数据
                    ViewBag.OrderId = id;
                    ViewBag.Amount = 99.00m;
                    ViewBag.Description = "示例商品";
                }
                else
                {
                    ViewBag.OrderId = "未指定";
                    ViewBag.Amount = 0;
                    ViewBag.Description = "请选择商品";
                }

                ViewBag.Title = "支付中心";

                // ✅ 关键：使用绝对路径指定视图
                return View("~/Views/Pay/Index.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载支付页面失败");
                ViewBag.Error = "系统繁忙，请稍后重试";
                return View("~/Views/Pay/Index.cshtml");
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

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "请先登录", redirect = "/Auth/Login" });
                }

                // 模拟支付处理
                await Task.Delay(500);

                // 模拟 90% 成功率
                if (new Random().Next(1, 101) <= 90)
                {
                    _logger.LogInformation($"用户 {userId} 支付成功，订单: {request.OrderId}");
                    return Json(new
                    {
                        success = true,
                        message = "支付成功",
                        redirect = "/Pay/Success?orderId=" + request.OrderId
                    });
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
            ViewBag.Amount = 99.00m;
            ViewBag.Description = "示例商品";
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
