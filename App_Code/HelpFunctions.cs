using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Web.Security;
using System.IO;
using System.Timers;
using Microsoft.Win32;
using System.Diagnostics;
using System.Data.OleDb;
using System.Data;

/// <summary>
///HelpFunctions 的摘要说明
/// </summary>
public class HelpFunctions
{
    static DataClassesDataContext db = new DataClassesDataContext();  
    #region 报名状态变化
    public enum RegisterState
    {
        正常,
        学生本人已提交,
        教秘已审核,
        初审已审核,
        终审已审核
    };
    #endregion
    #region 学生、学院基本信息
    //用于将学院名与其ID关联起来
    private static Hashtable Guid_College = new Hashtable();
    //用于将专业名称与其ID关联起来
    private static Hashtable Guid_Majors = new Hashtable();
    //用于将班级名称与其ID关联起来
    public static Hashtable Guid_Classes = new Hashtable();
    //用于将班级的GUID与学生的身份证号进行关联，填充学生表
    //private static Hashtable Guid_Class_StudentsIdentify = new Hashtable();


    enum StudentStateInfo
    {
        未确认,
        已确认,
        待修改,
    };
    #endregion

    #region 国家数据导入信息
    
    static int[] orgcode = { 620051, 620064 };
   
    enum StudentRegisterState
    {
        unRegister,
        Registered,
    };
    #endregion

    #region 其他信息  

    enum CheckedState
    {
        未审核,
        已审核,
        已否决,
    };

    #endregion
    public HelpFunctions()
	{
		//
		//TODO: 在此处添加构造函数逻辑
		//
	}

    #region 对学生、学院基本信息的操作
    /// <summary>
    /// 删除原有的信息
    /// </summary>
    private static void DelAllInfo()
    {
        string sql00 = "delete from StudentDetail";
        string sql0 = "delete from Student";
        string sql1 = "delete from Class";
        string sql2 = "delete from Major";
        string sql3 = "delete from Dept";
        db.ExecuteCommand(sql00);
        db.ExecuteCommand(sql0);
        db.ExecuteCommand(sql1);
        db.ExecuteCommand(sql2);
        db.ExecuteCommand(sql3);
        db.SubmitChanges();
    }

    /// <summary>
    /// 添加学院信息
    /// </summary>
    /// <param name="CollegeName"></param>
    public static string AddCollege(List<string> CollegeName)
    {
        //调用DelAllInfo删除原有的信息
        //DelteAllGKInfo();
        //DelAllInfo();

        Guid_College.Clear();        
        string CollegeSql = "";
        foreach (string ColName in CollegeName)
        {
            //产生学院的ID
            Guid CreatedGuid = Guid.NewGuid();
            //将学院与其GUID关联
            if (!Guid_College.ContainsKey(ColName))
                Guid_College.Add(ColName, CreatedGuid);
            //sql语句
            CollegeSql += "insert into Dept(DeptID,DeptName) values('" + CreatedGuid + "','" + ColName + "');";
        }
        try
        {
            db.ExecuteCommand(CollegeSql);
            db.SubmitChanges();
            return "以成功导入学院信息";
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "学院信息导入失败,错误信息为:" + msg;
        }
    }

    /// <summary>
    /// 添加专业信息
    /// </summary>
    /// <param name="College_Majors"></param>
    public static string AddMajors(Hashtable College_Majors)
    {
        Guid_Majors.Clear();
        string MajorSql = "";
        string CollegeID = "";
    
        foreach(string MajorName in College_Majors.Keys)
        {
            //获取学院名称
            string CollegeName = College_Majors[MajorName].ToString();
            //产生GUID
            Guid CreatedGuid = Guid.NewGuid();
            //将专业与其ID关联
            if (!Guid_Majors.ContainsKey(MajorName))
                Guid_Majors.Add(MajorName, CreatedGuid);
            if (!Guid_College.ContainsKey(CollegeName))
                CollegeID = db.Depts.First(d => d.DeptName == CollegeName).DeptID.ToString();
            else
                CollegeID = Guid_College[CollegeName].ToString();
            //插入语句
            MajorSql += "insert into Major(MajorID,DeptID,MajorName) values('" + CreatedGuid + "','" + CollegeID +
                "','" + MajorName + "');";
        }
        try
        {
            db.ExecuteCommand(MajorSql);
            db.SubmitChanges();
            return "以成功导入专业信息";
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "专业信息导入失败,错误为:" + msg;
        }
    }
    /// <summary>
    /// 添加班级信息
    /// </summary>
    /// <param name="Major_Classes"></param>
    public static string AddClasses(Hashtable Major_Classes)
    {
        Guid_Classes.Clear();
        string Sql_Class = "";
        //用于获取校区的ID
        Guid? CampusGuid = null;
        try
        {
            CampusGuid = db.CampusLocations.First(cl => cl.CampusCode == "620051").CampusLocationID;
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            return "校区不存在,请先配置校区信息";
        }
        
        
        foreach (string ClassName in Major_Classes.Keys)
        {
            //获取专业名称
            string MajorName = Major_Classes[ClassName].ToString();
            string MajorID = "";
            //产生班级的ID
            Guid CreatedGuid = Guid.NewGuid();
            //将班级与其ID关联
            if (!Guid_Classes.ContainsKey(ClassName))
                Guid_Classes.Add(ClassName, CreatedGuid);
            if (!Guid_Majors.ContainsKey(MajorName))
                MajorID = db.Majors.First(m => m.MajorName == MajorName).MajorID.ToString();
            else
                MajorID = Guid_Majors[MajorName].ToString();
            Sql_Class += "insert into Class(ClassID,MajorID,ClassName,GradeName,CampusLocationID,NCRECampusLocationID,CETCampusLocationID,MHKCampusLocationID) values('" +
                CreatedGuid + "','" + MajorID + "','" + ClassName + "','" + ClassName.Substring(0, 5) + "','" + CampusGuid + "','" + CampusGuid
                + "','" + CampusGuid + "','" + CampusGuid + "');";
        }
        try
        {
            db.ExecuteCommand(Sql_Class);
            db.SubmitChanges();
            return "信息添加成功";
        }
        catch (Exception ex) {
            string msg = ex.Message;
            return "信息添加失败,错误为:" + msg;
        }
    }

    /// <summary>
    /// 设置专业的Name和ID
    /// </summary>
    private static void SetMajorNameAndID()
    {
        var query = from m in db.Majors
                    select m;
        foreach (var item in query)
        {
            if (!Guid_Majors.ContainsKey(item.MajorName))
                Guid_Majors.Add(item.MajorName, item.MajorID);
        }
    }

    /// <summary>
    /// 填充学生信息表（必要信息表和详细信息表）
    /// </summary>
    /// <param name="NecessaryInfo">必要信息表</param>
    /// <param name="DetailInfo">详细信息表</param>
    public static void AddStudentInfo(List<string> DetailInfo)
    {
        DateTime time1 = DateTime.Now;

        #region 必要信息表

        //sql语句
        string NecessaryInfoSql = "";
        string DetailInfoSql="";
        List<string> sfzhList = new List<string>();
        List<string> xhList = new List<string>();

        foreach (string studentInfo in DetailInfo)
        {
            string[] ItemsInfo = studentInfo.Split('#');
            Guid Student_Guid = Guid.NewGuid(); //为每个学生产生一个GUID
            string xy = ItemsInfo[0];//学院
            string zy = ItemsInfo[1];//专业
            string bj = ItemsInfo[2];//班级
            Guid bj_Guid=(Guid)Guid_Classes[bj];
            string nj = ItemsInfo[3];//年级
            string mz = ItemsInfo[4];//民族
            string xb = ItemsInfo[5];//性别
            string sfzh = ItemsInfo[6];//身份证号
            string xh = ItemsInfo[7];//学号
            string csrq = ItemsInfo[8]; //出生日期
            string xm = ItemsInfo[9]; //姓名
            if (sfzh != "" && xh != "" && !sfzhList.Contains(sfzh) && !xhList.Contains(xh))
            {
                NecessaryInfoSql += "insert into Student(StudentID,StudentIDCardID,StudentName,ClassID,IsAccountExisted,IsInformationConfirmed) values('" +
                    Student_Guid + "','" + sfzh + "','" + xm + "','" + bj_Guid + "',0,'" + StudentStateInfo.未确认 + "');";
                DetailInfoSql += "insert into StudentDetail(StudentID,DeptName,MajorName,ClassName,GradeName,NationalityName,Gender,CardID,UniversityID,BirthDate,IsInfomationConfirmed,ImportedStudentInfo,StudentName) values('" +
                    Student_Guid + "','" + xy + "','" + zy + "','" + bj + "','" + nj + "','" + mz + "','" + xb + "','" + sfzh + "','" + xh + "','" + csrq + "','" + StudentStateInfo.未确认 + "','" + studentInfo + "','" + xm + "');";
            }
        }
        try
        {
            db.ExecuteCommand(NecessaryInfoSql);
            db.ExecuteCommand(DetailInfoSql);
            db.SubmitChanges();
        }
        catch (Exception ex) {
            string msg = ex.Message;                 
        }
        DateTime time2 = DateTime.Now;
        TimeSpan span = time1.Subtract(time2);
        string str = span.Minutes + "Minutes and " + span.Seconds + "seconds";

        #endregion
    }

    public static void UpdateStuInfo(DataTable dt)
    {
        int count = dt.Rows.Count;
        for (int i = 0; i < count; i++)
        {
            string stuDept = dt.Rows[i]["学院"].ToString();
            string stuMajor = dt.Rows[i]["专业名称"].ToString();
            string stuClass = dt.Rows[i]["行政班"].ToString();
            string stuGrade = dt.Rows[i]["年级"].ToString();
            string stuCard = dt.Rows[i]["身份证号"].ToString();
            UpdateInfo(stuCard, stuDept, stuMajor, stuClass, stuGrade);
        }
    }

    public static List<string> GetCardID(DataTable dt)
    {
        List<string> CardList = new List<string>();
        int count = dt.Rows.Count;
        for (int i = 0; i < count; i++)
        {
            string stuCardID = dt.Rows[i]["身份证号"].ToString();
            if (!CardList.Contains(stuCardID))
                CardList.Add(stuCardID);
        }
        return CardList;
    }

    private static void UpdateInfo(string Card, string Dept, string Major, string Class, string Grade)
    {
        string ClassID = db.Classes.First(cl => cl.ClassName == Class).ClassID.ToString();
        string strCon = "update StudentDetail set DeptName='" + Dept + "',MajorName='" + Major + "',ClassName='" + Class + "',GradeName='" + Grade + "' where CardID='" + Card + "';update Student set ClassID='" + ClassID + "' where StudentIDCardID='" + Card + "'";
        try
        {                       
            db.ExecuteCommand(strCon);
            db.SubmitChanges();                    
        }
        catch (Exception ex)
        {
            
        }

    }
    
   

    public static DataSet ConnectExcel(string filepath)
    {
        string[] Split_File = filepath.Split('\\');
        string tableName = Split_File[Split_File.Length - 1];
        //链接到excel数据源，其中HDR=YES表示第一行是列名；hdr=no表示第一行是数据行
        string strCon = "";
        if (tableName.Split('.')[1] == "xlsx")
            strCon = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filepath + ";Extended Properties='Excel 12.0;HDR=YES;IMEX=1'";
        else
            strCon = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + filepath + ";Extended Properties='Excel 8.0'";
        OleDbConnection ExcelConn = new OleDbConnection(strCon);
        try
        {
            ExcelConn.Open();
            DataTable _Table = ExcelConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null); //获取excel的所有表名
            string tb = _Table.Rows[0]["Table_Name"].ToString(); //取得第一个表名
            string strCom = string.Format("SELECT * FROM [" + tb + "]");

            OleDbDataAdapter myCommand = new OleDbDataAdapter(strCom, ExcelConn);
            DataSet ds = new DataSet();
            myCommand.Fill(ds, "[" + tableName + "$]");
            ExcelConn.Close();
            return ds;
        }
        catch
        {
            ExcelConn.Close();
            return null;
        }
    }
    /// <summary>
    /// 根据学院名称获取学院的ID
    /// </summary>
    /// <param name="deptName"></param>
    /// <returns></returns>
    public string GetDeptID1(string deptName)
    {
        try
        {
            string DeptID = db.Depts.First(d => d.DeptName == deptName).DeptID.ToString();
            return DeptID;
        }
        catch (Exception ex)
        {
            return "";
        }
    }
  
    #endregion

    #region 对考试编排信息的操作

    private static void DelteAllGKInfo()
    {
        string sql_NcreRegister = "delete from NCREREGISTER";
        string sql_NcreSubject = "delete from NCRESUBJECT";
        string sql_NcreInfo = "delete from NCREINFO";
        string sql_RegisterProcess = "delete from RegisterProcess";
        string sql_TestRegister = "delete from TestRegister";
        string sql_Test = "delete from Test";
        try
        {
            db.ExecuteCommand(sql_NcreRegister);
            db.ExecuteCommand(sql_NcreSubject);
            db.ExecuteCommand(sql_NcreInfo);
            db.ExecuteCommand(sql_RegisterProcess);
            db.ExecuteCommand(sql_TestRegister);
            db.ExecuteCommand(sql_Test);
            db.SubmitChanges();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }

    /// <summary>
    /// 填充数据表test,计算机等级考试部分
    /// </summary>
    /// <param name="TestTypeID">考试类型的ID</param>
    /// <param name="TestTypeName">考试类型的NAME</param>
    public void AddTestTypeData(string TestTypeName,string TestWord)
    {
        //DelteAllGKInfo();  //清空之前的数据
        string testTypeID = GetTestTypeID1(TestWord);
        if (testTypeID != "") return;        
        Guid TestTypeGuid = Guid.NewGuid();
        string sqlCmd = "insert into Test(TestTypeID,TestTypeName) values('" + TestTypeGuid +
            "','" + TestTypeName + "')";
        try
        {
            db.ExecuteCommand(sqlCmd);
            db.SubmitChanges();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }
    /// <summary>
    /// 返回TestRegisterID
    /// </summary>
    /// <returns></returns>
    public string GetTestRegisterID(Guid RegisterTypeID)
    {
        var query = from tr in db.TestRegisters
                               where tr.TestTypeID == RegisterTypeID && tr.State != BaseSetting.RegisterActiveState.Aborted.ToString()
                               select tr;
        string TestRegisterID = "";            
        foreach (var item in query)
        {
            if (item.State != BaseSetting.RegisterActiveState.Aborted.ToString())
            {
                TestRegisterID = item.TestRegisterID.ToString();
                break;
            }
        }
        return TestRegisterID;
    }
    /// <summary>
    /// 根据关键搜索字返回该此报名的ID
    /// </summary>
    /// <param name="KeyWord"></param>
    /// <returns></returns>
    public string GetTestRegisterID(string KeyWord)
    {
        Guid TestTypeID = Guid.Parse(GetTestTypeID1(KeyWord));
        string TestRegisterID = GetTestRegisterID(TestTypeID);
        return TestRegisterID;
    }
   
    /// <summary>
    /// 实例化的获取报名类型的当前ID
    /// </summary>
    /// <param name="KeyWord"></param>
    /// <returns></returns>
    public string GetTestTypeID1(string KeyWord)
    {
        var query = from test in db.Tests
                    where test.TestTypeName.Contains(KeyWord)
                    select test.TestTypeID;
        if (query.Count() == 0)
            return "";
        else
        {
            string NCRERegID = query.First().ToString();
            return NCRERegID;
        }
    }   

    /// <summary>
    /// 填充报名表TestRegister
    /// </summary>
    /// <param name="TypeID"></param>
    /// <param name="RegisterName"></param>
    /// <param name="StartTime"></param>
    /// <param name="EndTime"></param>
    public void AddTestRegisterData(string RegisterName, DateTime StartTime, DateTime EndTime)
    {       
        //将字符串转化为Guid
        string TestTypeID =GetTestTypeID1(BaseSetting.NCREWord);
        Guid TestRegisterID = Guid.NewGuid();
        //在插入之前，首先进行更新操作，即将之前的报名该为已终止，终止后的报名不能再次启动
         var ChangeStateQuery = from tr in db.TestRegisters
                                where tr.TestTypeID.ToString() == TestTypeID && tr.State != BaseSetting.RegisterActiveState.Aborted.ToString()
                                select tr;
         if (ChangeStateQuery.Count() != 0)
         {
             foreach (var item in ChangeStateQuery)
             {
                 Guid TestRegID = item.TestRegisterID;
                 string str = "update TestRegister set State='" + BaseSetting.RegisterActiveState.Aborted.ToString() + "' where TestRegisterID='" + TestRegID + "'";                
                 db.ExecuteCommand(str);
                 db.SubmitChanges();
                 AddRegisterProcessData(TestRegID, StartTime, EndTime, true);
             }
         }
         string sqlCmd = "insert into TestRegister(TestTypeID,TestRegisterID,RegisterName,StartDate,EndDate,State) values('" +
             TestTypeID + "','" + TestRegisterID + "','" + RegisterName + "','" + StartTime + "','" + EndTime + "','" + BaseSetting.RegisterActiveState.UnStarted.ToString() + "')";
        try
        {            
            db.ExecuteCommand(sqlCmd);
            db.SubmitChanges();
            AddRegisterProcessData(TestRegisterID, StartTime, EndTime);
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }

    }
    /// <summary>
    /// 填充报名过程表,当教务处启动一次报名时，各个学院方可进行报名工作
    /// </summary>
    /// <param name="TestRegisterID"></param>
    private void AddRegisterProcessData(Guid TestRegisterID, DateTime StartTime, DateTime EndTime, bool IsAbort = false)
    {
        string sql = "";
        var query = from dp in db.Depts
                    select dp.DeptID;
        foreach (var q in query)
        {
            if (!IsAbort)
                sql += "insert into RegisterProcess(TestRegisterID,DeptID,State) values('" +
                    TestRegisterID + "','" + q + "','" + BaseSetting.RegisterActiveState.UnStarted.ToString() + "');";
            else
            {
                sql += "Update RegisterProcess set State='" + BaseSetting.RegisterActiveState.Aborted.ToString() + "' where TestRegisterID='" +
                    TestRegisterID + "';";
                break;
            }
        }
        try
        {
            db.ExecuteCommand(sql);
            db.SubmitChanges();
            //按时间自动启动
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }

    /// <summary>
    /// 自动启动及结束报名
    /// </summary>
    private static void AutoStarting(Guid TestRegisterID, DateTime StartTime, DateTime EndTime)
    {
        DateTime GetCurTime = DateTime.Now; //获取当前时间
        string CurState = db.TestRegisters.First(tr => tr.TestRegisterID == TestRegisterID).State;
        if (GetCurTime >= StartTime && CurState == BaseSetting.RegisterActiveState.UnStarted.ToString()) //如果达到启动时间，报名还未启动的话则自动启动报名
        {
            //在此处添加一个Timer事件，时间到达后就自动启动
            Timer StartTimer = new Timer();
            //StartTimer.
        }
    }

    /// <summary>
    /// 填充NCREINFO表
    /// </summary>
    /// <param name="Testid"></param>
    /// <param name="orgcode"></param>
    /// <param name="exporttype"></param>
    /// <param name="createtime"></param>
    public void AddNCREINFOData(int Testid, int exporttype, DateTime createtime)
    {
        Guid YZ_NCRETestRegisterID = Guid.NewGuid();
        Guid BM_NCRETestRegisterID = Guid.NewGuid();
        Guid TestTypeID=Guid.Parse(GetTestTypeID1(BaseSetting.NCREWord));
        string TestRegisterID = (new HelpFunctions()).GetTestRegisterID(TestTypeID);
        string sql_NCRESql = "";
        var query_CampusLocations = from cls in db.CampusLocations
                                    select new
                                    {
                                        ID = cls.CampusLocationID,
                                        Name = cls.CampusLocationName,
                                        org = cls.CampusCode
                                    };
        foreach (var campus in query_CampusLocations)
        {
            if (campus.org == "620051")
            {
                sql_NCRESql += "insert into NCREINFO(TestRegisterID,TEST_ID,ORG_CODE,EXPORTTYPE,CREATETIME,CampusLocationID,NCRETestRegisterID) values('" +
                    TestRegisterID + "'," + Testid + ",'" + orgcode[0] + "'," + exporttype + ",'" + createtime + "','" + campus.ID + "','" + YZ_NCRETestRegisterID + "');";
            }
            else if (campus.org == "620064")
            {
                sql_NCRESql += "insert into NCREINFO(TestRegisterID,TEST_ID,ORG_CODE,EXPORTTYPE,CREATETIME,CampusLocationID,NCRETestRegisterID) values('" +
                    TestRegisterID + "'," + Testid + ",'" + orgcode[1] + "'," + exporttype + ",'" + createtime + "','" + campus.ID + "','" + BM_NCRETestRegisterID + "');";
            }
        }
        try
        {
            db.ExecuteCommand(sql_NCRESql);
            db.SubmitChanges();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }

    public void AddNCRESubject(int testid, string sbCode, string sbName, int sbGrade, int sbtime1, int sbtime2)
    {
        Guid SubjectID = Guid.NewGuid();
        string sql_Subject = "insert into NCRESUBJECT(TestRegisterID,TEST_ID,SUBJECTCODE,GRADE,TESTTIME1,TESTTIME2,SUBJECTNAME,SubjectID) values('" +
          GetTestRegisterID(BaseSetting.NCREWord) + "'," + testid + ",'" + sbCode + "'," + sbGrade + "," + sbtime1 + "," + sbtime2 + ",'" + sbName + "','" + SubjectID + "')";
        try
        {
            db.ExecuteCommand(sql_Subject);
            db.SubmitChanges();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }
    #endregion

    #region 启动报名
    

    /// <summary>
    /// 教务处启动报名
    /// </summary>
    /// <param name="RegisterID"></param>
    /// <param name="RegisterState"></param>
    /// <returns></returns>
    public static string StartRegister(string RegisterID, string RegisterState, int count = 1, bool flag = false)
    {
        Guid TestRegisterID = Guid.Parse(RegisterID);
        if (!flag)
        {
            string sql_StateRegister = "update TestRegister set State='" + BaseSetting.RegisterActiveState.Started.ToString() + "',RegisterNum=" + count + " where TestRegisterID='" +
               TestRegisterID + "'";
            try
            {
                db.ExecuteCommand(sql_StateRegister);
                db.SubmitChanges();
                College_StartedRegister(TestRegisterID);
                SetDefaultValue();
                return "成功启动!";
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return "启动失败!";
            }
        }
        else if (flag && RegisterState == BaseSetting.RegisterActiveState.Ended.ToString()) {
            string sql_SecondStart = "update TestRegister set State='" + BaseSetting.RegisterActiveState.Started.ToString() + "' where TestRegisterID='" +
                TestRegisterID + "'";
            try
            {
                db.ExecuteCommand(sql_SecondStart);
                db.SubmitChanges();
                return "二次启动成功!";
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return "启动失败";
            }
        }
        else
        {
            return "不能启动本次报名!";
        }
    }

    /// <summary>
    /// 将学生的基本状态信息重置
    /// </summary>
    private static void SetDefaultValue()
    {
        //首先要删除学生之前的报名信息
        //string NCRE_sql = "delete from NCREREGISTER";
        string Stu_sql = "update Student set IsAccountExisted=0,IsInformationConfirmed='未确认'";
        string StuDetail_sql = "update StudentDetail set IsInfomationConfirmed='未确认'";
       // db.ExecuteCommand(NCRE_sql);
        db.ExecuteCommand(Stu_sql);
        db.ExecuteCommand(StuDetail_sql);
        db.SubmitChanges();

    }

    /// <summary>
    /// 教务处启动报名后，学院自动启动报名
    /// </summary>
    /// <param name="TestRegisterID"></param>
    public static void College_StartedRegister(Guid TestRegisterID)
    {
        string sql_CollegeRegister = "Update RegisterProcess set State='" + BaseSetting.RegisterActiveState.Started.ToString() + "' where TestRegisterID='" +
            TestRegisterID + "'";
        try
        {
            db.ExecuteCommand(sql_CollegeRegister);
            db.SubmitChanges();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }

    /// <summary>
    /// 将各个学院在时间达到后自动强制结束
    /// </summary>
    /// <param name="TestRegisterID"></param>
    public static void College_ForcedEnd(Guid TestRegisterID)
    {
        string sql_CollegeEnd = "Update RegisterProcess set State='" + BaseSetting.RegisterActiveState.Ended.ToString() + "' where TestRegisterID='" +
            TestRegisterID + "' and State='" + BaseSetting.RegisterActiveState.Started.ToString() + "'";
        try
        {
            db.ExecuteCommand(sql_CollegeEnd);
            db.SubmitChanges();
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
        }
    }


    public static string PauseRegister(string RegisterID, string RegisterState)
    {
        Guid TextRegisterID=Guid.Parse(RegisterID);
        if (RegisterState ==BaseSetting.RegisterActiveState.Started.ToString())
        {
            //检查是否还有未完成报名的学院
            if (!CheckedCollegeState(TextRegisterID))
                return "还有未完成报名的学院，不能暂停报名!";
            string sql_PauseRegister = "update TestRegister set State='" + BaseSetting.RegisterActiveState.Ended.ToString() + "' where TestRegisterID='" +
                TextRegisterID + "'";
            try
            {
                db.ExecuteCommand(sql_PauseRegister);
                db.SubmitChanges();
                return "已暂停报名!";
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                return "暂停失败!";
            }
        }
        else
        {
            return "报名未启动或已经暂停!";
        }
    }
   

    /// <summary>
    /// 遍历寻找为完成报名的学院
    /// </summary>
    /// <param name="RegisterID"></param>
    /// <returns></returns>
    private static bool CheckedCollegeState(Guid RegisterID)
    {
        var result = from rp in db.RegisterProcesses
                     where rp.TestRegisterID == RegisterID
                     select rp.State;
        foreach (var item in result)
        {
            if (item.ToString() != BaseSetting.RegisterActiveState.Ended.ToString())
            {
                return false;
            }
        }
        return true;
    }


    #endregion


    #region 学生报名

   

    /// <summary>
    /// 根据用户的身份证号获取用户的ID
    /// </summary>
    /// <param name="StuCardID"></param>
    /// <returns></returns>
    public static Guid GetStudentID(string StuCardID)
    {
        Guid curStudentID = Guid.NewGuid();
        var studentID = from s in db.Students
                        where s.StudentIDCardID == StuCardID
                        select s.StudentID;
        foreach (var id in studentID)
        {
            if (curStudentID != id)
                curStudentID = id;           
        }
        return curStudentID;
    }
    #endregion

    #region 辅助
    /// <summary>
    /// 产生提示信息
    /// </summary>
    /// <param name="t"></param>
    /// <param name="p"></param>
    /// <param name="content"></param>
    public static void ClientMessageBox(Type t, System.Web.UI.Page p, string content)
    {
        p.ClientScript.RegisterClientScriptBlock(t, "yourkey", "<script>alert('" + content + "')</script>");
    }

    /// <summary>
    /// 根据班级名返回班级的ID
    /// </summary>
    /// <param name="ClassName"></param>
    /// <returns></returns>
    public static Guid GetClassID(string ClassName)
    {
        Guid ClassID = (from c in db.Classes
                        where c.ClassName == ClassName
                        select c.ClassID).First();
        return ClassID;
    }

    /// <summary>
    /// 获取校区ID与校区名的对应关系
    /// </summary>
    /// <returns></returns>
    public static Hashtable GetCampusNameAndID()
    {
        //DataClassesDataContext db = new DataClassesDataContext();
        Hashtable Campus = new Hashtable();
        var query = from cl in db.CampusLocations
                    select cl;
        foreach (var item in query)
        {
            if (!Campus.ContainsKey(item.CampusLocationID))
                Campus.Add(item.CampusLocationID.ToString(), item.CampusLocationName);
        }
        return Campus;
    }
    public Hashtable GetCampusNameAndIDByInstance()
    {
        Hashtable Campus = new Hashtable();
        var query = from cl in db.CampusLocations
                    select cl;
        foreach (var item in query)
        {
            if (!Campus.ContainsKey(item.CampusLocationID))
                Campus.Add(item.CampusLocationName.ToString(), item.CampusLocationID.ToString());
        }
        return Campus;
    }
    /// <summary>
    /// 根据校区ID获取校区名
    /// </summary>
    /// <param name="CampusID"></param>
    /// <returns></returns>
    public string CampusName(Guid CampusID)
    {
        return db.CampusLocations.First(cl => cl.CampusLocationID == CampusID).CampusLocationName;
    }
    public string CampusName(string param)
    {
        return db.CampusLocations.First(c1 => c1.CampusLocationID.ToString() == param || c1.CampusNumber == param).CampusLocationName;
    }
    public string CampusName(int KDCode)
    {
        if (KDCode == 620102)
            return "榆中校区";
        else
            return "城关校区";
    }
    #endregion

    #region 产生流水账号与准考证号    
    /// <summary>
    /// 返回0的个数
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public string createZero(string i)
    {
        if (i.Length == 1)
            return "00" + i;
        else if (i.Length == 2)
            return "0" + i;
        else
            return i;
    }
    /// <summary>
    /// 将流水号补齐
    /// </summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public string ReturnZero(int len)
    {
        string[] ZeroArr = { "0", "00", "000", "0000", "00000" };
        return ZeroArr[len - 1];
    }

   
    #endregion

    #region 将学生的报名信息导入到sqlite数据库中

   
    
   


    #endregion
}
