using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Timers;
/// <summary>
///BaseSetting 的摘要说明
/// </summary>
public class BaseSetting
{    
    //导出照片时，需要进行报名种类的识别
    public const string NCREReg = "NCREReg";  //计算机等级考试
    public const string EngReg = "EngReg";  //外语等级考试
    public const string MandarinReg = "MandarinReg";  //普通话报名
    public const string MHKReg = "MHKReg";  //MHK报名 


    #region 报名类型设置
    
    //存储四类考试的查找短语
    public const string NCREWord = "计算机等级";  //计算机报名的查找短语

    public const string EngTestType = "全国外语等级考试"; // 外语等级考试的全称
    public const string ENGWord = "外语等级"; //外语等级报名的查找短语

    public const string MandarinType = "普通话考试";  //普通话考试的全称
    public const string MandarinWord = "普通话"; //普通话报名的查找短语

    public const string MHKTye = "MHK报名考试";  //MHK考试的全称
    public const string MHKWord = "MHK";  //MHK报名的查找短语


    //启动报名的类型包括，未启动、已启动、已、已终止
    //其中在已启动与已之间可以进行相互转化，而已终止后，将不可再次启动
    public enum RegisterActiveState
    {
        UnStarted, //未启动
        Started, //已启动
        Ended,//已
        Aborted //已终止
    }

    public string GetChinesWord(string EngWord)
    {
        try
        {
            RegisterActiveState ras = (RegisterActiveState)Enum.Parse(typeof(RegisterActiveState), EngWord, true);
            string RetureVal = "";
            switch (ras)
            {
                case RegisterActiveState.UnStarted:
                    RetureVal = "未启动";
                    break;
                case RegisterActiveState.Started:
                    RetureVal = "已启动";
                    break;
                case RegisterActiveState.Ended:
                    RetureVal = "已结束";
                    break;
                case RegisterActiveState.Aborted:
                    RetureVal = "已终止";
                    break;
            }
            return RetureVal;
        }
        catch (Exception ex)
        {
            return "出错";
        }
    }
    
    #endregion

    Timer StartTimer;

	public BaseSetting()
	{
		//
		//TODO: 在此处添加构造函数逻辑
		//
        StartTimer = new Timer(1200000);
        StartTimer.Elapsed += new ElapsedEventHandler(StartTimer_Elapsed);
	}

    void StartTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        //throw new NotImplementedException();
    }

    /// <summary>
    /// 建立学院、专业、班级以及年级的结构
    /// </summary>
    public struct Strore_dmc
    {
       public string DeptName;  //学院名称
       public string MajorName; //专业名称
       public string NJName;  //年级
       public string ClassName;  //班级名称
    }

}
