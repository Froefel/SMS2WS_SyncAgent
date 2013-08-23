using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Data;

namespace SMS2WS_SyncAgent
{
    public static class MyExtensionMethods
    {
        public static bool IsNullOrZero<T>(this Nullable<T> value) where T: struct
        {
            return value == null || Equals(value, 0);
        }

        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }

        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T">The type of the data stored in the record</typeparam>
        /// <param name="record">The record.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <returns></returns>
        public static T GetColumnValue<T>(this IDataRecord record, string columnName)
        {
            return GetColumnValue<T>(record, columnName, default(T));
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T">The type of the data stored in the record</typeparam>
        /// <param name="record">The record.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="defaultValue">The value to return if the column contains a <value>DBNull.Value</value> value.</param>
        /// <returns></returns>
        public static T GetColumnValue<T>(this IDataRecord record, string columnName, T defaultValue)
        {
            object value = record[columnName];
            if (value == null || value == DBNull.Value)
            {
                return defaultValue;
            }
            else
            {
                return (T)value;
            }
        }

        public static byte? GetNullableByte(this IDataRecord dr, string fieldName)
        {
            return GetNullableByte(dr, dr.GetOrdinal(fieldName));
        }

        public static byte? GetNullableByte(this IDataRecord dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? null : (byte?)dr.GetByte(ordinal);
        }

        public static int? GetNullableInt16(this IDataRecord dr, string fieldName)
        {
            return GetNullableInt16(dr, dr.GetOrdinal(fieldName));
        }

        public static int? GetNullableInt16(this IDataRecord dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? null : (int?)dr.GetInt16(ordinal);
        }

        public static int? GetNullableInt32(this IDataRecord dr, string fieldName)
        {
            return GetNullableInt32(dr, dr.GetOrdinal(fieldName));
        }

        public static int? GetNullableInt32(this IDataRecord dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? null : (int?)dr.GetInt32(ordinal);
        }

        public static decimal? GetNullableDecimal(this IDataRecord dr, string fieldName)
        {
            return GetNullableDecimal(dr, dr.GetOrdinal(fieldName));
        }

        public static decimal? GetNullableDecimal(this IDataRecord dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? null : (decimal?)Convert.ToDecimal(dr.GetValue(ordinal));
        }

        public static DateTime? GetNullableDateTime(this IDataRecord dr, string fieldName)
        {
            return GetNullableDateTime(dr, dr.GetOrdinal(fieldName));
        }

        public static DateTime? GetNullableDateTime(this IDataRecord dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? null : (DateTime?)dr.GetDateTime(ordinal);
        }

        public static string GetStringSafe(this IDataReader reader, int ordinal)
        {
            return GetStringSafe(reader, ordinal, string.Empty);
        }

        public static string GetStringSafe(this IDataReader reader, int ordinal, string defaultValue)
        {
            if (!reader.IsDBNull(ordinal))
                return reader.GetString(ordinal);
            else
                return defaultValue;
        }

        public static string GetStringSafe(this IDataReader reader, string indexName)
        {
            return GetStringSafe(reader, reader.GetOrdinal(indexName));
        }

        public static string GetStringSafe(this IDataReader reader, string indexName, string defaultValue)
        {
            return GetStringSafe(reader, reader.GetOrdinal(indexName), defaultValue);
        }

        public static string ToStringExtended(this OleDbCommand cmd)
        {
            string result = cmd.CommandText;
            
            if (cmd.Parameters.Count > 0)
            {
                foreach (OleDbParameter p in cmd.Parameters)
                {
                    result += "\n  => " + p.ParameterName + " = " + p.Value;
                }
            }

            return result;
        }
    }
}
