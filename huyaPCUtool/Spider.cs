using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using HttpHelpers;

namespace huyaPCUtool
{
    public class Spider
    {
        private Timer timer;
        private string url;


        private string roomid;

        public Spider(string url, double interval)
        {
            timer = new Timer(interval);
            timer.Elapsed += Timer_Elapsed;

            this.url = url;

            var k = url + "*";
            roomid = Between(k, "com/", "*");



            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var r = http.getHtml("http://www.huya.com/bashen");

            var rr = new rec
            {
                timesamp = ConvertDateTimeInt(DateTime.Now),
                roomid = roomid,
                activitycount = Between(r, "<div id=\"activityCount\">", "<"),
                livecount = Between(r, "id=\"live-count\">", "<")
            };
            //写入数据库
            Form1.Cmd(
                    $"INSERT into rec (roomname,timesamp,livecount,activityCount)values('{rr.roomid}','{rr.timesamp}','{rr.livecount}','{rr.activitycount}')");

            Console.Out.WriteLine("done");
        }

        /// <summary>
        /// 取文本中间内容
        /// </summary>
        /// <param name="str">原文本</param>
        /// <param name="leftstr">左边文本</param>
        /// <param name="rightstr">右边文本</param>
        /// <returns>返回中间文本内容</returns>
        public static string Between(string str, string leftstr, string rightstr)
        {
            var i = str.IndexOf(leftstr) + leftstr.Length;
            var temp = str.Substring(i, str.IndexOf(rightstr, i) - i);
            return temp;
        }
        /// <summary>  
        /// 时间戳转为C#格式时间  
        /// </summary>  
        /// <param name="timeStamp">Unix时间戳格式</param>  
        /// <returns>C#格式时间</returns>  
        public static DateTime GetTime(string timeStamp)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(timeStamp + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }


        /// <summary>  
        /// DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time"> DateTime时间格式</param>  
        /// <returns>Unix时间戳格式</returns>  
        public static string ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return ((int)(time - startTime).TotalSeconds).ToString();
        }

    }
}
