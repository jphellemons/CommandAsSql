using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CommandAsSql
{
    /// <summary>
    /// Extension method to parse a SqlCommand as string with filled SqlParameters. Makes it easy to paste the string in a Database Management tool to debug and profile etc.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Turns a parameter object to string
        /// </summary>
        /// <param name="sp">SqlParameter</param>
        /// <returns></returns>
        public static string ParameterValueForSQL(this SqlParameter sp)
        {
            string retval = string.Empty;

            switch (sp.SqlDbType)
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
                    retval = $"'{sp.Value.ToString().Replace("'", "''")}'";
                    break;

                case SqlDbType.Bit:
                    retval = (sp.Value.ToBooleanOrDefault(false)) ? "1" : "0";
                    break;
                case SqlDbType.Structured:
                    var sb = new System.Text.StringBuilder();
                    var dt = (DataTable)sp.Value;

                    sb.Append("declare ").Append(sp.ParameterName).Append(" ").AppendLine(sp.TypeName);

                    foreach (DataRow dr in dt.Rows)
                    {
                        sb.Append("insert ").Append(sp.ParameterName).Append(" values (");

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

                    retval = sb.ToString();
                    break;
                case SqlDbType.Decimal:
                case SqlDbType.Float:
                    retval = ((double)sp.Value).ToString(System.Globalization.CultureInfo.InvariantCulture).Replace("'", "''");
                    break;
                default:
                    retval = sp.Value.ToString().Replace("'", "''");
                    break;
            }

            return retval;
        }

        private static List<SqlParameter> GetStructured(this SqlParameterCollection c)
        {
            var filtered = new List<SqlParameter>();
            foreach (SqlParameter p in c)
            {
                if (p.SqlDbType == SqlDbType.Structured)
                    filtered.Add(p);
            }
            return filtered;
        }

        /// <summary>
        /// This method fills all parameters of the sqlcommand and displays it as a string which can be copy pasted in your DB management tool for debug purposes.
        /// </summary>
        /// <param name="sc">The SqlCommand you want parsed as a full SQL string</param>
        /// <returns></returns>
        public static string CommandAsSql(this SqlCommand sc)
        {
            var sql = new System.Text.StringBuilder();
            bool FirstParam = true;

            sql.Append("use ").Append(sc.Connection.Database).AppendLine(";");

            foreach (SqlParameter strucParam in sc.Parameters.GetStructured())
                sql.AppendLine(ParameterValueForSQL(strucParam));

            switch (sc.CommandType)
            {
                case CommandType.StoredProcedure:
                    sql.AppendLine("declare @return_value int;");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                        {
                            sql.Append("declare ").Append(sp.ParameterName).Append("\t").Append(sp.SqlDbType.ToString()).Append("\t= ");

                            sql.Append((sp.Direction == ParameterDirection.Output) ? "null" : sp.ParameterValueForSQL()).AppendLine(";");
                        }
                    }

                    sql.Append("exec [").Append(sc.CommandText).AppendLine("]");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if (sp.Direction != ParameterDirection.ReturnValue)
                        {
                            sql.Append((FirstParam) ? "\t" : "\t, ");

                            if (FirstParam) FirstParam = false;

                            if (sp.Direction == ParameterDirection.Input)
                            {
                                if (sp.SqlDbType != SqlDbType.Structured)
                                    sql.Append(sp.ParameterName).Append(" = ").AppendLine(sp.ParameterValueForSQL());
                                else
                                    sql.Append(sp.ParameterName).Append(" = ").AppendLine(sp.ParameterName);
                            }
                            else
                            {
                                sql.Append(sp.ParameterName).Append(" = ").Append(sp.ParameterName).AppendLine(" output");
                            }
                        }
                    }
                    sql.AppendLine(";");

                    sql.AppendLine("select 'Return Value' = convert(varchar, @return_value);");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                        {
                            sql.Append("select '").Append(sp.ParameterName).Append("' = convert(varchar, ").Append(sp.ParameterName).AppendLine(");");
                        }
                    }
                    break;
                case CommandType.Text:
                    sql.AppendLine(sc.CommandText);
                    break;
            }

            return sql.ToString();
        }

        public static bool ToBooleanOrDefault(this string s, bool Default)
        {
            return ToBooleanOrDefault((object)s, Default);
        }

        public static bool ToBooleanOrDefault(this object o, bool Default)
        {
            bool ReturnVal = Default;
            try
            {
                if (o != null)
                {
                    switch (o.ToString().ToLower())
                    {
                        case "yes":
                        case "true":
                        case "ok":
                        case "y":
                            ReturnVal = true;
                            break;
                        case "no":
                        case "false":
                        case "n":
                            ReturnVal = false;
                            break;
                        default:
                            ReturnVal = bool.Parse(o.ToString());
                            break;
                    }
                }
            }
            catch
            {
            }
            return ReturnVal;
        }
    }
}
