using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace CommandAsSql
{
    /// <summary>
    /// Extension method to parse a SqlCommand as string with filled SqlParameters. Makes it easy to paste the string in a Database Management tool to debug and profile etc.
    /// </summary>
    public static class ExtensionMethods
    {

        #region Boolean Helpers

        public static Boolean ToBooleanOrDefault(this string s, bool defaultValue)
        {
            return ToBooleanOrDefault((object)s, defaultValue);
        }

        public static Boolean ToBooleanOrDefault(this object o, bool defaultValue)
        {
            bool result = defaultValue;

            if (o != null)
                try
                {
                    switch (o.ToString().ToLower())
                    {
                        case "yes":
                        case "true":
                        case "ok":
                        case "y":
                            result = true;
                            break;

                        case "no":
                        case "false":
                        case "n":
                            result = false;
                            break;

                        default:
                            result = bool.Parse(o.ToString());
                            break;
                    }
                }
                catch
                {
                }

            return result;
        }

        #endregion

        #region SQL Helpers

        /// <summary>
        /// Turns a parameter object to string
        /// </summary>
        /// <param name="param">SqlParameter</param>
        /// <returns></returns>
        public static String ParameterValueForSQL(this SqlParameter param)
        {
            object paramValue = param.Value; //assuming param isn't null

            if (paramValue == null) //TODO: should probably use DBNull.Value instead or in combination with this
                return "NULL"; //TODO: naive code, won't work as is, need to replace later on = NULL with IS NULL at non-Update queries

            switch (param.SqlDbType)
            {
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.Time:
                case SqlDbType.VarChar:
                case SqlDbType.Xml:
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTimeOffset:
                    //return "'" + paramValue.ToString().Replace("'", "''") + "'";
                    return $"'{paramValue.ToString().Replace("'", "''")}'"; //C# 6 syntax

                case SqlDbType.Bit:
                    return (paramValue.ToBooleanOrDefault(false)) ? "1" : "0";

                case SqlDbType.Structured:
                    var sb = new System.Text.StringBuilder();
                    var dt = (DataTable)paramValue;

                    sb.Append("declare ").Append(param.ParameterName).Append(" ").AppendLine(param.TypeName);

                    foreach (DataRow dr in dt.Rows)
                    {                        
                        sb.Append("insert ").Append(param.ParameterName).Append(" values (");

                        for (int colIndex = 0; colIndex < dt.Columns.Count; colIndex++)
                        {
                            switch (Type.GetTypeCode(dr[colIndex].GetType()))
                            {
                                case TypeCode.Boolean:
                                    sb.Append(Convert.ToInt32(dr[colIndex]));
                                    break;

                                case TypeCode.String:
                                    sb.Append("'").Append(dr[colIndex]).Append("'");
                                    break;

                                case TypeCode.DateTime:
                                    sb.Append("'").Append(Convert.ToDateTime(dr[colIndex]).ToString("yyyy-MM-dd HH:mm")).Append("'");
                                    break;

                                default:
                                    sb.Append(dr[colIndex]); break;
                            }

                            sb.Append(", ");
                        }

                        sb.Length -= 2; // trailing ', '
                        sb.AppendLine(")");
                    }

                    return sb.ToString();

                case SqlDbType.Decimal:
                case SqlDbType.Float:
                    return ((double)paramValue).ToString(System.Globalization.CultureInfo.InvariantCulture).Replace("'", "''");

                default:
                    return paramValue.ToString().Replace("'", "''");
            }
        }

        private static List<SqlParameter> GetStructured(this SqlParameterCollection paramCollection)
        {
            List<SqlParameter> filtered = new List<SqlParameter>();
            foreach (SqlParameter p in paramCollection)
                if (p.SqlDbType == SqlDbType.Structured)
                    filtered.Add(p);

            return filtered;
        }

        #endregion

        /// <summary>
        /// This method fills all parameters of the sqlcommand and displays it as a string which can be copy pasted in your DB management tool for debug purposes.
        /// </summary>
        /// <param name="command">The SqlCommand you want parsed as a full SQL string</param>
        /// <returns></returns>
        public static string CommandAsSql(this SqlCommand command)
        {
            var sql = new System.Text.StringBuilder();

            sql.Append("use ").Append(command.Connection.Database).AppendLine(";");

            foreach (SqlParameter strucParam in command.Parameters.GetStructured())
                sql.AppendLine(strucParam.ParameterValueForSQL());

            switch (command.CommandType)
            {
                case CommandType.Text: //checking 1st, since if we use Text SQL Commands we'll probably be logging more of them that if we had them grouped in Stored Procedures
                    command.CommandAsSql_Text(sql);
                    break;

                case CommandType.StoredProcedure:
                    command.CommandAsSql_StoredProcedure(sql);
                    break;
            }

            return sql.ToString();
        }

        private static void CommandAsSql_Text(this SqlCommand command, System.Text.StringBuilder sql)
        {
            string query = command.CommandText;

            foreach (SqlParameter p in command.Parameters)
                query = Regex.Replace(query, "\\B" + p.ParameterName + "\\b", p.ParameterValueForSQL()); //the first one is \B, the 2nd one is \b, since ParameterName starts with @ which is a non-word character in RegEx (see https://stackoverflow.com/a/2544661)

            sql.AppendLine(query);
        }

        private static void CommandAsSql_StoredProcedure(this SqlCommand command, System.Text.StringBuilder sql)
        {
            sql.AppendLine("declare @return_value int;");

            foreach (SqlParameter sp in command.Parameters)
            {
                if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                {
                    sql.Append("declare ").Append(sp.ParameterName).Append("\t").Append(sp.SqlDbType.ToString()).Append("\t= ");

                    sql.Append((sp.Direction == ParameterDirection.Output) ? "null" : sp.ParameterValueForSQL()).AppendLine(";");
                }
            }

            sql.Append("exec [").Append(command.CommandText).AppendLine("]");

            bool FirstParam = true;
            foreach (SqlParameter param in command.Parameters)
            {
                if (param.Direction != ParameterDirection.ReturnValue)
                {
                    sql.Append((FirstParam) ? "\t" : "\t, ");

                    if (FirstParam)
                        FirstParam = false;

                    if (param.Direction == ParameterDirection.Input)
                        if (param.SqlDbType != SqlDbType.Structured)
                            sql.Append(param.ParameterName).Append(" = ").AppendLine(param.ParameterValueForSQL());
                        else
                            sql.Append(param.ParameterName).Append(" = ").AppendLine(param.ParameterName);
                    else
                        sql.Append(param.ParameterName).Append(" = ").Append(param.ParameterName).AppendLine(" output");
                }
            }
            sql.AppendLine(";");

            sql.AppendLine("select 'Return Value' = convert(varchar, @return_value);");

            foreach (SqlParameter sp in command.Parameters)
            {
                if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                    sql.Append("select '").Append(sp.ParameterName).Append("' = convert(varchar, ").Append(sp.ParameterName).AppendLine(");");
            }
        }

    }
}
