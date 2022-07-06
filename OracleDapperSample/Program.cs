using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Reflection;

#region PlSql
//SET serveroutput on;
//CREATE OR REPLACE PROCEDURE GetEmployees
//(
//   RowCount OUT INTEGER,
//   Employees OUT SYS_REFCURSOR,
//   PageSize IN INTEGER
//)
//AS
//BEGIN
//    SELECT Count(*) INTO RowCount FROM "pcEmployees";
//OPEN Employees FOR SELECT * FROM "pcEmployees" WHERE ROWNUM <= PageSize;
//END;

//----------------------------------------------------------------

//DECLARE
//    RowCount INTEGER;
//Employees SYS_REFCURSOR;
//BEGIN
//    GetEmployees(RowCount, Employees,1);
//dbms_output.put_line('RowCount = ' || RowCount);
//END;
#endregion

#region FoodConnection
var parameters = new List<OracleParameter>();

// **********
var rowCountParam = new OracleParameter
    (parameterName: "RowCount", type: OracleDbType.Int32, direction: ParameterDirection.Output);
parameters.Add(rowCountParam);
// **********

// **********
parameters.Add(new OracleParameter
    (parameterName: "Employees", type: OracleDbType.RefCursor, direction: ParameterDirection.Output));
// **********

// **********
var pageSizeParam = new OracleParameter
    (parameterName: "PageSize", type: OracleDbType.Int32, direction: ParameterDirection.Input);
pageSizeParam.Value = 10;
parameters.Add(pageSizeParam);
// **********

var result = ConnectionManager.ExecuteAction<Employee>("GetEmployees", parameters);

var rowCount = int.Parse(rowCountParam?.Value?.ToString() ?? "0");
#endregion

#region MMSConnection
//var parameters = new List<OracleParameter>();

//var datf = new OracleParameter("datf", type: OracleDbType.Date, direction: ParameterDirection.Input);
//datf.Value = "2019-03-21";
//parameters.Add(datf);

//var datt = new OracleParameter("datt", type: OracleDbType.Date, direction: ParameterDirection.Input);
//datf.Value = "2022-03-21";
//parameters.Add(datt);

//var terminalNo = new OracleParameter("terminalNo", type: OracleDbType.Varchar2, direction: ParameterDirection.Input);
//terminalNo.Value = "62365647";
//parameters.Add(terminalNo);

//var pgNo = new OracleParameter("pgNo", type: OracleDbType.Int64, direction: ParameterDirection.Input);
//pgNo.Value = 1;
//parameters.Add(pgNo);

//var pgsize = new OracleParameter("pgsize", type: OracleDbType.Int64, direction: ParameterDirection.Input);
//pgsize.Value = 10;
//parameters.Add(pgsize);

////var totalCountParam = 
////    new OracleParameter("totalCount", type: OracleDbType.Int32, direction: ParameterDirection.Output);
////parameters.Add(totalCountParam);

////var traceNo = new OracleParameter("TraceNo", type: OracleDbType.Varchar2, direction: ParameterDirection.Input);
////traceNo.Value = null;
////parameters.Add(traceNo);

////var trnType = new OracleParameter("TrnType", type: OracleDbType.Varchar2, direction: ParameterDirection.Input);
////trnType.Value = null;
////parameters.Add(trnType);

////var refNo = new OracleParameter("RefNo", type: OracleDbType.Varchar2, direction: ParameterDirection.Input);
////refNo.Value = null;
////parameters.Add(refNo);

////var status = new OracleParameter("Status ", type: OracleDbType.Varchar2, direction: ParameterDirection.Input);
////status.Value = null;
////parameters.Add(status);

//var result = ConnectionManager.ExecuteAction<object>("eftrn.GetPagedPOSTrnByFilter", parameters);

////var rowCount = totalCountParam.Value;
#endregion

Console.ReadLine();

public static class ConnectionManager
{
    private static readonly string connString =
        "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))" +
        "(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ORCLPDB1.localdomain)));User Id=FoodUser; Password=FoodPass;";

    public static IDbConnection GetConnection()
    {
        var conn = new OracleConnection(connString);

        if (conn.State == ConnectionState.Closed)
        {
            conn.Open();
        }

        return conn;
    }

    public static void CloseConnection(IDbConnection conn)
    {
        if (conn.State == ConnectionState.Open || conn.State == ConnectionState.Broken)
        {
            conn.Close();
        }
    }

    public static IList<TResult> ExecuteAction<TResult>
        (string spName, IList<OracleParameter> parameters) where TResult : class, new()
    {
        IDbConnection connection = GetConnection();

        var command = connection.CreateCommand();

        foreach (var param in parameters)
        {
            command.Parameters.Add(param);
        }

        command.CommandText = spName;
        command.CommandType = CommandType.StoredProcedure;

        var dataReader = command.ExecuteReader();

        DataTable dt = new();
        dt.Load(dataReader);

        var result = DataTableToList.ConvertToList<TResult>(dt);

        CloseConnection(connection);

        return result;
    }
}

public class Employee
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Family { get; set; }
}

public static class DataTableToList
{
    public static List<T> ConvertToList<T>
        (this DataTable table) where T : class, new()
    {
        try
        {
            List<T> list = new List<T>();

            foreach (var row in table.AsEnumerable())
            {
                T obj = new T();

                foreach (var prop in obj.GetType().GetProperties())
                {
                    try
                    {
                        PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);
                        propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                    }
                    catch
                    {
                        continue;
                    }
                }

                list.Add(obj);
            }

            return list;
        }
        catch
        {
            return null;
        }
    }
}