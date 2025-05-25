using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicManagement.Models
{

    /// Lớp singleton quản lý truy cập dữ liệu kết hợp Entity Framework và ADO.NET

    public class DataProvider
    {
        private static readonly object _lock = new object();
        private static DataProvider _instance;

        // Connection string được sử dụng bởi cả EF và ADO.NET
        private readonly string _connectionString = @"Data Source=acer\mssqlserver03;Initial Catalog=ClinicManagement;Integrated Security=True;Trust Server Certificate=True;MultipleActiveResultSets=True";

        // Instance của DbContext để sử dụng Entity Framework
        private ClinicDbContext _dbContext;


        /// Singleton instance access

        public static DataProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new DataProvider();
                    }
                }
                return _instance;
            }
        }


        /// Constructor private cho pattern Singleton

        private DataProvider()
        {
            // Khởi tạo DbContext
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseSqlServer(_connectionString)
                .Options;

            _dbContext = new ClinicDbContext(options);
        }


        /// Trả về DbContext instance cho EF operations

        public ClinicDbContext Context => _dbContext;


        /// Reset DbContext instance khi cần thiết

        public void ResetContext()
        {
            _dbContext.Dispose();
            var options = new DbContextOptionsBuilder<ClinicDbContext>()
                .UseSqlServer(_connectionString)
                .Options;
            _dbContext = new ClinicDbContext(options);
        }

        #region ADO.NET Methods


        /// Thực thi truy vấn và trả về DataTable

        public DataTable ExecuteQuery(string query, object[] parameters = null)
        {
            DataTable data = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(query, connection);

                if (parameters != null)
                {
                    string[] listPara = query.Split(new char[] { ' ', ',', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> paramNames = listPara.Where(p => p.StartsWith("@")).Distinct().ToList();

                    if (paramNames.Count != parameters.Length)
                    {
                        throw new ArgumentException($"Số lượng tham số không khớp! Cần: {paramNames.Count}, nhưng truyền vào: {parameters.Length}");
                    }

                    for (int i = 0; i < paramNames.Count; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], parameters[i] ?? DBNull.Value);
                    }
                }

                SqlDataAdapter adapter = new SqlDataAdapter(command);

                try
                {
                    adapter.Fill(data);
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("SQL Error: " + ex.Message);
                    throw;
                }
            }

            return data;
        }


        /// Thực thi truy vấn và trả về DataTable (async)

        public async Task<DataTable> ExecuteQueryAsync(string query, object[] parameters = null)
        {
            DataTable data = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(query, connection);

                if (parameters != null)
                {
                    string[] listPara = query.Split(new char[] { ' ', ',', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> paramNames = listPara.Where(p => p.StartsWith("@")).Distinct().ToList();

                    if (paramNames.Count != parameters.Length)
                    {
                        throw new ArgumentException($"Số lượng tham số không khớp! Cần: {paramNames.Count}, nhưng truyền vào: {parameters.Length}");
                    }

                    for (int i = 0; i < paramNames.Count; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], parameters[i] ?? DBNull.Value);
                    }
                }

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        data.Load(reader);
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("SQL Error: " + ex.Message);
                    throw;
                }
            }

            return data;
        }


        /// Thực thi truy vấn và trả về giá trị đầu tiên

        public object ExecuteScalar(string query, object[] parameters = null)
        {
            object result = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(query, connection);

                if (parameters != null)
                {
                    string[] listPara = query.Split(new char[] { ' ', ',', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> paramNames = listPara.Where(p => p.StartsWith("@")).Distinct().ToList();

                    if (paramNames.Count != parameters.Length)
                    {
                        throw new ArgumentException($"Số lượng tham số không khớp! Cần: {paramNames.Count}, nhưng truyền vào: {parameters.Length}");
                    }

                    for (int i = 0; i < paramNames.Count; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], parameters[i] ?? DBNull.Value);
                    }
                }

                result = command.ExecuteScalar();
            }

            return result;
        }


        /// Thực thi truy vấn và trả về giá trị đầu tiên (async)

        public async Task<object> ExecuteScalarAsync(string query, object[] parameters = null)
        {
            object result = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(query, connection);

                if (parameters != null)
                {
                    string[] listPara = query.Split(new char[] { ' ', ',', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> paramNames = listPara.Where(p => p.StartsWith("@")).Distinct().ToList();

                    if (paramNames.Count != parameters.Length)
                    {
                        throw new ArgumentException($"Số lượng tham số không khớp! Cần: {paramNames.Count}, nhưng truyền vào: {parameters.Length}");
                    }

                    for (int i = 0; i < paramNames.Count; i++)
                    {
                        command.Parameters.AddWithValue(paramNames[i], parameters[i] ?? DBNull.Value);
                    }
                }

                result = await command.ExecuteScalarAsync();
            }

            return result;
        }


        /// Thực thi truy vấn không trả về dữ liệu (INSERT, UPDATE, DELETE)

        public int ExecuteNonQuery(string query, object[] parameters = null)
        {
            int affectedRows = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        string[] listPara = query.Split(new char[] { ' ', ',', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
                        List<string> paramNames = listPara.Where(p => p.StartsWith("@")).Distinct().ToList();

                        if (paramNames.Count != parameters.Length)
                        {
                            throw new ArgumentException($"Số lượng tham số không khớp! Cần: {paramNames.Count}, nhưng truyền vào: {parameters.Length}");
                        }

                        for (int i = 0; i < paramNames.Count; i++)
                        {
                            command.Parameters.AddWithValue(paramNames[i], parameters[i] ?? DBNull.Value);
                        }
                    }

                    affectedRows = command.ExecuteNonQuery();
                }
            }

            return affectedRows;
        }


        /// Thực thi truy vấn không trả về dữ liệu (INSERT, UPDATE, DELETE) (async)

        public async Task<int> ExecuteNonQueryAsync(string query, object[] parameters = null)
        {
            int affectedRows = 0;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        string[] listPara = query.Split(new char[] { ' ', ',', '(', ')', '=' }, StringSplitOptions.RemoveEmptyEntries);
                        List<string> paramNames = listPara.Where(p => p.StartsWith("@")).Distinct().ToList();

                        if (paramNames.Count != parameters.Length)
                        {
                            throw new ArgumentException($"Số lượng tham số không khớp! Cần: {paramNames.Count}, nhưng truyền vào: {parameters.Length}");
                        }

                        for (int i = 0; i < paramNames.Count; i++)
                        {
                            command.Parameters.AddWithValue(paramNames[i], parameters[i] ?? DBNull.Value);
                        }
                    }

                    affectedRows = await command.ExecuteNonQueryAsync();
                }
            }

            return affectedRows;
        }


        /// Thực thi stored procedure và trả về DataTable

        public DataTable ExecuteStoredProcedure(string procedureName, SqlParameter[] parameters = null)
        {
            DataTable data = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                SqlCommand command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                SqlDataAdapter adapter = new SqlDataAdapter(command);

                try
                {
                    adapter.Fill(data);
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("SQL Error: " + ex.Message);
                    throw;
                }
            }

            return data;
        }


        /// Thực thi stored procedure và trả về DataTable (async)

        public async Task<DataTable> ExecuteStoredProcedureAsync(string procedureName, SqlParameter[] parameters = null)
        {
            DataTable data = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                try
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        data.Load(reader);
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("SQL Error: " + ex.Message);
                    throw;
                }
            }

            return data;
        }

        #endregion

        #region Entity Framework Helper Methods


        /// Lưu thay đổi trong DbContext

        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }


        /// Lưu thay đổi trong DbContext (async)

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }


        /// Chuyển đổi DataTable thành List<T>

        public List<T> ConvertDataTableToList<T>(DataTable dt) where T : class, new()
        {
            List<T> list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T obj = new T();
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (dt.Columns.Contains(prop.Name))
                    {
                        var value = row[prop.Name];
                        if (value != DBNull.Value)
                        {
                            prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                }
                list.Add(obj);
            }
            return list;
        }

        #endregion
    }
}