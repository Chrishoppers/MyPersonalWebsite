namespace MyPersonalWebsite.Models
{
    /// <summary>
    /// 真实抢票任务中的一位目标乘客及其要求。
    /// </summary>
    public class TicketTaskPassenger
    {
        public string Name { get; set; } = "";
        public string IdNumber { get; set; } = "";
        public string PassengerType { get; set; } = "成人";   // 成人 / 儿童 / 学生
        public string SeatClass { get; set; } = "二等座";   // 二等座 / 一等座 / 商务座
        public string SeatPref { get; set; } = "any";        // any / window / aisle / 具体列字母
        public string TimePeriod { get; set; } = "";         // 上午 / 下午 / 晚间 / HH:mm
        public string StationPref { get; set; } = "";        // 南站 / 北站 / 东站 / 西站 / 虹桥站 / 不限
        public string PayMethod { get; set; } = "alipay";    // alipay / wechat / card / unionpay / ecard
        public string PayPassword { get; set; } = "123456";  // 6 位数字
    }

    /// <summary>
    /// 一个评分维度。
    /// </summary>
    public class TicketTaskDimension
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public int Earned { get; set; }
        public int Max { get; set; }
        public bool Passed { get; set; }
        public string Note { get; set; } = "";
    }

    /// <summary>
    /// 选择乘客列表中的一个候选项（可能为目标乘客，也可能是干扰项）。
    /// </summary>
    public class PassengerCandidate
    {
        public string Name { get; set; } = "";
        public string IdNumber { get; set; } = "";
        public bool IsTarget { get; set; }                  // 服务端判定用，不下发给客户端
        public string Role { get; set; } = "";              // 干扰项身份说明，如"同事""邻居"
    }

    /// <summary>
    /// 真实抢票任务。
    /// </summary>
    public class TicketTask
    {
        public string TaskId { get; set; } = "";
        public string Difficulty { get; set; } = "normal";
        public string FromCity { get; set; } = "";
        public string ToCity { get; set; } = "";
        public string FromStation { get; set; } = "";       // 答案：正确出发站
        public string ToStation { get; set; } = "";         // 答案：正确到达站
        public string FromStationHint { get; set; } = "";   // 下发给玩家的出发站提示
        public string ToStationHint { get; set; } = "";     // 下发给玩家的到达站提示
        public string TrainNumber { get; set; } = "";       // 答案：正确车次
        public string TrainName { get; set; } = "";
        public string DepartureTime { get; set; } = "";     // 正确车次的发车时刻 HH:mm
        public string ArrivalTime { get; set; } = "";
        public List<TicketTaskPassenger> Passengers { get; set; } = new();
        public List<PassengerCandidate> Candidates { get; set; } = new();
        public int TimeLimitSeconds { get; set; } = 300;
        public DateTime Deadline { get; set; }
        public DateTime StartTime { get; set; }
        public bool Active { get; set; }
        public bool Submitted { get; set; }
        public bool Paid { get; set; }
        public int Score { get; set; }
        public string Rank { get; set; } = "";
        public List<string> Feedback { get; set; } = new();
        public List<TicketTaskDimension> Dimensions { get; set; } = new();
        public string LastOrderId { get; set; } = "";
        public string PayPassword { get; set; } = "";       // 统一支付密码（答案）
        public string PayMethod { get; set; } = "";         // 要求支付方式（答案）
    }
}
