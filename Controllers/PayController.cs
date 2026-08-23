using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Controllers
{
    public class PayController : Controller
    {
        private readonly ILogger<PayController> _logger;
        private readonly DataSyncService _dataSync;
        private readonly IConfiguration _configuration;

        public PayController(
            ILogger<PayController> logger,
            DataSyncService dataSync,
            IConfiguration configuration)
        {
            _logger = logger;
            _dataSync = dataSync;
            _configuration = configuration;
        }

        /// <summary>
        /// 支付页面主入口
        /// 支持 /pay 和 /pay/{orderId} 两种访问方式
        /// </summary>
        [HttpGet]
        public IActionResult Index(string? id)
        {
            try
            {
                // 设置页面数据
                ViewBag.OrderId = id ?? "未指定订单";
                ViewBag.Amount = 99.00m;
                ViewBag.Description = "示例商品 - 测试支付";
                ViewBag.Title = "支付中心";

                // 获取当前登录用户信息（如果有）
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    // 异步获取用户信息（如果 DataSyncService 有 GetUserByIdAsync 方法）
                    // 这里为了简单，先不调用异步方法，避免阻塞
                    ViewBag.UserName = $"用户{userId}";
                }

                // ✅ 关键：强制指定视图路径
                // 如果您的文件在 Views/Pay/Index.cshtml，使用下面这行：
                return View("~/Views/Pay/Index.cshtml");
                
                // 如果您的文件在 Views/Resource/Pay.cshtml，使用下面这行（注释掉上面那行）：
                // return View("~/Views/Resource/Pay.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载支付页面失败");
                ViewBag.Error = $"系统错误：{ex.Message}";
                return Content($"❌ 支付页面加载失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 处理支付请求（AJAX）
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "请求参数无效" });
                }

                // 验证用户是否登录
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "请先登录", redirect = "/Auth/Login" });
                }

                // 模拟支付处理（实际应对接真实支付网关）
                var result = await ProcessPaymentAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation($"用户 {userId} 支付成功，订单: {request.OrderId}");
                    return Json(new 
                    { 
                        success = true, 
                        message = "支付成功", 
                        redirect = $"/Pay/Success?orderId={request.OrderId}" 
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理支付请求失败");
                return Json(new { success = false, message = "系统异常，请稍后重试" });
            }
        }

        /// <summary>
        /// 支付成功页面
        /// </summary>
        [HttpGet]
        public IActionResult Success(string? orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction("Index");
            }

            ViewBag.OrderId = orderId;
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
                "PAY_TIMEOUT" => "支付超时，请重试",
                "PAY_FAIL" => "支付失败，请检查账户余额",
                _ => "支付遇到问题，请稍后重试"
            };

            return View("~/Views/Pay/Failure.cshtml");
        }

        // ============================================================
        // 私有辅助方法
        // ============================================================

        private async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            // 这里应该对接真实的支付网关（微信/支付宝/Stripe等）
            // 示例：模拟支付处理
            await Task.Delay(1000); // 模拟网络延迟
            
            // 模拟随机支付结果（90%成功）
            if (new Random().Next(1, 101) <= 90)
            {
                return new PaymentResult { Success = true };
            }
            else
            {
                return new PaymentResult { Success = false, ErrorMessage = "支付网关异常，请稍后重试" };
            }
        }
    }

    // ============================================================
    // 辅助模型类
    // ============================================================

    public class PaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // "wechat", "alipay", "card"
        public decimal Amount { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
