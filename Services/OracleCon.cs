using Oracle.ManagedDataAccess.Client;

namespace SmartMeterReadingDash.Services
{
    public class OracleCon
    {
        private readonly IConfiguration _configuration;

        public OracleCon(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public OracleConnection GetConnection()
        {
            return new OracleConnection(_configuration.GetConnectionString("DefaultConnection"));
        }  
    }
}
