using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
///ReturnModel 的摘要说明
/// </summary>
public class ReturnModel
{
    private int code;

    public int Code
    {
        get { return code; }
        set { code = value; }
    }
    private string msg;

    public string Msg
    {
        get { return msg; }
        set { msg = value; }
    }
    private Object data;

    public Object Data
    {
        get { return data; }
        set { data = value; }
    }

    public ReturnModel()
    {
        //
        //TODO: 在此处添加构造函数逻辑
        //
    }
}