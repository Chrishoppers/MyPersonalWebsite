using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    // ============================================================
    // 列车经停站信息
    // ============================================================
    public class TrainStop
    {
        public int StationNo { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string ArriveTime { get; set; } = string.Empty;
        public string DepartTime { get; set; } = string.Empty;
        public string StopTime { get; set; } = string.Empty;
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        public int DayOffset { get; set; }
    }

    // ============================================================
    // 列车完整信息（含实时状态）
    // ============================================================
    public class TrainFullInfo
    {
        public string TrainCode { get; set; } = string.Empty;
        public string TrainType { get; set; } = string.Empty;
        public string StartStation { get; set; } = string.Empty;
        public string EndStation { get; set; } = string.Empty;
        public DateTime QueryTime { get; set; } = DateTime.Now;
        public List<TrainStop> Stops { get; set; } = new();

        // 实时状态（根据当前时间计算）
        public string Status { get; set; } = "未发车";
        public string CurrentStation { get; set; } = string.Empty;
        public string NextStation { get; set; } = string.Empty;
        public string NextArriveTime { get; set; } = string.Empty;
        public string DelayInfo { get; set; } = "正点";
        public int ProgressPercent { get; set; } = 0;
    }

    // ============================================================
    // 第三方API响应格式（使用第三方免费API）
    // ============================================================
    public class TrainApiResponse
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public TrainApiData? Data { get; set; }
    }

    public class TrainApiData
    {
        public string? TrainCode { get; set; }
        public string? StartStation { get; set; }
        public string? EndStation { get; set; }
        public List<TrainApiStop>? Stops { get; set; }
    }

    public class TrainApiStop
    {
        public int StationNo { get; set; }
        public string? StationName { get; set; }
        public string? ArriveTime { get; set; }
        public string? DepartTime { get; set; }
        public string? StopTime { get; set; }
        public bool IsStart { get; set; }
        public bool IsEnd { get; set; }
        public int DayOffset { get; set; }
    }
}
