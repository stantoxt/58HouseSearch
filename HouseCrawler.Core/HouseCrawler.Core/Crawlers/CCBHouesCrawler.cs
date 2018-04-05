﻿using HouseCrawler.Core.DataContent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseCrawler.Core.Crawlers
{
    public class CCBHouesCrawler
    {

        private static readonly CrawlerDataContent DataContent = new CrawlerDataContent();


        public static void CaptureHouseInfo()
        {

            foreach (var crawlerConfiguration in DataContent.CrawlerConfigurations.Where(c => c.ConfigurationName
            == ConstConfigurationName.CCBHouse).ToList())
            {

                CaptureHouse(crawlerConfiguration);

                LogHelper.RunActionNotThrowEx(() =>
                {
                }, "CapturPinPaiHouseInfo", crawlerConfiguration);

            }

        }

        private static void CaptureHouse(BizCrawlerConfiguration crawlerConfiguration)
        {
            var confInfo = JsonConvert.DeserializeObject<dynamic>(crawlerConfiguration.ConfigurationValue);
            if (confInfo.shortcutname==null || string.IsNullOrEmpty(confInfo.shortcutname.Value))
            {
                return;
            }
            string cityShortCutName = confInfo.shortcutname.Value;
            for (var pageNum = 1; pageNum < confInfo.pagecount.Value; pageNum++)
            {
                var result = GetResultByAPI(cityShortCutName, pageNum);
                List<CCBHouseInfo> houseList = GetHouseData(cityShortCutName, result);
                DataContent.CCBHouseInfos.AddRange(houseList);
                DataContent.SaveChanges();
            }
        }

        private static List<CCBHouseInfo> GetHouseData(string cityShortCutName, string result)
        {
            var houseList = new List<CCBHouseInfo>();
            if (string.IsNullOrEmpty(result))
            {
                return houseList;
            }

            var resultJObject = JsonConvert.DeserializeObject<JObject>(result);
            foreach (var item in resultJObject["items"])
            {
                CCBHouseInfo houseInfo = new CCBHouseInfo();
                string houseURL = GetHouseOnlineURL(cityShortCutName, item);
                if (DataContent.CCBHouseInfos.Any(h => h.HouseOnlineURL == houseURL))
                    continue;
                houseInfo.HouseOnlineURL = houseURL;
                houseInfo.HouseLocation = item["headline"].ToObject<string>();
                houseInfo.HouseTitle = item["headline"].ToObject<string>();
                houseInfo.LocationCityName = item["cityName"].ToObject<string>(); 
                houseInfo.HouseText = item.ToString();
                houseInfo.HousePrice = item["totalPrice"].ToObject<Int32>();
                houseInfo.DisPlayPrice = item["totalPrice"].ToString();
                houseInfo.DataCreateTime = DateTime.Now;
                houseInfo.PubTime = item["publishTime"].ToObject<DateTime>();
                houseInfo.Source = ConstConfigurationName.CCBHouse;
                houseList.Add(houseInfo);
            }

            return houseList;
        }

        private static string GetHouseOnlineURL(string cityShortCutName, JToken item)
        {
            var houseURL = "";
            if (!string.IsNullOrEmpty(item["web_url"].ToString()))
            {
                houseURL = item["web_url"].ToString();
            }
            else if (!string.IsNullOrEmpty(item["app_url"].ToString()))
            {
                houseURL = item["app_url"].ToString();
            }
            else
            {
                houseURL = $"http://{cityShortCutName}.jiayuan.home.ccb.com/lease/{ item["dealCode"].ToString()}.html";
            }

            return houseURL;
        }

        private static string GetResultByAPI(string cityShortCutName, int page)
        {
            string formBody = $"_reqParams=apiKey%3D{ConnectionStrings.CCBHomeAPIKey}%26city%3D{cityShortCutName}%26saleOrLease%3Dlease%26pageSize%3D50%26page%3D{page}%26propType%3D11%26tmflags%3D3&_interfaceUrl=%2Fhlsp%2Fcityhouse%2Fdeal%2Fsearch&_reqMethod=GET";
            var client = new RestClient("http://bankservice.home.ccb.com/LHECISM/LanHaiHttpResfulReqServlet");
            var request = new RestRequest(Method.POST);
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("cookie2", "$Version=1");
            request.AddHeader("cookie", "UDC_Ser2018_ON=1; FAVOR=||||||||||||||||||||||||||||||||||||||||||||||||||; CCBIBS1=Qief3XbaeklLa3FZfwVHnEaGnFVRS3EOeEVxmbDcfhlWbc2zwKnabcKNraFiWdHUel1baPIKeD1K2yHHeSVJmyFMf0lvqqR9Vw3Xli; TC=249277366_1613198604_1362849648; UDC_ON=1; _BOA_mf_txcode_=HT0205");
            request.AddHeader("host", "bankservice.home.ccb.com");
            request.AddParameter("application/x-www-form-urlencoded", formBody, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful) {
                return response.Content;
            }
            return "";
        }
    }
}
