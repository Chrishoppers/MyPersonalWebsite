using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        /// URL: /Pay/Index/{id}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? id)
        {
            try
            {
                // 1. 检查用户是否登录
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login", "Auth");
                }

                // 2. 验证订单ID
                if (string.IsNullOrEmpty(id))
                {
                    TempData["Error"] = "无效的支付请求";
                    return RedirectToAction("History", "Resource");
                }

                // 3. 查找订单（支持数字ID或订单号）
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

                // 4. 验证订单归属
                if (request.UserId != userId.Value)
                {
                    TempData["Error"] = "该订单不属于您";
                    return RedirectToAction("History", "Resource");
                }

                // 5. 检查是否已支付
                if (request.IsPaid)
                {
                    TempData["Message"] = "该订单已支付";
                    return RedirectToAction("History", "Resource");
                }

                // 6. 传递给视图
                ViewBag.OrderId = request.OrderId;
                ViewBag.Amount = request.Amount;
                ViewBag.Description = request.ResourceName ?? "资源申请";
                ViewBag.CharacterName = request.CharacterName;
                ViewBag.UserName = request.UserName;
                ViewBag.RequestId = request.Id;
                ViewBag.QRCodeUrl = "/images/payment/wechat_qr.jpg";

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载支付页面失败");
                TempData["Error"] = "系统繁忙，请稍后重试";
                return RedirectToAction("History", "Resource");
            }
        }

        /// <summary>
        /// 查询支付状态（用户端 - 只读）
        /// URL: /Pay/CheckStatus?orderId=xxx
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckStatus(string orderId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { paid = false, message = "请先登录" });
                }

                if (string.IsNullOrEmpty(orderId))
                {
                    return Json(new { paid = false, message = "订单号无效" });
                }

                // 查找订单
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

                // 只返回支付状态
                return Json(new { 
                    paid = request.IsPaid,
                    message = request.IsPaid ? "已支付" : "等待支付确认"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询支付状态失败");
                return Json(new { paid = false, message = "查询失败，请重试" });
            }
        }

        /// <summary>
        /// 支付成功页面
        /// URL: /Pay/Success?orderId=xxx
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

            // 获取订单信息
            ResourceRequest? request = null;
            if (int.TryParse(orderId, out var requestId))
            {
                request = await _dataSync.GetResourceRequestByIdAsync(requestId);
            }
            else
            {
                request = await _dataSync.GetResourceRequestByOrderIdAsync(orderId);
            }

            if (request != null)
            {
                ViewBag.Amount = request.Amount;
                ViewBag.Description = request.ResourceName;
            }

            return View();
        }

        /// <summary>
        /// 支付失败页面
        /// URL: /Pay/Failure?errorCode=xxx
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
