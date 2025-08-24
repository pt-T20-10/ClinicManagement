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