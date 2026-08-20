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
        public async Task<IActionResult> Index(string? id)
        {
            try
            {
                // 如果提供了订单ID，尝试获取订单信息
                if (!string.IsNullOrEmpty(id))
                {
                    var order = await GetOrderByIdAsync(id);
                    if (order != null)
                    {
                        ViewBag.OrderId = id;
                        ViewBag.Amount = order.Amount;
                        ViewBag.Description = order.Description;
                        ViewBag.OrderStatus = order.Status;
                    }
                    else
                    {
                        ViewBag.Error = "未找到该订单，请确认订单号是否正确。";
                        _logger.LogWarning($"订单不存在: {id}");
                    }
                }
                else
                {
                    // 无订单ID时显示默认支付页面
                    ViewBag.Title = "支付中心";
                    ViewBag.Description = "请选择支付方式完成付款";
                }

                // 获取当前登录用户信息（如果有）
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    var user = await _dataSync.GetUserByIdAsync(userId.Value);
                    ViewBag.UserName = user?.Username;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载支付页面失败");
                ViewBag.Error = "系统繁忙，请稍后重试";
                return View("Error");
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

                // 验证支付信息（可对接真实支付网关）
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
        public async Task<IActionResult> Success(string? orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return RedirectToAction("Index");
            }

            ViewBag.OrderId = orderId;
            ViewBag.PayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            // 获取订单信息用于展示
            var order = await GetOrderByIdAsync(orderId);
            if (order != null)
            {
                ViewBag.Amount = order.Amount;
                ViewBag.Description = order.Description;
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
                "PAY_FAIL" => "支付失败，请检查账户余额",
                _ => "支付遇到问题，请稍后重试"
            };
            return View();
        }

        // ============================================================
        // 私有辅助方法（模拟数据，实际应从数据库读取）
        // ============================================================

        private async Task<OrderInfo?> GetOrderByIdAsync(string orderId)
        {
            // 这里应该从数据库查询订单
            // 示例：从 Turso 数据库查询
            try
            {
                var sql = $"SELECT * FROM Orders WHERE OrderId = '{orderId}'";
                var result = await _dataSync.QueryAsync(sql);
                if (!string.IsNullOrEmpty(result) && result != "{}")
                {
                    return new OrderInfo
                    {
                        OrderId = orderId,
                        Amount = 99.00m,
                        Description = "示例商品",
                        Status = "pending"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取订单失败: {orderId}");
            }
            return null;
        }

        private async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            // 这里应该对接真实的支付网关
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

    public class OrderInfo
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, paid, cancelled
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
