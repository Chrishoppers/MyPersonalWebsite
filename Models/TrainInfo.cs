using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    // ============================================================
    // 车站详细信息
    // ============================================================
    public class StationDetail
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Type { get; set; } = "普通站";
        public string BuildingArea { get; set; } = string.Empty;
        public string PlatformCount { get; set; } = string.Empty;
        public string LineCount { get; set; } = string.Empty;
        public string NearbyTransport { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    // ============================================================
    // 经停站信息（基础版）
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
    // 经停站详细信息（含站台/开门方向）
    // ============================================================
    public class TrainStopDetail : TrainStop
    {
        public string Platform { get; set; } = string.Empty;
        public string DoorDirection { get; set; } = string.Empty;
        public string TrackNumber { get; set; } = string.Empty;
        public string StationType { get; set; } = string.Empty;
        public string WaitingArea { get; set; } = string.Empty;
        public StationDetail? StationInfo { get; set; }
        public string BoardingGuide { get; set; } = string.Empty;
        public string PlatformSide { get; set; } = string.Empty;
        public string LandmarkColor { get; set; } = string.Empty;
        public string CarriageDirection { get; set; } = string.Empty;
        public string SpecialNote { get; set; } = string.Empty;
        public string NearbyTransport { get; set; } = string.Empty;
    }

    // ============================================================
    // 列车完整信息
    // ============================================================
    public class TrainFullInfo
    {
        public string TrainCode { get; set; } = string.Empty;
        public string TrainType { get; set; } = string.Empty;
        public string StartStation { get; set; } = string.Empty;
        public string EndStation { get; set; } = string.Empty;
        public DateTime QueryTime { get; set; } = DateTime.Now;
        public List<TrainStop> Stops { get; set; } = new();
        public string Status { get; set; } = "未发车";
        public string CurrentStation { get; set; } = string.Empty;
        public string NextStation { get; set; } = string.Empty;
        public string NextArriveTime { get; set; } = string.Empty;
        public string DelayInfo { get; set; } = "正点";
        public int ProgressPercent { get; set; } = 0;
    }

    // ============================================================
    // 列车完整详细信息（含增强数据）
    // ============================================================
    public class TrainFullDetailInfo : TrainFullInfo
    {
        public List<TrainStopDetail> DetailStops { get; set; } = new();
        public int TotalStops { get; set; }
        public string TotalDistance { get; set; } = string.Empty;
        public string TotalDuration { get; set; } = string.Empty;
        public string TrainBrand { get; set; } = string.Empty;
        public string TrainModel { get; set; } = string.Empty;
        public string Consist { get; set; } = string.Empty;
        public string MaxSpeed { get; set; } = string.Empty;
        public string NextStationPlatform { get; set; } = string.Empty;
        public string NextStationDoorSide { get; set; } = string.Empty;
        public string CurrentStationInfo { get; set; } = string.Empty;
    }

    // ============================================================
    // API 请求模型
    // ============================================================
    public class TrainQueryRequest
    {
        public string TrainCode { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }

    // ============================================================
    // 12306 API 响应模型
    // ============================================================
    public class McpTrainResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public McpTrainData? Data { get; set; }
    }

    public class McpTrainData
    {
        public string TrainCode { get; set; } = string.Empty;
        public string TrainType { get; set; } = string.Empty;
        public string StartStation { get; set; } = string.Empty;
        public string EndStation { get; set; } = string.Empty;
        public List<McpTrainStop> Stops { get; set; } = new();
    }

    public class McpTrainStop
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
}
