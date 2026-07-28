using System;
using System.Collections.Generic;
using System.Linq;
using MyPersonalWebsite.Models;

namespace MyPersonalWebsite.Services
{
    public static class StationDataService
    {
        // ============================================================
        // 内置车站信息库
        // ============================================================
        public static Dictionary<string, StationDetail> GetStationInfo()
        {
            return new Dictionary<string, StationDetail>(StringComparer.OrdinalIgnoreCase)
            {
                // 北京
                { "北京南", new StationDetail {
                    Name = "北京南站", Code = "BJP", City = "北京", Province = "北京市",
                    Address = "北京市丰台区永外大街12号",
                    Type = "特等站", BuildingArea = "32万平方米", PlatformCount = "13台24线",
                    NearbyTransport = "地铁4号线、14号线", 
                    Description = "亚洲最大的火车站之一，京沪高铁、京津城际始发站" } },
                { "北京西", new StationDetail {
                    Name = "北京西站", Code = "BXP", City = "北京", Province = "北京市",
                    Address = "北京市丰台区莲花池东路118号",
                    Type = "特等站", BuildingArea = "70万平方米", PlatformCount = "10台20线",
                    NearbyTransport = "地铁7号线、9号线",
                    Description = "京广高铁、京九铁路始发站" } },
                { "北京北", new StationDetail {
                    Name = "北京北站", Code = "BAP", City = "北京", Province = "北京市",
                    Address = "北京市西城区西直门外大街1号",
                    Type = "一等站", PlatformCount = "6台11线",
                    NearbyTransport = "地铁2号线、4号线、13号线",
                    Description = "京张高铁始发站" } },

                // 上海
                { "上海", new StationDetail {
                    Name = "上海站", Code = "SHH", City = "上海", Province = "上海市",
                    Address = "上海市静安区秣陵路303号",
                    Type = "特等站", PlatformCount = "13台15线",
                    NearbyTransport = "地铁1号线、3号线、4号线",
                    Description = "上海主要普速列车始发站" } },
                { "上海虹桥", new StationDetail {
                    Name = "上海虹桥站", Code = "AOH", City = "上海", Province = "上海市",
                    Address = "上海市闵行区申虹路",
                    Type = "特等站", BuildingArea = "44万平方米", PlatformCount = "16台30线",
                    NearbyTransport = "地铁2号线、10号线、17号线",
                    Description = "华东地区最大的高铁枢纽站，京沪高铁、沪昆高铁始发站" } },

                // 广州
                { "广州南", new StationDetail {
                    Name = "广州南站", Code = "IZQ", City = "广州", Province = "广东省",
                    Address = "广州市番禺区石壁街道",
                    Type = "特等站", BuildingArea = "61.5万平方米", PlatformCount = "15台28线",
                    NearbyTransport = "地铁2号线、7号线、22号线",
                    Description = "华南地区最大的高铁站，京广高铁、广深港高铁始发站" } },
                { "广州", new StationDetail {
                    Name = "广州站", Code = "GZQ", City = "广州", Province = "广东省",
                    Address = "广州市越秀区环市西路159号",
                    Type = "特等站", PlatformCount = "4台7线",
                    NearbyTransport = "地铁2号线、5号线",
                    Description = "广州主要普速列车始发站" } },

                // 深圳
                { "深圳北", new StationDetail {
                    Name = "深圳北站", Code = "IOQ", City = "深圳", Province = "广东省",
                    Address = "深圳市龙华区民治街道",
                    Type = "特等站", BuildingArea = "18.2万平方米", PlatformCount = "11台20线",
                    NearbyTransport = "地铁4号线、5号线、6号线",
                    Description = "深圳主要高铁站，广深港高铁、厦深铁路枢纽" } },

                // 武汉
                { "武汉", new StationDetail {
                    Name = "武汉站", Code = "WHN", City = "武汉", Province = "湖北省",
                    Address = "武汉市洪山区武汉火车站",
                    Type = "特等站", BuildingArea = "35.5万平方米", PlatformCount = "11台20线",
                    NearbyTransport = "地铁4号线、5号线",
                    Description = "华中地区最大高铁枢纽站，京广高铁、沪汉蓉高铁交汇" } },

                // 成都
                { "成都东", new StationDetail {
                    Name = "成都东站", Code = "ICW", City = "成都", Province = "四川省",
                    Address = "成都市成华区青衣江路",
                    Type = "特等站", BuildingArea = "21.6万平方米", PlatformCount = "14台26线",
                    NearbyTransport = "地铁2号线、7号线",
                    Description = "西南地区最大高铁站，西成高铁、成贵高铁始发站" } },

                // 南京
                { "南京南", new StationDetail {
                    Name = "南京南站", Code = "NKH", City = "南京", Province = "江苏省",
                    Address = "南京市雨花台区玉兰路98号",
                    Type = "特等站", BuildingArea = "45.8万平方米", PlatformCount = "15台28线",
                    NearbyTransport = "地铁1号线、3号线、S1号线",
                    Description = "华东地区重要高铁枢纽，京沪高铁、沪宁城际交汇" } },

                // 杭州
                { "杭州东", new StationDetail {
                    Name = "杭州东站", Code = "HGH", City = "杭州", Province = "浙江省",
                    Address = "杭州市上城区全福桥路1号",
                    Type = "特等站", BuildingArea = "34万平方米", PlatformCount = "15台30线",
                    NearbyTransport = "地铁1号线、4号线、6号线、19号线",
                    Description = "长三角地区重要高铁枢纽站" } },

                // 郑州
                { "郑州东", new StationDetail {
                    Name = "郑州东站", Code = "ZAF", City = "郑州", Province = "河南省",
                    Address = "郑州市金水区心怡路199号",
                    Type = "特等站", BuildingArea = "41.2万平方米", PlatformCount = "16台32线",
                    NearbyTransport = "地铁1号线、5号线",
                    Description = "全国最大的高铁站之一，京广高铁、徐兰高铁交汇" } },

                // 西安
                { "西安北", new StationDetail {
                    Name = "西安北站", Code = "EAY", City = "西安", Province = "陕西省",
                    Address = "西安市未央区文景路北段",
                    Type = "特等站", PlatformCount = "18台34线",
                    NearbyTransport = "地铁2号线、4号线、14号线",
                    Description = "西北地区最大高铁站，徐兰高铁、西成高铁始发站" } },

                // 长沙
                { "长沙南", new StationDetail {
                    Name = "长沙南站", Code = "CWQ", City = "长沙", Province = "湖南省",
                    Address = "长沙市雨花区花侯路",
                    Type = "特等站", PlatformCount = "13台28线",
                    NearbyTransport = "地铁2号线、4号线",
                    Description = "中南地区重要高铁枢纽，京广高铁、沪昆高铁交汇" } },

                // 重庆
                { "重庆西", new StationDetail {
                    Name = "重庆西站", Code = "CXW", City = "重庆", Province = "重庆市",
                    Address = "重庆市沙坪坝区凤中路",
                    Type = "特等站", PlatformCount = "15台31线",
                    NearbyTransport = "地铁5号线、环线",
                    Description = "西南地区重要高铁站" } },

                // 天津
                { "天津", new StationDetail {
                    Name = "天津站", Code = "TJP", City = "天津", Province = "天津市",
                    Address = "天津市河北区新纬路1号",
                    Type = "特等站", PlatformCount = "10台18线",
                    NearbyTransport = "地铁2号线、3号线、9号线",
                    Description = "天津主要铁路枢纽" } },
                { "天津南", new StationDetail {
                    Name = "天津南站", Code = "TIP", City = "天津", Province = "天津市",
                    Address = "天津市西青区张家窝镇",
                    Type = "一等站", PlatformCount = "2台6线",
                    NearbyTransport = "地铁3号线",
                    Description = "京沪高铁经停站" } },

                // 济南
                { "济南西", new StationDetail {
                    Name = "济南西站", Code = "JGK", City = "济南", Province = "山东省",
                    Address = "济南市槐荫区齐鲁大道6号",
                    Type = "特等站", PlatformCount = "8台17线",
                    NearbyTransport = "地铁1号线",
                    Description = "京沪高铁重要经停站" } },

                // 石家庄
                { "石家庄", new StationDetail {
                    Name = "石家庄站", Code = "SJP", City = "石家庄", Province = "河北省",
                    Address = "石家庄市桥西区新石南路",
                    Type = "特等站", PlatformCount = "13台30线",
                    NearbyTransport = "地铁2号线、3号线",
                    Description = "京广高铁、石太高铁交汇站" } },

                // 南昌
                { "南昌西", new StationDetail {
                    Name = "南昌西站", Code = "NXG", City = "南昌", Province = "江西省",
                    Address = "南昌市红谷滩新区九龙湖",
                    Type = "特等站", PlatformCount = "12台26线",
                    NearbyTransport = "地铁2号线",
                    Description = "沪昆高铁、昌赣高铁枢纽" } },

                // 贵阳
                { "贵阳北", new StationDetail {
                    Name = "贵阳北站", Code = "KQW", City = "贵阳", Province = "贵州省",
                    Address = "贵阳市观山湖区西二环",
                    Type = "特等站", PlatformCount = "15台32线",
                    NearbyTransport = "地铁1号线",
                    Description = "西南地区重要高铁枢纽" } },

                // 昆明
                { "昆明南", new StationDetail {
                    Name = "昆明南站", Code = "KOM", City = "昆明", Province = "云南省",
                    Address = "昆明市呈贡区吴家营街道",
                    Type = "特等站", PlatformCount = "16台30线",
                    NearbyTransport = "地铁1号线、4号线",
                    Description = "沪昆高铁终点站，东南亚铁路枢纽" } },

                // 福州
                { "福州", new StationDetail {
                    Name = "福州站", Code = "FZS", City = "福州", Province = "福建省",
                    Address = "福州市晋安区华林路502号",
                    Type = "一等站", PlatformCount = "7台14线",
                    NearbyTransport = "地铁1号线",
                    Description = "福建省重要铁路枢纽" } },

                // 厦门
                { "厦门北", new StationDetail {
                    Name = "厦门北站", Code = "XMS", City = "厦门", Province = "福建省",
                    Address = "厦门市集美区后溪镇",
                    Type = "特等站", PlatformCount = "12台24线",
                    NearbyTransport = "地铁1号线",
                    Description = "沿海高铁重要枢纽站" } },

                // 青岛
                { "青岛", new StationDetail {
                    Name = "青岛站", Code = "QDK", City = "青岛", Province = "山东省",
                    Address = "青岛市市南区费县路1号",
                    Type = "一等站", PlatformCount = "6台10线",
                    NearbyTransport = "地铁1号线、3号线",
                    Description = "百年火车站，青岛主要普速始发站" } },

                // 大连
                { "大连", new StationDetail {
                    Name = "大连站", Code = "DLT", City = "大连", Province = "辽宁省",
                    Address = "大连市中山区长江路259号",
                    Type = "一等站", PlatformCount = "5台9线",
                    NearbyTransport = "地铁2号线",
                    Description = "东北地区重要火车站" } },

                // 沈阳
                { "沈阳", new StationDetail {
                    Name = "沈阳站", Code = "SYT", City = "沈阳", Province = "辽宁省",
                    Address = "沈阳市和平区胜利南街2号",
                    Type = "特等站", PlatformCount = "9台16线",
                    NearbyTransport = "地铁1号线",
                    Description = "东北地区最大火车站之一" } },

                // 长春
                { "长春", new StationDetail {
                    Name = "长春站", Code = "CCT", City = "长春", Province = "吉林省",
                    Address = "长春市宽城区长白路1号",
                    Type = "特等站", PlatformCount = "9台16线",
                    NearbyTransport = "地铁1号线、3号线",
                    Description = "吉林省铁路枢纽" } },

                // 哈尔滨
                { "哈尔滨西", new StationDetail {
                    Name = "哈尔滨西站", Code = "HBB", City = "哈尔滨", Province = "黑龙江省",
                    Address = "哈尔滨市南岗区哈尔滨大街",
                    Type = "特等站", PlatformCount = "10台22线",
                    NearbyTransport = "地铁3号线",
                    Description = "东北地区重要高铁站" } },
            };
        }

        // ============================================================
        // 获取车站信息（带模糊匹配）
        // ============================================================
        public static StationDetail? GetStationInfo(string stationName)
        {
            if (string.IsNullOrEmpty(stationName)) return null;

            var db = GetStationInfo();

            // 精确匹配
            if (db.ContainsKey(stationName))
                return db[stationName];

            // 模糊匹配（包含）
            var key = db.Keys.FirstOrDefault(k => k.Contains(stationName) || stationName.Contains(k));
            if (key != null)
                return db[key];

            // 去掉"站"字匹配
            var nameWithoutStation = stationName.Replace("站", "");
            key = db.Keys.FirstOrDefault(k => k.Replace("站", "").Contains(nameWithoutStation) || nameWithoutStation.Contains(k.Replace("站", "")));
            if (key != null)
                return db[key];

            return null;
        }
    }
}
