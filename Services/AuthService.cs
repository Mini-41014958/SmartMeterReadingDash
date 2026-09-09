using Oracle.ManagedDataAccess.Client;
using SmartMeterReadingDash.Models.Dashboard;
using System.Data;

namespace SmartMeterReadingDash.Services
{
    public class AuthRepository
    {
        private readonly IConfiguration _configuration;

        public AuthRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            return _configuration
                .GetConnectionString("AuthConnection")
                ?? throw new InvalidOperationException(
                    "AuthConnection is not configured."
                );
        }

        public async Task<bool> TestConnectionAsync()
        {
            await using var connection = new OracleConnection(GetConnectionString());

            await connection.OpenAsync();

            return connection.State == ConnectionState.Open;
        }

        public async Task<DashboardUser?> GetUserByUsernameAsync( string username)
        {
            const string sql = @"
                    SELECT
                        USER_ID,
                        USERNAME,
                        PASSWORD,
                        FULL_NAME,
                        ROLE,
                        IS_ACTIVE,
                        CREATED_DATE,
                        LAST_LOGIN_DATE,
                        COMPANY,
                        DEPARTMENT
                    FROM SMART_METER_DASHBOARD_USERS
                    WHERE UPPER(USERNAME) = UPPER(:USERNAME) ";

            await using var connection = new OracleConnection(GetConnectionString());

            await connection.OpenAsync();

            await using var command = new OracleCommand(sql, connection);

            command.Parameters.Add( "USERNAME", OracleDbType.Varchar2 ).Value = username.Trim();

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new DashboardUser
            {
                UserId = Convert.ToInt32( reader["USER_ID"] ),
                Username =  reader["USERNAME"]?.ToString() ?? string.Empty,

                Password = reader["PASSWORD"]?.ToString() ?? string.Empty,

                FullName = reader["FULL_NAME"] == DBNull.Value
                        ? null
                        : reader["FULL_NAME"].ToString(),

                Role =  reader["ROLE"]?.ToString() ?? "USER", 

                IsActive = Convert.ToInt32(reader["IS_ACTIVE"]),

                CreatedDate = Convert.ToDateTime(reader["CREATED_DATE"]),

                LastLoginDate =  reader["LAST_LOGIN_DATE"] == DBNull.Value ? null
                        : Convert.ToDateTime(reader["LAST_LOGIN_DATE"] ),

                Company = reader["COMPANY"] == DBNull.Value ? null
                        : reader["COMPANY"].ToString(),

                Department = reader["DEPARTMENT"] == DBNull.Value ? null
                        : reader["DEPARTMENT"].ToString()
            };
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            const string sql = @"
                UPDATE SMART_METER_DASHBOARD_USERS
                SET LAST_LOGIN_DATE = SYSDATE
                WHERE USER_ID = :USER_ID
            ";

            await using var connection = new OracleConnection(GetConnectionString());

            await connection.OpenAsync();

            await using var command = new OracleCommand(sql, connection);

            command.Parameters.Add(
                "USER_ID",
                OracleDbType.Int32
            ).Value = userId;

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> UsernameExistsAsync(
            string username)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM SMART_METER_DASHBOARD_USERS
                WHERE UPPER(USERNAME) = UPPER(:USERNAME)
            ";

            await using var connection =  new OracleConnection(GetConnectionString());

            await connection.OpenAsync();

            await using var command = new OracleCommand(sql, connection);

            command.Parameters.Add(
                "USERNAME",
                OracleDbType.Varchar2
            ).Value = username.Trim();

            var count =
                Convert.ToInt32(
                    await command.ExecuteScalarAsync()
                );

            return count > 0;
        }

        public async Task<int> GetUserCountAsync()
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM SMART_METER_DASHBOARD_USERS
            ";

            await using var connection =
                new OracleConnection(GetConnectionString());

            await connection.OpenAsync();

            await using var command =
                new OracleCommand(sql, connection);

            var result =
                await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        public async Task<int> CreateUserAsync( DashboardUser user)
        {
            const string sql = @"
                INSERT INTO SMART_METER_DASHBOARD_USERS
                (
                    USERNAME,
                    PASSWORD,
                    FULL_NAME,
                    ROLE,
                    IS_ACTIVE,
                    CREATED_DATE,
                    COMPANY,
                    DEPARTMENT
                )
                VALUES
                (
                    :USERNAME,
                    :PASSWORD,
                    :FULL_NAME,
                    :ROLE,
                    :IS_ACTIVE,
                    SYSDATE,
                    :COMPANY,
                    :DEPARTMENT
                )
                RETURNING USER_ID INTO :USER_ID
            ";

            await using var connection =
                new OracleConnection(GetConnectionString());

            await connection.OpenAsync();

            await using var command =
                new OracleCommand(sql, connection);

            command.Parameters.Add(
                "USERNAME",
                OracleDbType.Varchar2
            ).Value = user.Username.Trim();

            command.Parameters.Add(
                "PASSWORD",
                OracleDbType.Varchar2
            ).Value = user.Password;

            command.Parameters.Add(
                "FULL_NAME",
                OracleDbType.Varchar2
            ).Value =
                (object?)user.FullName
                ?? DBNull.Value;

            command.Parameters.Add(
                "ROLE",
                OracleDbType.Varchar2
            ).Value =
                user.Role.Trim().ToUpperInvariant();

            command.Parameters.Add(
                "IS_ACTIVE",
                OracleDbType.Int32
            ).Value = user.IsActive;

            command.Parameters.Add(
                "COMPANY",
                OracleDbType.Varchar2
            ).Value =
                string.IsNullOrWhiteSpace(user.Company)
                    ? DBNull.Value
                    : user.Company.Trim().ToUpperInvariant();

            command.Parameters.Add(
                "DEPARTMENT",
                OracleDbType.Varchar2
            ).Value =
                string.IsNullOrWhiteSpace(user.Department)
                    ? DBNull.Value
                    : user.Department.Trim().ToUpperInvariant();

            var userIdParameter =
                new OracleParameter(
                    "USER_ID",
                    OracleDbType.Int32)
                {
                    Direction = ParameterDirection.Output
                };

            command.Parameters.Add(userIdParameter);

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32( userIdParameter.Value.ToString()
            );
        }
    }
}
