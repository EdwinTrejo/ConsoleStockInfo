using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Data.SqlClient;
using System.Reflection;

namespace StockInfoGui.Structures
{
    public class MetaSqlHelper<T>
    {
        private List<string> localTypeParameters;
        private string connectionString;
        private List<T> results;
        private string CompleteTypeName;

        public MetaSqlHelper(string connString, List<string> localparams, string typename)
        {
            connectionString = connString;
            localTypeParameters = localparams;
            CompleteTypeName = typename;
        }

        private void ProcessReturn(IDataReader sqlReturns)
        {
            results = new List<T>();

            if (localTypeParameters.Count < 1)
                throw new Exception("Need to Match Returns");

            while (sqlReturns.Read())
            {
                IDataRecord dataRecord = ((IDataRecord)sqlReturns);
                results.Add(ParseRow(dataRecord, CompleteTypeName));
            }
        }

        private T ParseRow(IDataRecord dataRecord, string fulltypename)
        {
            Assembly asm = typeof(T).Assembly;
            Type type = asm.GetType(fulltypename);
            T newT = (T)Activator.CreateInstance(type);

            //object newTBuilder = new object();

            if (dataRecord.FieldCount != type.GetProperties().Length)
                throw new Exception("Type Mismatch SQL return row and type different size");

            for (int i = 0; i < dataRecord.FieldCount; i++)
            {
                PropertyInfo property = type.GetProperty(localTypeParameters[i]);
                property.SetValue(newT, dataRecord[i], null);
            }
            return newT;
        }

        /// <summary>
        /// get back your type
        /// also clears results
        /// </summary>
        /// <param name="returns"></param>
        public void GetResults(out List<T> returns)
        {
            List<T> resultConvert = new List<T>();
            resultConvert.AddRange(results);
            returns = resultConvert;
            results.Clear();
        }

        /// <summary>
        /// Commit a store procedure
        /// </summary>
        /// <param name="sqlParameters"></param>
        /// <param name="storedProcedure"></param>
        public void CommitStoreProcedure(List<SqlParameter> sqlParameters, string storedProcedure)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand sqlCommand = new SqlCommand(storedProcedure, connection) { CommandType = CommandType.StoredProcedure })
                {
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();

                    sqlCommand.Connection = connection;
                    sqlCommand.Transaction = transaction;
                    sqlCommand.Parameters.AddRange(sqlParameters.ToArray());

                    try
                    {
                        IDataReader returns = sqlCommand.ExecuteReader();
                        //transaction.Commit();
                        ProcessReturn(returns);
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            transaction.Rollback();
                            connection.Close();
                        }
                        catch (Exception ex)
                        {
                            //return stack trace style
                            connection.Close();
                            throw new Exception($"{e.Message}{ex.Message}");
                        }
                        throw new Exception(e.Message);
                    }
                }
            }
        }
    }
}
