using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HttpHelpers;
using Timer = System.Timers.Timer;
using NLog;

namespace huyaPCUtool
{
    public partial class Form1 : Form
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static SQLiteConnection _db;
        public Form1()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            logger.Info("started");

        }

        List<Spider> spiders = new List<Spider>();


        private Timer timer;

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckSqlFile();

            // getallup();


            timer = new Timer(40000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

        }

        private int count = 0;
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            getallup();
            count++;
            label2.Text = count.ToString();
            logger.Info("count={0}", count);
        }

        public void getallup()
        {
            var sw = new Stopwatch();
            sw.Start();



            var swr = sw.ElapsedMilliseconds;
            var alluphtml = http.getHtml("http://www.huya.com/g/hw");

            logger.Info("getallhtml time={0}", sw.ElapsedMilliseconds - swr);


            var re = new Regex("game-live-item\">(\\s*?)<a href=\"([\\s\\S]*?)\" class=\"([\\s\\S]*?)' title=\"([\\s\\S]*?)\" target=\"([\\s\\S]*?)nick\" title=\"([\\s\\S]*?)\">([\\s\\S]*?)\"js-num\">([\\s\\S]*?)<");
            var ms = re.Matches(alluphtml);
            var tran = _db.BeginTransaction();

            //2 4 6 8
            logger.Info("ms={0}", ms.Count);

            var ts = Spider.ConvertDateTimeInt(DateTime.Now);

            var totalcount = 0;

            var rs = new List<rec>();

            foreach (Match m in ms)
            {
                var url = m.Groups[2].ToString();
                url += "*";
                var r = new rec()
                {
                    roomid = Spider.Between(url, "com/", "*"),
                    title = m.Groups[4].ToString(),
                    nick = m.Groups[6].ToString(),
                    timesamp = ts,

                };


                //得到新的数字

                var room = http.getHtml(m.Groups[2].ToString());

                r.activitycount = Spider.Between(room, "<div id=\"activityCount\">", "<");
                r.livecount = Spider.Between(room, "id=\"live-count\">", "<");
                r.livecount = r.livecount.Replace(",", "");


                totalcount += int.Parse(r.livecount);


                rs.Add(r);

            }

            foreach (var r in rs)
            {
                Form1.Cmd(
             $"INSERT into rec " +
             $"(roomid,title,nick,timesamp,livecount,activityCount,totalcount)" +
             $"values" +
             $"('{r.roomid}','{r.title}','{r.nick}','{r.timesamp}','{r.livecount}','{r.activitycount}','{totalcount}')",
             tran);
            }



            tran.Commit();

            sw.Stop();

            label4.Text = sw.Elapsed.TotalSeconds + " S";
            label6.Text = ms.Count.ToString();


            logger.Info("count={0}, time={1}s done", ms.Count, sw.Elapsed.TotalSeconds);

        }


        private static void CreatSqlTable()
        {

            Cmd("create table rec (no integer primary key,roomid varchar(20),title varchar(20),nick varchar(20),timesamp varchar(20),livecount varchar(20),activityCount varchar(20),totalcount varchar(20))");

        }
        private static void CheckSqlFile()
        {
            if (!File.Exists("db.db"))
            {
                SQLiteConnection.CreateFile("db.db");

                Opendb();
                CreatSqlTable();
            }
            else
            {
                Opendb();
            }

        }
        private static void Opendb()
        {
            _db = new SQLiteConnection("Data Source=db.db");
            _db.Open();
        }

        public static int Cmd(string cmd, SQLiteTransaction tran = null)
        {

            var command = new SQLiteCommand(cmd, _db);
            if (tran != null)
            {
                command.Transaction = tran;
            }

            return command.ExecuteNonQuery();
        }
        public static DataTable GetSql(string sql, DataTable sdt = null)
        {
            var command = new SQLiteCommand(sql, _db);

            var da = new SQLiteDataAdapter(command);


            if (sdt == null)
            {
                sdt = new DataTable();
            }

            da.Fill(sdt);
            return sdt;




        }
        private void button1_Click(object sender, EventArgs e)
        {


            var t = new Thread(writefile);
            t.Start();
        }

        private void writefile()
        {
            if (textBox1.Text == "")
                return;


            var dt = GetSql($"SELECT * FROM rec where nick='{textBox1.Text}'");



            logger.Info("write temp start");
            logger.Info("count=" + dt.Rows.Count);
            var str = "[";

            var lastcount = "";

            foreach (DataRow r in dt.Rows)
            {

                //去重
                var livecount = (r["livecount"].ToString()).Replace(",", "");
                if (livecount != lastcount)
                    str += $"['{r["timesamp"]}',{livecount},'{r["nick"]}',{r["activitycount"]}],";
            }
            str += "]";
            logger.Info("add done");

            var fs = File.Create("tmep.txt");
            var bs = Encoding.UTF8.GetBytes(str);
            fs.Write(bs, 0, bs.Length);
            logger.Info("write temp done");
        }

    }

    public struct rec
    {
        public string roomid;
        public string title;
        public string nick;
        public string timesamp;
        public string livecount;
        public string activitycount;
        public string totalcount;

        public override string ToString()
        {
            return $"title={title},timesamp={timesamp},livecount={livecount},activitycount={activitycount}";
        }
    }


}
