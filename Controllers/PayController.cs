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
        [Route("Pay/Index/{id?}")]
        [Route("Pay/{id?}")]
        public async Task<IActionResult> Index(string? id)
        {
            try
            {
                _logger.LogInformation($"🔍 Pay/Index 被调用, id={id}");

                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("⚠️ 用户未登录，跳转到登录页");
                    return RedirectToAction("Login", "Auth");
                }

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("⚠️ id 为空");
                    TempData["Error"] = "无效的支付请求";
                    return RedirectToAction("History", "Resource");
                }

                if (!int.TryParse(id, out var requestId) || requestId <= 0)
                {
                    _logger.LogWarning($"⚠️ id 无效: {id}");
                    TempData["Error"] = "无效的支付请求";
                    return RedirectToAction("History", "Resource");
                }

                _logger.LogInformation($"🔍 查询订单 ID: {requestId}");

                var request = await _dataSync.GetResourceRequestByIdAsync(requestId);

                if (request == null)
                {
                    _logger.LogWarning($"⚠️ 订单不存在: {requestId}");
                    TempData["Error"] = "订单不存在";
                    return RedirectToAction("History", "Resource");
                }

                _logger.LogInformation($"✅ 找到订单: {request.OrderId}, 用户: {request.UserId}");

                if (request.UserId != userId.Value)
                {
                    _logger.LogWarning($"⚠️ 订单不属于当前用户: {request.UserId} != {userId.Value}");
                    TempData["Error"] = "该订单不属于您";
                    return RedirectToAction("History", "Resource");
                }

                if (request.IsPaid)
                {
                    _logger.LogInformation($"ℹ️ 订单已支付: {request.OrderId}");
                    TempData["Message"] = "该订单已支付";
                    return RedirectToAction("History", "Resource");
                }

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
                _logger.LogError(ex, $"❌ Pay/Index 异常: {ex.Message}");
                TempData["Error"] = "系统繁忙，请稍后重试";
                return RedirectToAction("History", "Resource");
            }
        }

        /// <summary>
        /// 查询支付状态
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

                return Json(new { paid = request.IsPaid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ CheckStatus 异常: {ex.Message}");
                return Json(new { paid = false, message = "查询失败" });
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
