using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Hubs;

namespace MyPersonalWebsite.Services
{
    public class WerewolfVoiceService
    {
        private readonly IHubContext<WerewolfHub> _hubContext;

        // 需要重复两遍的播报（仅公共信息）
        private readonly HashSet<string> _repeatTwoTimes = new HashSet<string>
        {
            "天黑请闭眼",
            "天亮了",
            "狼人自爆",
            "被放逐",
            "当选警长",
            "游戏结束",
            "好人阵营获胜",
            "狼人阵营获胜"
        };

        public WerewolfVoiceService(IHubContext<WerewolfHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// 公共语音播报（所有玩家都能听到）
        /// </summary>
        public async Task AnnounceAsync(string roomId, string message, bool repeat = false)
        {
            // 如果是重要播报，重复两遍
            if (repeat || ShouldRepeat(message))
            {
                await _hubContext.Clients.Group(roomId).SendAsync("VoiceAnnounce", message, "important");
                await Task.Delay(800);
                await _hubContext.Clients.Group(roomId).SendAsync("VoiceAnnounce", message, "important");
            }
            else
            {
                await _hubContext.Clients.Group(roomId).SendAsync("VoiceAnnounce", message, "normal");
            }
        }

        /// <summary>
        /// 仅播报给指定玩家（神职专属信息，不公开）
        /// </summary>
        public async Task WhisperToPlayerAsync(string connectionId, string message)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("VoiceWhisper", message);
        }

        private bool ShouldRepeat(string message)
        {
            foreach (var keyword in _repeatTwoTimes)
            {
                if (message.Contains(keyword))
                    return true;
            }
            return false;
        }
    }
}
