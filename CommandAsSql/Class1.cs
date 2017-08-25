using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace CommandAsSql
{
    static class ExtensionMethods
    {
        public static String ParameterValueForSQL(this SqlParameter sp)
        {
            String retval = "";

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
                    retval = "'" + sp.Value.ToString().Replace("'", "''") + "'";
                    break;

                case SqlDbType.Bit:
                    retval = (sp.Value.ToBooleanOrDefault(false)) ? "1" : "0";
                    break;
                case SqlDbType.Structured:
                    var sb = new System.Text.StringBuilder();
                    var dt = (DataTable)sp.Value;

                    sb.AppendLine("declare " + sp.ParameterName + " " + sp.TypeName);

                    foreach (DataRow dr in dt.Rows)
                    {
                        sb.Append("insert " + sp.ParameterName + " values (");

                        for (int colIndex = 0; colIndex < dt.Columns.Count; colIndex++)
                        {
                            switch (Type.GetTypeCode(dr[colIndex].GetType()))
                            {
                                case TypeCode.Boolean:
                                    sb.Append(Convert.ToInt32(dr[colIndex]));
                                    break;
                                case TypeCode.String:
                                    sb.Append("'");
                                    sb.Append(dr[colIndex]);
                                    sb.Append("'");
                                    break;
                                case TypeCode.DateTime:
                                    sb.Append("'");
                                    sb.Append(Convert.ToDateTime(dr[colIndex]).ToString("yyyy-MM-dd HH:mm"));
                                    sb.Append("'");
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
        public static String CommandAsSql(this SqlCommand sc)
        {
            var sql = new System.Text.StringBuilder();
            Boolean FirstParam = true;

            sql.AppendLine("use " + sc.Connection.Database + ";");

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
                            sql.Append("declare " + sp.ParameterName + "\t" + sp.SqlDbType.ToString() + "\t= ");

                            sql.AppendLine(((sp.Direction == ParameterDirection.Output) ? "null" : sp.ParameterValueForSQL()) + ";");
                        }
                    }

                    sql.AppendLine("exec [" + sc.CommandText + "]");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if (sp.Direction != ParameterDirection.ReturnValue)
                        {
                            sql.Append((FirstParam) ? "\t" : "\t, ");

                            if (FirstParam) FirstParam = false;

                            if (sp.Direction == ParameterDirection.Input)
                            {
                                if (sp.SqlDbType != SqlDbType.Structured)
                                    sql.AppendLine(sp.ParameterName + " = " + sp.ParameterValueForSQL());
                                else
                                    sql.AppendLine(sp.ParameterName + " = " + sp.ParameterName);
                            }
                            else
                                sql.AppendLine(sp.ParameterName + " = " + sp.ParameterName + " output");
                        }
                    }
                    sql.AppendLine(";");

                    sql.AppendLine("select 'Return Value' = convert(varchar, @return_value);");

                    foreach (SqlParameter sp in sc.Parameters)
                    {
                        if ((sp.Direction == ParameterDirection.InputOutput) || (sp.Direction == ParameterDirection.Output))
                        {
                            sql.AppendLine("select '" + sp.ParameterName + "' = convert(varchar, " + sp.ParameterName + ");");
                        }
                    }
                    break;
                case CommandType.Text:
                    sql.AppendLine(sc.CommandText);
                    break;
            }

            return sql.ToString();
        }

        public static Boolean ToBooleanOrDefault(this String s, Boolean Default)
        {
            return ToBooleanOrDefault((Object)s, Default);
        }

        public static Boolean ToBooleanOrDefault(this Object o, Boolean Default)
        {
            Boolean ReturnVal = Default;
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
                            ReturnVal = Boolean.Parse(o.ToString());
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
