namespace MyPersonalWebsite.Models
{
    public class Order
    {
        public string OrderId { get; set; }
        public string TrainNumber { get; set; } = "";
        public string TrainName { get; set; } = "";
        public string FromStation { get; set; } = "";
        public string ToStation { get; set; } = "";
        public string DepartureTime { get; set; } = "";
        public string ArrivalTime { get; set; } = "";
        public List<string> PassedStations { get; set; } = new();
        public List<PassengerInfo> Passengers { get; set; } = new();
        public OrderStatus Status { get; set; } = OrderStatus.Queued;
        public string FailReason { get; set; } = "";
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime? ProcessTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public List<string> AssignedSeats { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public int AttemptCount { get; set; }
        public string Source { get; set; } = "user";   // user / simulated
        public int MaxWaitlistRetries { get; set; } = 2;

        public Order()
        {
            OrderId = GenerateOrderId();
        }

        private static string GenerateOrderId()
        {
            string s = Guid.NewGuid().ToString("N");
            return "E" + s[..9].ToUpper();
        }
    }

    public class PassengerInfo
    {
        public string Name { get; set; }
        public string PreferredSeat { get; set; }
        public string SeatClass { get; set; }
        public string IdNumber { get; set; }
        public PassengerType PassengerType { get; set; } = PassengerType.Adult;
        public int Age { get; set; } = 25;
        public string Gender { get; set; } = "男";

        public PassengerInfo(string name, string preferredSeat, string seatClass)
        {
            Name = name;
            PreferredSeat = preferredSeat ?? "";
            SeatClass = seatClass;
            IdNumber = GenerateId();
        }

        public string TypeName => PassengerType switch
        {
            PassengerType.Child => "儿童",
            PassengerType.Student => "学生",
            _ => "成人"
        };

        public decimal Discount => PassengerType switch
        {
            PassengerType.Child => 0.5m,
            PassengerType.Student => 0.75m,
            _ => 1.0m
        };

        private static string GenerateId()
            => $"{Random.Shared.Next(100000, 999999)}{Random.Shared.Next(100000, 999999)}0000";
    }
}
