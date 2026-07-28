using System;
using System.Collections.Generic;

namespace MyPersonalWebsite.Models
{
    // ============================================================
    // 车站详细信息
    // ============================================================
    public class StationDetail
    {
        public string Name { get; set; } = string.Empty;           // 车站名称
        public string Code { get; set; } = string.Empty;           // 车站代码（三字码）
        public string Pinyin { get; set; } = string.Empty;         // 拼音
        public string City { get; set; } = string.Empty;           // 所在城市
        public string Province { get; set; } = string.Empty;       // 所在省份
        public string Address { get; set; } = string.Empty;        // 详细地址
        public string Type { get; set; } = "普通站";               // 车站类型：特等站/一等站/二等站/三等站
        public string BuildingArea { get; set; } = string.Empty;   // 建筑面积
        public string PlatformCount { get; set; } = string.Empty;  // 站台数量
        public string LineCount { get; set; } = string.Empty;      // 线路数量
        public string NearbyTransport { get; set; } = string.Empty; // 附近交通（地铁/公交）
        public string Description { get; set; } = string.Empty;    // 简介
        public double? Latitude { get; set; }                      // 纬度
        public double? Longitude { get; set; }                     // 经度
    }

    // ============================================================
    // 经停站详细信息（含站台/开门方向）
    // ============================================================
    public class TrainStopDetail : TrainStop
    {
        public string Platform { get; set; } = string.Empty;       // 停靠站台（如 16站台、2F）
        public string DoorDirection { get; set; } = string.Empty;  // 开门方向（左侧/右侧/不固定）
        public string TrackNumber { get; set; } = string.Empty;    // 股道号
        public string StationType { get; set; } = string.Empty;    // 车站类型
        public string WaitingArea { get; set; } = string.Empty;    // 候车区域
        public StationDetail? StationInfo { get; set; }            // 车站详细信息

        // 进站引导信息
        public string BoardingGuide { get; set; } = string.Empty;  // 乘车引导说明
        public string PlatformSide { get; set; } = string.Empty;   // 站台方向（左侧/右侧下车）
        public string LandmarkColor { get; set; } = string.Empty;  // 地标颜色（黄/绿/蓝/紫）
        public string CarriageDirection { get; set; } = string.Empty; // 车厢方向（向前/向后）
        public string SpecialNote { get; set; } = string.Empty;    // 特殊提示（如：换乘通道、无电梯等）
    }

    // ============================================================
    // 列车完整信息（含详细数据）
    // ============================================================
    public class TrainFullDetailInfo : TrainFullInfo
    {
        public List<TrainStopDetail> DetailStops { get; set; } = new();
        public int TotalStops { get; set; }
        public string TotalDistance { get; set; } = string.Empty;  // 总里程
        public string TotalDuration { get; set; } = string.Empty;  // 全程用时
        public string TrainBrand { get; set; } = string.Empty;     // 车型品牌（复兴号/和谐号）
        public string TrainModel { get; set; } = string.Empty;     // 车型号（CR400AF等）
        public string Consist { get; set; } = string.Empty;        // 编组（8节/16节）
        public string MaxSpeed { get; set; } = string.Empty;       // 最高速度

        // 实时状态增强
        public string NextStationPlatform { get; set; } = string.Empty;
        public string NextStationDoorSide { get; set; } = string.Empty;
        public string CurrentStationInfo { get; set; } = string.Empty;
    }
}
