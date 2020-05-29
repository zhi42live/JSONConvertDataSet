using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data;
using System.IO;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Text;
using System.Xml;
using System.Diagnostics;

/// <summary>
/// Gavin 开发， 用于数据转换类集 update 2020/4/7
/// </summary>
namespace DataConvert
{
    public class KeyValue
    {
        public String key { get; set; }
        public String value { get; set; }
        public String typeString { get; set; }
    }
    public static class DataJson
    {
        #region DataTable 转换为Json 字符串  
        /// <summary>  
        /// DataTable 对象 转换为Json 字符串  
        /// </summary>  
        /// <param name="dt"></param>  
        /// <returns></returns>  
        public static string ToJson(this DataTable dt)
        {
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = Int32.MaxValue; //取得最大数值  
            ArrayList arrayList = new ArrayList();
            foreach (DataRow dataRow in dt.Rows)
            {
                Dictionary<string, object> dictionary = new Dictionary<string, object>();  //实例化一个参数集合  
                foreach (DataColumn dataColumn in dt.Columns)
                {
                    dictionary.Add(dataColumn.ColumnName, dataRow[dataColumn.ColumnName].ToString().Trim());
                }
                arrayList.Add(dictionary); //ArrayList集合中添加键值  
            }
            return javaScriptSerializer.Serialize(arrayList);  //返回一个json字符串  
        }
        #endregion

        #region Json 字符串 转换为 DataTable数据集合  
        /// <summary>  
        /// Json 字符串 转换为 DataTable数据集合  
        /// </summary>  
        /// <param name="json"></param>  
        /// <returns></returns>  
        public static DataTable ToDataTable(this string json)
        {
            DataTable dataTable = new DataTable();  //实例化  
            DataTable result;
            try
            {
                JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
                javaScriptSerializer.MaxJsonLength = Int32.MaxValue; //取得最大数值  
                ArrayList arrayList = javaScriptSerializer.Deserialize<ArrayList>(json);
                if (arrayList.Count > 0)
                {
                    foreach (Dictionary<string, object> dictionary in arrayList)
                    {
                        if (dictionary.Keys.Count<string>() == 0)
                        {
                            result = dataTable;
                            return result;
                        }
                        if (dataTable.Columns.Count == 0)
                        {
                            foreach (string current in dictionary.Keys)
                            {
                                dataTable.Columns.Add(current, dictionary[current].GetType());
                            }
                        }
                        DataRow dataRow = dataTable.NewRow();
                        foreach (string current in dictionary.Keys)
                        {
                            dataRow[current] = dictionary[current];
                        }

                        dataTable.Rows.Add(dataRow); //循环添加行到DataTable中  
                    }
                }
            }
            catch (Exception exception1)
            {
                throw exception1;
            }
            result = dataTable;
            return result;
        }
        #endregion


        /// <summary>
        /// 在DataSet 查找 指定表名的表，如果有返回序，如果没有返回-1
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int findDataSetTable(DataSet ds , String name)
        {
            int tableIndex = 0;
            foreach(DataTable dt in ds.Tables)
            {
                if (dt.TableName.Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return tableIndex;
                }
                tableIndex++;
            }
            return -1;
        }

        /// <summary>
        /// DataSet 转 JSON  第0个表是主表，其它是子表
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        public static string DataSetToJson(DataSet ds,int tableIndex,String tableNamePath,String qc="", DataColumn[] pdc=null)
        {
            //\r\n
            if (ds.Tables.Count == 0) return "";
            if(tableIndex<0 || tableIndex>ds.Tables.Count-1) return "";

            DataTable hdt = ds.Tables[tableIndex];

            #region 筛选子表数据
            if (qc!="" && pdc != null)
            {
                DataTable dt2 = hdt.Clone();
                DataRow[] rows = hdt.Select(qc);
                if (rows != null && rows.Length > 0)
                {
                    foreach (DataRow row in rows)
                    {
                        dt2.ImportRow(row);
                    }
                }
                hdt = dt2.Copy();
                dt2.Dispose();
            }
            #endregion

            System.Text.StringBuilder jsonBuilder = new System.Text.StringBuilder();
            if (hdt.Rows.Count > 1 || (hdt.Rows.Count>0 && qc !="")) jsonBuilder.Append("[");
            bool fc,rfc;
            String sv;
            DateTime dsv;
            int ti;
            rfc = false;
            foreach(DataRow dr in hdt.Rows)
            {
                if(rfc) jsonBuilder.Append(",");
                rfc = true;
                fc = false;
                #region 转换每行数据
                jsonBuilder.Append("{");
                foreach (DataColumn dc in hdt.Columns)
                {
                    #region 检查是否父表key字段 如果是跳过此字段
                    if (pdc != null)
                    {
                        bool keyYesNo = false;
                        foreach (DataColumn kdc in pdc)
                        {
                            if (kdc.ColumnName.Trim().Equals(dc.ColumnName.Trim()))
                            {
                                keyYesNo = true;
                                break;
                            }
                        }
                        if (keyYesNo) continue;
                    }
                    #endregion
                    if (fc) { jsonBuilder.Append(",");  }
                    fc = true;
                    jsonBuilder.Append("\"");
                    jsonBuilder.Append(dc.ColumnName.Trim());
                    jsonBuilder.Append("\":");

                    //如果有递归子表
                    ti = DataJson.findDataSetTable(ds, tableNamePath.Trim() + "_" + dc.ColumnName.Trim());
                    if (ti> 0)
                    {
                        //PrimaryKey 主键 值 
                        String qcStr = "";
                        foreach (DataColumn qcol in hdt.PrimaryKey)
                        {
                            if (qcStr != "") qcStr += " and ";
                            qcStr += qcol.ColumnName.Trim() + "=";
                            if (!dc.DataType.IsValueType) qcStr += "'";
                            qcStr += dr[qcol].ToString().Trim();
                            if (!dc.DataType.IsValueType) qcStr += "'";
                        }
                        jsonBuilder.Append(DataSetToJson(ds, ti, tableNamePath.Trim() + "_" + dc.ColumnName.Trim(),
                            qcStr,hdt.PrimaryKey));
                        continue;
                    }

                    if (!dc.DataType.IsValueType)
                    {
                        jsonBuilder.Append("\"");
                    }
                    sv = dr[dc].ToString().Trim();
                    if (dc.DataType.Name.Equals("DateTime"))
                    {
                        if (DateTime.TryParse(sv, out dsv))
                        {
                            sv = "\""+ dsv.ToString("yyyy-MM-dd HH:mm:ss") + "\"";
                        }
                        else { sv = ""; }
                    }
                    jsonBuilder.Append(sv);

                    if (!dc.DataType.IsValueType)
                    {
                        jsonBuilder.Append("\"");
                    }
                }
                jsonBuilder.Append("}");
                #endregion
            }
            
            if (hdt.Rows.Count > 1 || (hdt.Rows.Count > 0 && qc != "")) jsonBuilder.Append("]");
            return jsonBuilder.ToString();
        }

        /// <summary>
        /// json 转 DataSet
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static DataSet jsonToDataSet(String jsonString)
        {
            //try
            //{
                DataSet ds = new DataSet();
                JsonReader reader = new JsonTextReader(new StringReader(jsonString));
                String tableName = "";
                if (reader != null) { tableName = getJsonTableName(reader.Path); }
                jsonToDataTable(reader, "", ref ds);

                return ds;
            //}
            //catch(Exception e)
            //{
            //    Trace.WriteLine("jsonToDataSet error:" + e.Message);
            //    return null;
            //}            
        }

        /// <summary>
        /// 获取json 对象或数组 到 dataTable 的表名
        /// </summary>
        /// <param name="jsonPath"></param>
        /// <returns></returns>
        public static String getJsonTableName(String jsonPath , bool isParent=false)
        {
            string[] sv = jsonPath.Split('.');
            String tn = "";
            if (sv.Length > 1)
            {
                for (int i = 0; i < sv.Length ; i++)
                {
                    if (!String.IsNullOrEmpty(tn)) { tn += "."; }
                    tn += sv[i].Split('[')[0];
                }
            }
            else
            {
                if (jsonPath.Split('[').Length > 1)
                {
                    tn = jsonPath.Split('[')[0].Trim();
                }
                else
                {
                    if (jsonPath.Trim() != "") { tn = jsonPath.Trim(); } else { tn = "_data_"; }
                }
            }
            if(isParent)
            {
                if (tn.Split('.').Length > 1)
                    tn = tn.Split('.')[tn.Split('.').Length - 2];
                else tn = "_data_";
            }
            return tn;
        }

        /// <summary>
        ///  通过递归，把数据对象存到对表格后添加到 DataSet，并建立关系主健     
        /// </summary>        
        /// <returns></returns>
        private static void jsonToDataTable(JsonReader reader, String uuid, ref DataSet ds)
        {
            String tokenType = "";            
            List<KeyValue> kvlist = new List<KeyValue>();
            KeyValue fv = new KeyValue();

            String tableName="";

            while (reader.Read())
            {
                tokenType = reader.TokenType.ToString().Trim();
                Trace.WriteLine(reader.TokenType + "\t\t" + reader.ValueType + "\t\t"
                    + reader.Value + "\t\t" + reader.Path + "\t\t");

                //进入递归
                if (tokenType.Equals("StartObject", StringComparison.OrdinalIgnoreCase) ||
                    tokenType.Equals("StartArray", StringComparison.OrdinalIgnoreCase))
                {
                              
                    if(fv.key != null)
                    {
                        KeyValue kv;
                        if (!string.IsNullOrEmpty(uuid))
                        {
                            kv = new KeyValue();
                            kv.key = "data_uuid";
                            kv.typeString = "String";
                            kv.value = uuid;
                            kvlist.Add(kv);
                        }

                        uuid = System.Guid.NewGuid().ToString("N");
                        kv = new KeyValue();
                        kv.key = fv.key;
                        kv.typeString = "String";
                        kv.value = uuid;
                        kvlist.Add(kv);
                    }                    
                    jsonToDataTable(reader,uuid,ref ds);
                    continue;
                }

                //提交行
                if (tokenType.Equals("EndArray", StringComparison.OrdinalIgnoreCase) ||
                    tokenType.Equals("EndObject", StringComparison.OrdinalIgnoreCase)) //row end
                {
                    if (kvlist.Count > 0)
                    {
                        tableName = getJsonTableName(reader.Path);                      
                        addTableRow(kvlist, ref ds, tableName, uuid);
                    }
                    return;
                }

                //添加字段
                if (tokenType.Equals("PropertyName", StringComparison.OrdinalIgnoreCase)) //Get fileds name and value
                {
                    fv = new KeyValue();
                    fv.key = reader.Value.ToString().Trim();
                    fv.typeString = "";
                    fv.value = "";
                }
                else//添加值 
                {
                    String fn = "";
                    if (reader.Path.Split('.').Length - 1 >= 0)
                    {
                        fn = reader.Path.Split('.')[reader.Path.Split('.').Length - 1];
                        fn = fn.Split('[')[0];
                    }
                    if(fv.key == null) //处理没有字段名的数组
                    {
                        fn=fn + kvlist.Count.ToString();
                        fv.key = fn;
                        fv.typeString = "";
                        fv.value = "";
                    }
                    if (fv.key.Trim().Equals(fn.Trim()))
                    {
                        fv.typeString = tokenType;
                        if (fv.value.Trim() != "")
                        {
                            fv.value += ";";
                            fv.value += reader.Value.ToString().Trim();
                            kvlist[kvlist.Count - 1] = fv;
                        }
                        else
                        {
                            if (reader.Value == null) fv.value = ""; else fv.value = reader.Value.ToString().Trim();
                            kvlist.Add(fv);
                            fv = new KeyValue();
                        }
                    }                    
                }
            }

            //检查 kvlist 是否有未提交行的
            if (kvlist.Count > 0)
            {
                tableName = getJsonTableName(reader.Path);
                addTableRow(kvlist, ref ds, tableName, uuid);                  
            }            
        }

        /// <summary>
        /// 提交数据行
        /// </summary>
        /// <param name="kvlist"></param>
        /// <param name="ds"></param>
        /// <param name="tableName"></param>
        private static void addTableRow(List<KeyValue> kvlist, ref DataSet ds, String tableName, String uuid)
        {
            if (tableName == "") return;
            if (kvlist.Count > 0)
            {
                #region 检查数据表是否存在，不存在进行创建                        
                int tsn = -1;
                for (int i = 0; i < ds.Tables.Count; i++)
                {
                    if (ds.Tables[i].TableName.Trim().Equals(tableName.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        tsn = i;
                        break;
                    }
                }
                if (tsn < 0)
                {
                    ds.Tables.Add(tableName);
                    ds.Tables[tableName].Columns.Add("data_uuid", typeof(String));
                }

                #region 添加表字段
                bool fok;
                foreach (KeyValue row in kvlist)
                {
                    fok = false;
                    foreach (DataColumn dc in ds.Tables[tableName].Columns)
                    {
                        if (row.key.Trim().Equals(dc.ColumnName.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            fok = true;
                            break;
                        }
                    }
                    if (!fok) //如果缺少Column，添加 Int64
                    {
                        if (row.typeString.Substring(0, 3).Equals("Int", StringComparison.OrdinalIgnoreCase))
                        {
                            ds.Tables[tableName].Columns.Add(row.key, typeof(int));
                        }
                        else if (row.typeString.Equals("Boolean", StringComparison.OrdinalIgnoreCase))
                        {
                            ds.Tables[tableName].Columns.Add(row.key, typeof(bool));
                        }
                        else if (row.typeString.Equals("Date", StringComparison.OrdinalIgnoreCase))
                        {
                            ds.Tables[tableName].Columns.Add(row.key, typeof(DateTime));
                        }
                        else if (row.typeString.Equals("String", StringComparison.OrdinalIgnoreCase))
                        {
                            ds.Tables[tableName].Columns.Add(row.key, typeof(String));
                        }
                        else
                        {
                            ds.Tables[tableName].Columns.Add(row.key, typeof(String));
                        }
                    }
                }
                #endregion
                #endregion

                #region 添加字段数据
                DataRow dr;
                dr = ds.Tables[tableName].NewRow();
                dr["data_uuid"] = uuid;
                foreach (KeyValue row in kvlist)
                {
                    if (row.typeString.Equals("Null", StringComparison.OrdinalIgnoreCase))
                    {
                        dr[row.key] = "";
                    }
                    else { dr[row.key] = row.value; }
                }
                if (tableName.Equals("_data_")) dr["data_uuid"] = System.Guid.NewGuid().ToString("N"); 
                ds.Tables[tableName].Rows.Add(dr);
                kvlist.Clear();
                #endregion
            }
        }

    }
}