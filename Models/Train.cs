namespace MyPersonalWebsite.Models
{
    public enum CarriageType { Business, FirstClass, SecondClass }
    public enum OrderStatus { Queued, Processing, Success, Failed, Paid, Waitlisted }
    public enum PassengerType { Adult, Child, Student }

    public class Train
    {
        public string TrainNumber { get; set; }
        public string TrainName { get; set; }
        public List<Station> Stations { get; set; } = new();
        public List<Carriage> Carriages { get; set; } = new();
        public DateTime DepartureTime { get; set; } = DateTime.Now.Date.AddHours(7);
        public int MaxSpeed { get; set; } = 300;
        public int TotalSeats { get; set; }
        public int SoldSeats { get; set; }

        public Train(string number, string name)
        {
            TrainNumber = number;
            TrainName = name;
            // 车厢布局：1/10 商务座，2/9 一等座，3-8 二等座
            for (int i = 1; i <= 10; i++)
            {
                CarriageType type = (i == 1 || i == 10) ? CarriageType.Business
                                  : (i == 2 || i == 9) ? CarriageType.FirstClass
                                  : CarriageType.SecondClass;
                Carriages.Add(new Carriage(i, type));
            }
            TotalSeats = Carriages.Sum(c => c.Seats.Count);
        }

        public int DistanceBetween(string from, string to)
        {
            int fi = Stations.FindIndex(s => s.StationName == from);
            int ti = Stations.FindIndex(s => s.StationName == to);
            if (fi < 0 || ti <= fi) return 0;
            return Stations[ti].Distance - Stations[fi].Distance;
        }

        public Station? GetStation(string name)
            => Stations.FirstOrDefault(s => s.StationName == name);
    }

    public class Station
    {
        public int StationOrder { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public int Distance { get; set; }          // 距始发站的公里数
        public int ArriveMinutes { get; set; }     // 自始发起的到达分钟数
        public int DepartMinutes { get; set; }     // 自始发起的发车分钟数
        public int StopMinutes { get; set; } = 3;

        public Station(int order, string name, string code, int distance, int arrive, int depart)
        {
            StationOrder = order;
            StationName = name;
            StationCode = code;
            Distance = distance;
            ArriveMinutes = arrive;
            DepartMinutes = depart;
        }
    }

    public class Carriage
    {
        public int CarriageNumber { get; set; }
        public CarriageType Type { get; set; }
        public List<Seat> Seats { get; set; } = new();

        public Carriage(int num, CarriageType type)
        {
            CarriageNumber = num;
            Type = type;
            string[] cols = type == CarriageType.Business ? new[] { "A", "F" }
                          : type == CarriageType.FirstClass ? new[] { "A", "C", "D", "F" }
                          : new[] { "A", "B", "C", "D", "F" };
            int rows = type == CarriageType.Business ? 8 : 10;
            for (int r = 1; r <= rows; r++)
                foreach (string c in cols)
                    Seats.Add(new Seat(num, r, c, type));
        }

        public static string TypeName(CarriageType t) => t switch
        {
            CarriageType.Business => "商务座",
            CarriageType.FirstClass => "一等座",
            _ => "二等座"
        };
    }

    public class Seat
    {
        public int CarriageNumber { get; set; }
        public int Row { get; set; }
        public string Column { get; set; }
        public CarriageType Type { get; set; }
        public string SeatNumber => $"{CarriageNumber}车{Row}{Column}";
        public Dictionary<string, bool> SectionOccupied { get; set; } = new();

        public Seat(int cn, int r, string c, CarriageType t)
        {
            CarriageNumber = cn;
            Row = r;
            Column = c;
            Type = t;
        }

        public bool IsWindow => Column is "A" or "F";
    }
}
