using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Newtonsoft.Json;
using System.Web.Security;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Configuration;

/// <summary>
///WebService 的摘要说明
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
//若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消对下行的注释。 
// [System.Web.Script.Services.ScriptService]
public class WebService : System.Web.Services.WebService {

    DataClassesDataContext db = new DataClassesDataContext();
    private static string tokenName = ConfigurationManager.AppSettings["loginName"];
    public WebService () {

        //如果使用设计的组件，请取消注释以下行 
        //InitializeComponent(); 
    }  

    /**
     * 登录功能
     * 需要输入用户名和密码，这个是由webconfig中进行配置的
     * 不是学生的用户名和密码
     * 
     * */
    [WebMethod(EnableSession = true)]
    public String Login(string username, string userPWD)
    {
        ReturnModel rm = new ReturnModel();
        Guid newGuid;

        rm.Code = 0;
        rm.Msg = "";
       // string name = ConfigurationManager.AppSettings["loginName"];
        string pwd = ConfigurationManager.AppSettings["pwd"];
        if (username == tokenName && userPWD == pwd)
        {
            rm.Msg = "登录成功！";
            rm.Code = 1;
            newGuid = Guid.NewGuid();
            var jsonData = new
            {
                token = newGuid
            };
            rm.Data = jsonData;
            Context.Session[tokenName] = newGuid;
        }
        else
        {
            rm.Msg = "该学生不存在";
            rm.Code = -1;
        }        
        return JsonConvert.SerializeObject(rm);
    }

    /***
     * 该功能用于获取系统中的报名信息
     * 如各类考试的当次考试的id，考试的名称以及各类考试的科目信息
     * 
     * */
    [WebMethod(EnableSession = true)]
    public String GetRegInfo(string token)
    {
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code",0),
                    new JProperty("Msg","请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token!=tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
        }
        else
        {            
            JArray rss =new JArray(
                            from regInfo in db.TestRegisters
                            where regInfo.State == "Started"
                            select new JObject(
                                   // new JProperty("TestTypeID", regInfo.TestTypeID),
                                    new JProperty("TestRegisterID", regInfo.TestRegisterID),
                                    new JProperty("RegisterName", regInfo.RegisterName),
                                    new JProperty("StartDate", regInfo.StartDate),
                                    new JProperty("EndDate", regInfo.EndDate),
                                    new JProperty("RegisterNum", regInfo.RegisterNum),
                                    new JProperty("MHKSubjectInfo", new JArray(
                                        from sub in db.MHKSubjects
                                        where regInfo.RegisterName.Contains("MHK") || regInfo.RegisterName.Contains("少数")
                                        select new JObject(
                                                new JProperty("SubjectName", sub.MHKSubName),
                                                new JProperty("SubjectID", sub.MHKSubID)
                                            )
                                        )),
                                    new JProperty("EngSubjectInfo", new JArray(
                                          from engSub in db.llvs
                                          where engSub.IsAllowRegister == true && regInfo.RegisterName.Contains("外语")
                                          select new JObject(
                                                 new JProperty("SubjectName", engSub.llv_Name),
                                                 new JProperty("SubjectID", engSub.llv_ID)
                                              )
                                        ))
                                )                        
                );
            returnInfo = new JObject(
                    new JProperty("Code", 1),
                    new JProperty("Msg", "获取成功"),
                    new JProperty("Data", rss)
                );          
        }
        return JsonConvert.SerializeObject(returnInfo);
    }


    /**
     * 进行普通话的报名工作
     * */
    [WebMethod(EnableSession=true)]
    public string MandarinInfo(string username,string token)
    {

        ReturnModel rm = new ReturnModel();

        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        
        try
        {
            DataClassesDataContext db = new DataClassesDataContext();
            HelpFunctions hf = new HelpFunctions();
            string CurUser = username; //获取当前用户名
            StudentDetail stuInfo = db.StudentDetails.First(sd => sd.CardID == CurUser);
            string DeptID = hf.GetDeptID1(stuInfo.DeptName);  //获取学院的Guid
            //获取普通话的Guid
            //此处需要用相关的状态值进行替换
            string TestRegisterID = hf.GetTestRegisterID(BaseSetting.MandarinWord);
            TestRegister tr = db.TestRegisters.First(trg => trg.TestRegisterID.ToString() == TestRegisterID);
         
            if (tr.State != BaseSetting.RegisterActiveState.Started.ToString()) //判断教务处是否已启动报名
            {
                rm.Code = -1;
                rm.Msg = "不在报名阶段";                
                return JsonConvert.SerializeObject(rm);
            }
            //判断学院是否已经启动了报名
            RegisterProcess rp = db.RegisterProcesses.First(rpc => rpc.DeptID.ToString() == DeptID && rpc.TestRegisterID.ToString() == TestRegisterID);
            if (rp.State != BaseSetting.RegisterActiveState.Started.ToString())
            {
                rm.Code = -1;
                rm.Msg = "学院不在报名阶段";
                return JsonConvert.SerializeObject(rm);
            }
            //判断学生的基本信息是否已被确认，只有确认的学生才可以报名
            if (stuInfo.IsInfomationConfirmed != "已确认")
            {
                rm.Code = -1;
                rm.Msg = "基本信息未确认，不能报名";
                return JsonConvert.SerializeObject(rm);
            }
            //对于普通话的报名，学生只能一次报一个
            int countRegister = (from mr in db.mandarinRegisters
                                 where mr.TestRegisterID.ToString() == TestRegisterID && mr.StudentID == stuInfo.StudentID && mr.IsRegistered == true
                                 select mr).Count();
            if (countRegister == 1)
            {
                rm.Code = -1;
                rm.Msg = "已经报名";                
                return JsonConvert.SerializeObject(rm);
            }
            string str = "insert into mandarinRegister values('" + TestRegisterID + "','" + stuInfo.StudentID + "'," + 10M + "," + 25M + "," + 10M + ",1,getdate(),0,'未撤销')";

            db.ExecuteCommand(str);
            db.SubmitChanges();

            rm.Code = 1;
            rm.Msg = "报名成功";
            return JsonConvert.SerializeObject(rm);
        }
        catch (Exception ex)
        {
            rm.Code = -1;
            rm.Msg = "报名失败";
            rm.Data = ex;
            return JsonConvert.SerializeObject(rm);            
        }
    }
    /// <summary>
    /// 学生进行MHK的考试报名
    /// </summary>
    /// <param name="SubjectId"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    [WebMethod(EnableSession = true)]
    public string SubMHKInfo(string SubjectId, string username,string zzmm,string nj,string token)
    {
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        try
        {
            HelpFunctions hf = new HelpFunctions();
            DataClassesDataContext db = new DataClassesDataContext();
            string TestRegisterID = hf.GetTestRegisterID(BaseSetting.MHKWord);
            TestRegister tr = db.TestRegisters.First(trg => trg.TestRegisterID.ToString() == TestRegisterID);

            if (tr.State != BaseSetting.RegisterActiveState.Started.ToString()) //判断教务处是否已启动报名
            {
                rm.Code = -1;
                rm.Msg = "不在报名阶段";
                return JsonConvert.SerializeObject(rm);
            }
            //获取允许报名的数量
            int RegNum = (int)tr.RegisterNum;
            if (tr.State != BaseSetting.RegisterActiveState.Started.ToString())
            {
                rm.Code = -1;
                rm.Msg = "报名还未启动或已经结束";
                return JsonConvert.SerializeObject(rm);
            }
            string CurUser = username; //获取用户名
            //获取当前用户的相关信息
            StudentDetail CurStu = db.StudentDetails.First(sd => sd.CardID == CurUser);
            //获取学生所在的学院
            string stuDept = CurStu.DeptName;
            //获取学院的GUID
            string DeptID = hf.GetDeptID1(stuDept);
            //判断学生所在的学院是否允许报名
            //DeptProcess dp = db.DeptProcesses.First(dps => dps.DeptID.ToString() == DeptID && dps.RegisterTypeID.ToString() == RegisterTypeID);
            RegisterProcess rp = db.RegisterProcesses.First(rpc => rpc.DeptID.ToString() == DeptID && rpc.TestRegisterID.ToString() == TestRegisterID);
            //if (rp.State != BaseSetting.RegisterActiveState.Started.ToString())
            //{
            //    rm.Code = -1;
            //    rm.Msg = "学院不处于报名阶段，请确认";
            //    return JsonConvert.SerializeObject(rm);
            //}
            if (CurStu.IsInfomationConfirmed != "已确认")
            {
                rm.Code = -1;
                rm.Msg = "信息还未确认，不能报名";
                return JsonConvert.SerializeObject(rm);               
            }
            //判断学生报名的科目数量是否超出了允许报名的范围
            //已经报名的数量
            int HasRegistered = (from sr in db.MHKStuRegs
                                 where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.IsApproved != "已否决"
                                 select sr).Count();
            int DenyedRegistered = (from sr in db.MHKStuRegs
                                    where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.IsApproved == "已否决"
                                    select sr).Count();
            int TheCount = HasRegistered - DenyedRegistered;
            if (TheCount >= RegNum)
            {
                rm.Code = -1;
                rm.Msg = "超出了允许报名的数量限制";
                return JsonConvert.SerializeObject(rm);
            }
            //获得学生的政治面貌
            string StuZZMM = zzmm;
            if (StuZZMM == null)
            {
                rm.Code = -1;
                rm.Msg = "政治面貌不能为空";
                return JsonConvert.SerializeObject(rm);               
            }
            //获取学生所在的年级
            string StuNJ = nj;
            if (StuNJ == null)
            {
                rm.Code = -1;
                rm.Msg = "年级不能为空";
                return JsonConvert.SerializeObject(rm);
            }
            //获得学生的报名科目
            string SubjectName = db.MHKSubjects.First(mhk => mhk.MHKSubID.ToString() == SubjectId).MHKSubName;
            if (SubjectName == null)
            {
                rm.Code = -1;
                rm.Msg = "请选择报名科目";
                return JsonConvert.SerializeObject(rm);
            }
            //获取学生报名科目的信息
            MHKSubject mhkSubj = db.MHKSubjects.First(msj => msj.MHKSubName == SubjectName);


            //查看该学生是否已经保了该科目
            int IsExistCount = (from sr in db.MHKStuRegs
                                where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.SubjID == mhkSubj.MHKSubID && sr.IsApproved != "已否决"
                                select sr).Count();
            if (IsExistCount != 0)
            {
                rm.Code = -1;
                rm.Msg = "已经报过该科目，报名失败";
                return JsonConvert.SerializeObject(rm);
            }
            //查看该学生是否已经报过其他科目
            var RegQuery = from sr in db.MHKStuRegs
                           where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.IsApproved != "已否决"
                           select sr;
            if (RegQuery.Count() > 0)
            {
                //该学生已经报过其他科目，判断是否存在冲突
                //获取已经报名过的科目
                //string RegSubjectName = RegQuery.First().MHKSubject.MHKSubName;
                foreach (var item in RegQuery)
                {
                    //进行判断
                    if (!IsAllowRegister(mhkSubj.MHKSubID.ToString(), item.MHKSubject.MHKSubName))
                    {
                        rm.Code = -1;
                        rm.Msg = "不再允许报该科目，请选择其他科目!";
                        return JsonConvert.SerializeObject(rm);
                    }
                }
            }

            //获取学生的所在校区
            string CampusName = db.View_ClassDepts.First(cd => cd.ClassName == CurStu.ClassName).CampusLocationName;
            string kdCode = "";
            if (CampusName.Contains("榆中"))
                kdCode = "620102";
            else
                kdCode = "620101";
            string IsNeedAudit = mhkSubj.IsNeedAudit ? "未审核" : "已通过";

            //查看该学生是否已经报名，如果已经报名，则将流水号和打印号插入该科目，否则不插入，
            //在付费时产生

            string str = "";
            str = "insert into MHKStuReg(TestRegisterID,StuID,SubjID,KDNum,Politicstatus,CurGrade,IsApproved) values('" +
                    TestRegisterID + "','" + CurStu.StudentID + "','" + mhkSubj.MHKSubID + "','" + kdCode + "','" + StuZZMM + "','" + StuNJ + "','" +
                    IsNeedAudit + "')";
            db.ExecuteCommand(str);
            db.SubmitChanges();

            rm.Code = 1;
            rm.Msg = "报名成功";
            return JsonConvert.SerializeObject(rm);
        }
        catch (Exception ex)
        {
            rm.Code = -1;
            rm.Msg = "报名失败";
            rm.Data = ex;
            return JsonConvert.SerializeObject(rm);
        }
    }
    /// <summary>
    /// 学生进行英语考试的报名
    /// </summary>
    /// <param name="SubjectId"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    [WebMethod(EnableSession=true)]
    public string SubEngInfo(string SubjectId, string username,string token)
    {
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();
        //报名前需判断，是否已启动报名（由教务处控制的报名和由学院控制的报名）      
        Guid TestTypeID = Guid.Parse(hf.GetTestTypeID1("外语等级"));  //获取测试类型的ID
        string TestRegisterID = hf.GetTestRegisterID(TestTypeID);  //获取本次报名的ID
        TestRegister tr = db.TestRegisters.First(trg => trg.TestRegisterID.ToString() == TestRegisterID);

        //获取允许报名的数量
        int AllowCounts = (int)tr.RegisterNum;
        if (tr.State != "Started") //判断教务处是否已启动报名
        {
            rm.Code = -1;
            rm.Msg = "不在报名阶段";
            return JsonConvert.SerializeObject(rm);
        }
        string CurUser = username; //获取用户名
        //获取当前用户的相关信息
        StudentDetail CurStu = db.StudentDetails.First(sd => sd.CardID == CurUser);
        //获取学生所在的学院
        string stuDept = CurStu.DeptName;
        //获取学院的GUID
        string DeptID = hf.GetDeptID1(stuDept);
        //判断学生所在的学院是否允许报名
        //DeptProcess dp = db.DeptProcesses.First(dps => dps.DeptID.ToString() == DeptID && dps.RegisterTypeID == RegID);
        RegisterProcess rp = db.RegisterProcesses.First(rpc => rpc.DeptID.ToString() == DeptID && rpc.TestRegisterID.ToString() == TestRegisterID);
        //if (rp.State != "Started")
        //{
        //    rm.Code = -1;
        //    rm.Msg = "学院不处于报名阶段,请确认";
        //    return JsonConvert.SerializeObject(rm);
        //}
        if (CurStu.IsInfomationConfirmed != "已确认")
        {
            rm.Code = -1;
            rm.Msg = "信息还未确认，不能报名！";
            return JsonConvert.SerializeObject(rm);
        }
        //获得学生的报名科目
        string SubjectName = db.llvs.First(sub => sub.llv_ID.ToString() == SubjectId).llv_Name;

        //判断学生报名的科目数量是否超出了允许报名的范围
        //已经报名的数量
        int HasRegistered = (from sr in db.StuRegs
                             where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID
                             select sr).Count();
        int DenyedRegistered = (from sr in db.StuRegs
                                where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.IsApproved == "已否决"
                                select sr).Count();
        int TheCount = HasRegistered - DenyedRegistered;
        if (TheCount >= AllowCounts)
        {
            rm.Code = -1;
            rm.Msg = "超出了允许的数量,报名失败!";
            return JsonConvert.SerializeObject(rm);
        }
        //获取科目的编码
        llv llSub = db.llvs.First(ll => ll.llv_Name == SubjectName);
        //科目的GUID
        Guid SubID = llSub.llv_ID;
        //科目的编码
        string SubNum = llSub.llv_Number;
        //获取学生的所在校区
        //Guid ClassID = CurStu.Student.Class.ClassID; //班级ID
        Guid ClassID = db.Classes.First(c => c.ClassName == CurStu.ClassName).ClassID;
        //查看该学生是否已经保了该科目
        int IsExistCount = (from sr in db.StuRegs
                            where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.SubID == SubID
                            select sr).Count();
        if (IsExistCount != 0)
        {
            rm.Msg = "已经保了该科目!";
            rm.Code = -1;
            return JsonConvert.SerializeObject(rm);
        }

        //查看该学生是否报过其他科目
        var RegQuery = from sr in db.StuRegs
                       where sr.TestRegisterID.ToString() == TestRegisterID && sr.StuID == CurStu.StudentID && sr.IsApproved == "已通过"
                       select sr;
        if (RegQuery.Count() > 0)
        {
            //该学生已经报过其他科目，判断是否存在冲突
            //获取已经报名过的科目
            //string RegSubjectName = RegQuery.First().llv.llv_Name;
            foreach (var item in RegQuery)
            {
                //进行判断
                if (!IsAllowRegister(SubjectName, item.llv.llv_Name))
                {
                    rm.Code = -1;
                    rm.Msg = "四级不能同时报名，六级不能同时报名";
                    return JsonConvert.SerializeObject(rm);
                }
            }
        }

        //四六级的考试校区ID
        Guid? CETKsID = db.Classes.First(c => c.ClassID == ClassID).CETCampusLocationID;
        //获取校区编号
        string CampusNum = db.CampusLocations.First(cl => cl.CampusLocationID == CETKsID).CampusNumber;
        //获取语言级别
        string LanguageLevelNum = llSub.llv_Number;
        string StuName = CurStu.StudentName;  //学生姓名
        string StuGender = CurStu.Gender;  //性别
        string StuEduLel = CurStu.cc; //学历
        string StuEduLelNum = "";
        if (StuEduLel == "本科")
            StuEduLelNum = "2";
        else
            StuEduLelNum = "1";
        string StuEduLen = CurStu.xz;  //学制
        string stuNum = CurStu.UniversityID;  //学号
        string DeptNum = db.Depts.First(d => d.DeptName == CurStu.DeptName).DeptNumber;//学院
        string MajorNum = db.Majors.First(m => m.MajorName == CurStu.MajorName).MajorNumber; //专业
        //string GradeNum = stuNum.Substring(1, 2); //年级,年纪不可通过学号查找，需要通过gradeName查找
        string GradeNum = CurStu.GradeName.Substring(2, 2); //获取年级
        string ClassNum = db.Classes.First(c => c.ClassID == ClassID).ClassNumber;  //班级
        string sfzh = CurStu.CardID; //身份证号码
        long PhotoSize = 8088; //获取照片的大小
        //string BelongCampusNum = CurStu.Student.Class.CampusLocation.CampusNumber; //所属校区
        Guid CampusLocationID = db.Classes.First(c => c.ClassID == ClassID).CampusLocationID;  //班级所属校区
        string BelongCampusNum = db.CampusLocations.First(cl => cl.CampusLocationID == CampusLocationID).CampusNumber; //获取校区编号

        try
        {
            DateTime dt = DateTime.Now;
            string str = "";
            if (llSub.IsNeedAudit)
                str = "insert into StuRegs values('" + TestRegisterID + "','" + CurStu.StudentID + "','" + SubID + "','','" + CampusNum + "','" + LanguageLevelNum + "','" + StuName + "','" + StuGender + "','" + StuEduLelNum + "','" + StuEduLen + "','" + stuNum + "','" + DeptNum + "','" + MajorNum + "','" + GradeNum + "','" + ClassNum + "','" + sfzh + "','" + BelongCampusNum + "',getdate(),getdate(),getdate(),getdate(),getdate()," + PhotoSize + ",'','已通过',0,'未撤销')";
            else
                str = "insert into StuRegs values('" + TestRegisterID + "','" + CurStu.StudentID + "','" + SubID + "','','" + CampusNum + "','" + LanguageLevelNum + "','" + StuName + "','" + StuGender + "','" + StuEduLelNum + "','" + StuEduLen + "','" + stuNum + "','" + DeptNum + "','" + MajorNum + "','" + GradeNum + "','" + ClassNum + "','" + sfzh + "','" + BelongCampusNum + "',getdate(),getdate(),getdate(),getdate(),getdate()," + PhotoSize + ",'','已通过',0,'未撤销')";
            db.ExecuteCommand(str);
            db.SubmitChanges();
            rm.Code = 1;
            rm.Msg = "提交成功";
            return JsonConvert.SerializeObject(rm);
        }
        catch (Exception ex)
        {
            rm.Code = -1;
            rm.Msg = ex.Message;
            return JsonConvert.SerializeObject(rm);
        }
    }
    /// <summary>
    /// 判断三个四级不能同时报名，三个六级不能同时报名
    /// </summary>
    /// <param name="CurSubjectName">当前正在报的科目</param>
    /// <param name="RegisteredSubjectName">已经报名过的科目</param>
    /// <returns></returns>
    private bool IsAllowRegister(string CurSubjectName, string RegisteredSubjectName)
    {
        bool flag = true;
        if (CurSubjectName.Contains("四级") && RegisteredSubjectName.Contains("四级"))
            flag = false;
        if (CurSubjectName.Contains("六级") && RegisteredSubjectName.Contains("六级"))
            flag = false;
        return flag;
    }

    /***
     * 用于获取某个学生在报名阶段中的英语报名情况
     * 其中需要传入学生的身份证号以及所需要验证的令牌信息
     * */
    [WebMethod(EnableSession = true)]
    public String GetEngRegInfo(string username,string token)
    {
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();        
        JArray engRegInfo = new JArray(
                            from ve in db.View_EngApplies
                            where ve.TestRegisterID.ToString() == hf.GetTestRegisterID(BaseSetting.ENGWord) && ve.CardID == username
                            select new JObject(                               
                                new JProperty("StudentName", ve.StudentName),
                                new JProperty("CarID", ve.CardID),
                                new JProperty("StuNum", ve.UniversityID),
                                new JProperty("ClassName", ve.ClassName),
                                new JProperty("SubjectName", ve.llv_Name),
                                new JProperty("SimpleSubjectName", ve.llv_SimpleName),
                                new JProperty("BelongCampus", db.CampusLocations.First(c1 => c1.CampusNumber == ve.BelongCampus).CampusLocationName),
                                new JProperty("KSCampus", db.CampusLocations.First(c1 => c1.CampusNumber == ve.RegisterCampus).CampusLocationName),
                                new JProperty("cost", ve.llv_cost),
                                new JProperty("AuditState", ve.IsApproved)
                             )
            );
       // rm.Data = JsonConvert.SerializeObject(engRegInfo);
        
        if (engRegInfo.Count > 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 1),
                    new JProperty("Msg", "获取成功"),
                    new JProperty("Data", engRegInfo)
                );
           // rm.Code = 1;
           // rm.Msg = "获取成功";
        }
        else
        {
            returnInfo = new JObject(
                   new JProperty("Code", -1),
                   new JProperty("Msg", "没有该学生的信息"),
                   new JProperty("Data", engRegInfo)
               );
            //rm.Code = -1;
            //rm.Msg = "没有改学生的信息";
        }
        return JsonConvert.SerializeObject(returnInfo);
    }

    /***
     * 获取学生在mhk报名阶段的报名情况信息
     * */
    [WebMethod(EnableSession = true)]
    public string GetMHKRegInfo(string username,string token)
    {
        ReturnModel rm = new ReturnModel();
        HelpFunctions hf = new HelpFunctions();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        JArray mhkRegInfo = new JArray(
                    from vmhk in db.View_MHKRegs
                    where vmhk.TestRegisterID.ToString()==hf.GetTestRegisterID(BaseSetting.MHKWord) && vmhk.CardID==username
                    select new JObject(
                            new JProperty("StudentName",vmhk.StudentName),
                            new JProperty("CardID",vmhk.CardID),
                            new JProperty("StuNum",vmhk.UniversityID),
                            new JProperty("ClassName",vmhk.ClassName),
                            new JProperty("SubjectName",vmhk.MHKSubName),
                            new JProperty("BelongCampus", vmhk.KDNum == "620102" ? "榆中校区" : "城关校区"),
                            new JProperty("State",vmhk.IsApproved)
                        )                            
                );        
        if (mhkRegInfo.Count > 0)
        {
            returnInfo = new JObject(
                        new JProperty("Code", 1),
                        new JProperty("Msg", "获取成功"),
                        new JProperty("Data", mhkRegInfo)
                    );
        }
        else
        {
            returnInfo = new JObject(
                    new JProperty("Code",-1),
                    new JProperty("Msg","没有该学生的信息"),
                    new JProperty("Data",mhkRegInfo)
                );
        }

        return JsonConvert.SerializeObject(returnInfo);               
    }

    /**
     * 获取在普通话报名阶段，学生的报名情况
     * */
    [WebMethod(EnableSession = true)]
    public string GetMandarinRegInfo(string username,string token)
    {

        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();

        JArray mandarinRegInfo = new JArray(
                from mandarin in db.View_MandarinRegInfos
                where mandarin.TestRegisterID.ToString()==hf.GetTestRegisterID(BaseSetting.MandarinWord) && mandarin.CardID==username
                select new JObject(
                        new JProperty("StudentName",mandarin.StudentName),
                        new JProperty("CardID",mandarin.CardID),
                        new JProperty("StuNum",mandarin.UniversityID),
                        new JProperty("ClassName",mandarin.ClassName),
                        new JProperty("SubjectName","普通话"),
                        new JProperty("State",mandarin.IsPayed)
                    )
            );       
        if (mandarinRegInfo.Count > 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 1),
                    new JProperty("Msg", "获取成功"),
                    new JProperty("Data", mandarinRegInfo)
                );
        }
        else
        {
            returnInfo = new JObject(
                    new JProperty("Code",-1),
                    new JProperty("Msg","没有该学生的信息"),
                    new JProperty("Data",mandarinRegInfo)
                );
        }
        return JsonConvert.SerializeObject(returnInfo);
    }

    /***
     * 用户名
     * 年份
     * 科目类型
     * */
    [WebMethod(EnableSession=true)]
    public string GetGradeOfStudentForJsj(string username, string year,string token)
    {
        //用户根据用户名和年份获取该学生在该年份的所有成绩
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();
        JArray jsjGrade = new JArray(
                from ncreGrade in db.View_NCREGrades
                where ncreGrade.CardID == username && ncreGrade.TestDate.Year.ToString() == year
                select new JObject(
                        new JProperty("StudentName",ncreGrade.StudentName),
                        new JProperty("CertificationNum",ncreGrade.CertificationNum),
                        new JProperty("grade",ncreGrade.cj),
                        new JProperty("cj",ncreGrade.cj),
                        new JProperty("subName",ncreGrade.SUBJECTNAME)
                    )                
            );        
        if (jsjGrade.Count > 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 1),
                    new JProperty("Msg", "获取成功"),
                    new JProperty("Data", jsjGrade)
                );
        }
        else
        {
            returnInfo = new JObject(
                    new JProperty("Code",-1),
                    new JProperty("Msg","没有该学生的信息"),
                    new JProperty("Data",jsjGrade)
                );
        }
        return JsonConvert.SerializeObject(returnInfo);       
    }
    [WebMethod(EnableSession=true)]
    public string GetGradeOfStuForEng(string userName, string year, string token)
    {
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();
        JArray EngGrade = new JArray(
                from engGrade in db.View_EngGrades
                where engGrade.CardID == userName && engGrade.TestDate.Year.ToString() == year
                select new JObject(
                        new JProperty("StudentName",engGrade.StudentName),
                        new JProperty("level",engGrade.LanguageLevel),
                        new JProperty("grade",engGrade.TestGrade)
                    )                
            );       
        if (EngGrade.Count > 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 1),
                    new JProperty("Msg", "获取成功"),
                    new JProperty("Data", EngGrade)
                );
        }
        else
        {
            returnInfo = new JObject(
                    new JProperty("Code",-1),
                    new JProperty("Msg","没有该学生的信息"),
                    new JProperty("Data",EngGrade)
                );
        }
        return JsonConvert.SerializeObject(returnInfo);        
    }

    [WebMethod(EnableSession = true)]
    public string GetGradeOfStuForMHK(string userName,string year, string token)
    {
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();
        JArray MHKGrade = new JArray(
                from mhkGrade in db.View_MHKGrades
                where mhkGrade.CardID ==userName && mhkGrade.TestDate.Year.ToString()==year
                select new JObject(
                        new JProperty("StudentName",mhkGrade.StudentName),
                        new JProperty("MHKName",mhkGrade.SubjectName),
                        new JProperty("grade",mhkGrade.TestGrade),
                        new JProperty("level",mhkGrade.TestLevel),
                        new JProperty("CertificationNum",mhkGrade.CertificationNum)
                    )
            );       
        if (MHKGrade.Count > 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code",1),
                    new JProperty("Msg","获取成功"),
                    new JProperty("Data",MHKGrade)
                );
        }
        else
        {
            returnInfo = new JObject(
                    new JProperty("Code",-1),
                    new JProperty("Msg","没有该学生的信息"),
                    new JProperty("Data",MHKGrade)
                );
        }
        return JsonConvert.SerializeObject(returnInfo);        
    }

    [WebMethod(EnableSession = true)]
    public string GetMaridanGrade(string userName, string year, string token)
    {
        ReturnModel rm = new ReturnModel();
        JObject returnInfo = null;
        if (Context.Session.Count == 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 0),
                    new JProperty("Msg", "请先获取令牌")
                );
            return JsonConvert.SerializeObject(returnInfo);
        }
        string tokken = Context.Session[tokenName].ToString();
        if (token != tokken)
        {
            returnInfo = new JObject(
                 new JProperty("Code", 0),
                 new JProperty("Msg", "获取数据出错"));
            return JsonConvert.SerializeObject(returnInfo);
        }
        HelpFunctions hf = new HelpFunctions();
        JArray mandarinGrade = new JArray(
                from mandarin in db.View_MandarinGrades
                where mandarin.CardID == userName && mandarin.TestDate.Year.ToString()==year
                select new JObject(
                        new JProperty("StudentName",mandarin.StudentName),
                        new JProperty("Grade",mandarin.TestGrade),
                        new JProperty("level",mandarin.TestLevel),
                        new JProperty("CertificateNumber",mandarin.CertificateNumber)
                    )
            );        
        if (mandarinGrade.Count > 0)
        {
            returnInfo = new JObject(
                    new JProperty("Code", 1),
                    new JProperty("Msg", "获取成功"),
                    new JProperty("Data", mandarinGrade)
                );
        }
        else
        {
            returnInfo = new JObject(
                    new JProperty("Code",-1),
                    new JProperty("Msg","没有该学生的信息"),
                    new JProperty("Data",mandarinGrade)
                );
        }
        return JsonConvert.SerializeObject(returnInfo);      
    }
}
