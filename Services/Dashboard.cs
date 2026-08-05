using Microsoft.AspNetCore.Identity;
using Oracle.ManagedDataAccess.Client;
using SmartMeterReadingDash.Models.Dashboard;
using System.Security.Cryptography.X509Certificates;

namespace SmartMeterReadingDash.Services
{
    public class Dashboard
    {
        private readonly OracleCon _db;
        public Dashboard(OracleCon oracleCon)
        {
            _db = oracleCon;
            
        }
        //Test connection to the database
        public string Testconnection()
        {
            using var connection = _db.GetConnection();
            
            connection.Open();
            
            return connection.State.ToString();
        }

        //Get Meter Summary Allied + Kimbal including all department for the current month till day - 1
        public TotalMeterSummary GetMeterSummary(string ReadingMonth)
        {
            TotalMeterSummary Summary  = new TotalMeterSummary();

            using (OracleConnection  con = _db.GetConnection())
            {
                con.Open();

                string query = @"	
                WITH DOWNLOAD AS
                    (
                        SELECT
                            COUNT(*) AS DOWNLOAD_COUNT,
                            SUM(CASE WHEN SUBSTR(METERNO,1,2) IN ('90','AL') THEN 1 ELSE 0 END) AS ALLIED_DOWNLOAD,
                            SUM(CASE WHEN SUBSTR(METERNO,1,2)='91' THEN 1 ELSE 0 END) AS KIMBAL_DOWNLOAD
                        FROM RCMPA.SMART_METER_BILLING_DATA
                        WHERE METERNO NOT LIKE '%D%'
                          AND (
                                (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                          )
                         AND BILLMONTH = :READING_MONTH
                    ),
                    FAILED AS
                    (
                        SELECT
                            COUNT(DISTINCT METERNO) AS FAILED_COUNT,
                            COUNT(DISTINCT CASE
                                WHEN SUBSTR(METERNO,1,2) IN ('90','AL')
                                THEN METERNO
                            END) AS ALLIED_FAILED,
                            COUNT(DISTINCT CASE
                                WHEN SUBSTR(METERNO,1,2)='91'
                                THEN METERNO
                            END) AS KIMBAL_FAILED
                        FROM RCMPA.SMART_METER_SCHEDULER_LOGS
                        WHERE (
                                (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                          )
                          AND MESSAGE NOT LIKE 'Data%'
                          AND READING_MONTH = :READING_MONTH
                    )
                    SELECT
                        (D.DOWNLOAD_COUNT + F.FAILED_COUNT) AS TotalMeters,
                        (D.ALLIED_DOWNLOAD + F.ALLIED_FAILED) AS AlliedCount,
                        (D.KIMBAL_DOWNLOAD + F.KIMBAL_FAILED) AS KimbalCount
                    FROM DOWNLOAD D
                    CROSS JOIN FAILED F
                    ";
              

                using (OracleCommand cmd = new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2)
                      .Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                      
                        if (dr.Read())
                        {
                            Summary.TotalMeter = dr["TOTALMETERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTALMETERS"]);
                            Summary.AlliedCount = dr["ALLIEDCOUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIEDCOUNT"]);
                            Summary.KimbalCount = dr["KIMBALCOUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBALCOUNT"]);
                        }
                    }
                }
            }
            return Summary;
        }

        // Get Meter Download Summary Allied + Kimbal including all department for the current month till day - 1
        public MeterReceivedSummary GetMeterReceivedDownloadSummary(string ReadingMonth)
        {
            MeterReceivedSummary Summary = new MeterReceivedSummary();

            using(OracleConnection con  = _db.GetConnection())
            {
                con.Open();
                string query = @"
                   WITH DOWNLOAD AS
                    (
                        SELECT /*+ PARALLEL(SM,8) */
                            COUNT(*) AS HES_DOWNLOAD
                        FROM RCMPA.SMART_METER_BILLING_DATA
                        WHERE (
                                (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                        )
                        AND BILLMONTH = :READING_MONTH
                    ),
                    FAILED AS
                    (
                        SELECT
                            COUNT(DISTINCT METERNO) AS HES_FAILED
                        FROM RCMPA.SMART_METER_SCHEDULER_LOGS
                        WHERE (
                                (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                        )
                          AND MESSAGE NOT LIKE 'Data%'
                          AND READING_MONTH = :READING_MONTH
                    )
                    SELECT
                        (D.HES_DOWNLOAD + F.HES_FAILED) AS TOTAL_METERS,
                        D.HES_DOWNLOAD,
                        F.HES_FAILED,
                        ROUND(D.HES_DOWNLOAD * 100 / NULLIF(D.HES_DOWNLOAD + F.HES_FAILED,0), 2) AS HES_DOWNLOAD_PERCENTAGE,
                        ROUND(F.HES_FAILED * 100 / NULLIF(D.HES_DOWNLOAD + F.HES_FAILED,0), 2) AS HES_FAILED_PERCENTAGE
                    FROM DOWNLOAD D
                    CROSS JOIN FAILED F ";


                using(OracleCommand cmd =  new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                          Summary.totalMetersCount = dr["TOTAL_METERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_METERS"]);
                          Summary.hesDownloadCount = dr["HES_DOWNLOAD"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_DOWNLOAD"]);
                          Summary.manualForwardinCount = dr["HES_FAILED"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_FAILED"]);
                          Summary.hesDownloadPercentage = dr["HES_DOWNLOAD_PERCENTAGE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["HES_DOWNLOAD_PERCENTAGE"]);
                          Summary.hesFailedPercentage = dr["HES_FAILED_PERCENTAGE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["HES_FAILED_PERCENTAGE"]);
                        }
                    }
                }
            }
            return Summary;
        }

        // Get Meter Download Detailed Summary Allied + Kimbal including all department for the current month till day - 1
        public List<MeterDownloadDetailedSummary> MeterDetailedSummary(string ReadingMonth)
        {
            List<MeterDownloadDetailedSummary> SummaryList = new List<MeterDownloadDetailedSummary>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"SELECT
                            B.METERNO,
                            B.CONS_REF,
                            B.DISTRICT,
                            NVL(B.SAP_DEPARTMENT,'SLCC') AS SAP_DEPARTMENT,
                            CASE
                                WHEN SUBSTR(B.METERNO,1,2) IN ('90','AL') THEN 'ALLIED'
                                WHEN SUBSTR(B.METERNO,1,2)='91' THEN 'KIMBAL'
                            END AS METER_TYPE,
                            'Download' AS STATUS,
                            NULL AS SCHEDULER_MESSAGE,
                            NULL AS ENTRY_DATE
                        FROM RCMPA.SMART_METER_BILLING_DATA B
                        WHERE B.METERNO NOT LIKE '%D%'
                        AND (
                               (SUBSTR(B.METERNO,1,2)='91' AND LENGTH(B.METERNO)=8)
                            OR (SUBSTR(B.METERNO,1,2)='90' AND LENGTH(B.METERNO)=8)
                            OR (SUBSTR(B.METERNO,1,2)='AL' AND LENGTH(B.METERNO)=10)
                        )
                        AND B.BILLMONTH = :READING_MONTH

                        UNION ALL

                        SELECT
                            L.METERNO,
                            L.CONS_REF,
                            L.DISTRICT,
                            NVL(L.SAP_DEPARTMENT,'SLCC') AS SAP_DEPARTMENT,
                            CASE
                                WHEN SUBSTR(L.METERNO,1,2) IN ('90','AL') THEN 'ALLIED'
                                WHEN SUBSTR(L.METERNO,1,2)='91' THEN 'KIMBAL'
                            END AS METER_TYPE,
                            'Failed' AS STATUS,
                            L.MESSAGE AS SCHEDULER_MESSAGE,
                            L.ENTRY_DATE
                        FROM
                        (
                            SELECT
                                METERNO,
                                CONS_REF,
                                DISTRICT,
                                SAP_DEPARTMENT,
                                MESSAGE,
                                ENTRY_DATE,
                                ROW_NUMBER() OVER
                                (
                                    PARTITION BY METERNO
                                    ORDER BY ENTRY_DATE DESC
                                ) RN
                            FROM RCMPA.SMART_METER_SCHEDULER_LOGS
                            WHERE MESSAGE NOT LIKE 'Data%'
                              AND (
                                     (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                                  OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                                  OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                              )
                            AND READING_MONTH = :READING_MONTH
                        ) L
                        WHERE L.RN = 1
                        AND NOT EXISTS
                        (
                            SELECT 1
                            FROM RCMPA.SMART_METER_BILLING_DATA B
                            WHERE B.CONS_REF = L.CONS_REF
                        )

                        ORDER BY
                        SAP_DEPARTMENT,
                        DISTRICT,
                        STATUS,
                        METERNO";

                using(OracleCommand cmd  = new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                       
                        while (dr.Read())
                        {
                            SummaryList.Add(new MeterDownloadDetailedSummary
                            {
                                MeterNumber = dr["METERNO"].ToString(),
                                SapDepartment = dr["SAP_DEPARTMENT"].ToString(),
                                MeterType = dr["METER_TYPE"].ToString(),
                                ConsRef = dr["CONS_REF"].ToString(),
                                SapDivision = dr["DISTRICT"].ToString(),
                                Status = dr["STATUS"].ToString(),
                                SchedulerMessage = dr["SCHEDULER_MESSAGE"] == DBNull.Value ? null : dr["SCHEDULER_MESSAGE"].ToString(),
                                EntryDate = dr["ENTRY_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["ENTRY_DATE"])

                            });
                        }
                    }
                }
                return SummaryList;
            }
        
        }
        // get reading trend date wise for the current month till day - 1
        public List<ReadingTrendDateWise> GetReadingTrend(string ReadingMonth)
        {
            List<ReadingTrendDateWise> ReadingList = new List<ReadingTrendDateWise>();
            using(OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"
                    SELECT /*+ PARALLEL(8) */
                    READING_DATE,
                    COUNT(*) AS TOTAL_COUNT
                FROM
                (
                    SELECT  /*+ PARALLEL(SF,8) */ READING_DATE
                    FROM RCMPA.SAP_SLCC_FORMY
                    WHERE SAP_COMPANY = 'BRPL'
                      AND READING_MONTH = :READING_MONTH
                      AND READING_DATE IS NOT NULL
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                      )
                      AND CSTS_CD = 'R'
                    UNION ALL
                    SELECT /*+ PARALLEL(F,8) */ READING_DATE
                    FROM RCMPA.SAP_FORMY
                    WHERE SAP_COMPANY = 'BRPL'
                      AND READING_MONTH = :READING_MONTH
                      AND READING_DATE IS NOT NULL
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                      )
                      AND CSTS_CD = 'R'
                )
                GROUP BY READING_DATE
                ORDER BY READING_DATE
                ";
                using(OracleCommand cmd = new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while(dr.Read())
                        {
                            ReadingList.Add(new ReadingTrendDateWise
                            {
                                ReadingDate = Convert.ToDateTime(dr["READING_DATE"]),
                                ReadingCount = Convert.ToInt32(dr["TOTAL_COUNT"])
                            });
                        }
                    }
                }
            }
            return ReadingList;
        }
        public List<DepartmentWiseSummary> GetDepartmentSummary(string ReadingMonth)
        {
            List<DepartmentWiseSummary> departmentWiseData = new List<DepartmentWiseSummary>();
            using(OracleConnection conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"WITH DOWNLOAD AS
                    (
                        SELECT
                            CASE
                                WHEN SAP_DEPARTMENT = 'MLCC' AND CYCLE = '0N' THEN 'KCC'
                                WHEN CYCLE IN ('KA','KC','KG') THEN 'KCC'
                                WHEN SAP_DEPARTMENT IS NULL THEN 'SLCC'
                                ELSE SAP_DEPARTMENT
                            END AS DEPARTMENT,

                            COUNT(*) AS HES_DOWNLOAD
                        FROM RCMPA.SMART_METER_BILLING_DATA
                        WHERE (
                                (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                              )
                        AND BILLMONTH = :READING_MONTH
                        GROUP BY
                            CASE
                                WHEN SAP_DEPARTMENT = 'MLCC' AND CYCLE = '0N' THEN 'KCC'
                                WHEN CYCLE IN ('KA','KC','KG') THEN 'KCC'
                                WHEN SAP_DEPARTMENT IS NULL THEN 'SLCC'
                                ELSE SAP_DEPARTMENT
                            END
                    ),
                    FAILED AS
                    (
                        SELECT
                            CASE
                                WHEN SAP_DEPARTMENT = 'MLCC' AND CYCLE = '0N' THEN 'KCC'
                                WHEN CYCLE IN ('KA','KC','KG') THEN 'KCC'
                                WHEN SAP_DEPARTMENT IS NULL THEN 'SLCC'
                                ELSE SAP_DEPARTMENT
                            END AS DEPARTMENT,

                            COUNT(DISTINCT METERNO) AS FAILED
                        FROM RCMPA.SMART_METER_SCHEDULER_LOGS
                        WHERE (
                                (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                             OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                              )
                          AND MESSAGE NOT LIKE 'Data%'
                          AND READING_MONTH = :READING_MONTH
                        GROUP BY
                            CASE
                                WHEN SAP_DEPARTMENT = 'MLCC' AND CYCLE = '0N' THEN 'KCC'
                                WHEN CYCLE IN ('KA','KC','KG') THEN 'KCC'
                                WHEN SAP_DEPARTMENT IS NULL THEN 'SLCC'
                                ELSE SAP_DEPARTMENT
                            END
                    )
                    SELECT
                        COALESCE(D.DEPARTMENT, F.DEPARTMENT) AS DEPARTMENT,
                        NVL(D.HES_DOWNLOAD,0) AS HESDOWNLOAD,
                        NVL(F.FAILED,0) AS FAILED
                    FROM DOWNLOAD D
                    FULL OUTER JOIN FAILED F
                    ON D.DEPARTMENT = F.DEPARTMENT
                    ORDER BY DEPARTMENT";
                using(OracleCommand cmd = new OracleCommand(query,conn))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            departmentWiseData.Add(new DepartmentWiseSummary
                            {
                                Department = dr["DEPARTMENT"].ToString(),
                                HesDownload = Convert.ToInt32(dr["HESDOWNLOAD"]),
                                Failed = Convert.ToInt32(dr["FAILED"]),
                            });
                        }
                    }
                }
            }
            return departmentWiseData;
        }

        //public List<TempDashHesDownload> TempDashBoardHESCount()
        //{
        //    List<TempDashHesDownload> departmentWiseData = new List<TempDashHesDownload>();
        //    using (OracleConnection conn = _db.GetConnection())
        //    {
        //        conn.Open();
        //        string query = @"SELECT /*+ PARALLEL(8) */
        //            SUM(HES_DOWNLOAD) AS HES_DOWNLOAD,
        //            SUM(ALLIED_COUNT) AS ALLIED_COUNT,
        //            SUM(KIMBAL_COUNT) AS KIMBAL_COUNT,
        //            SUM(SLCC_COUNT) AS SLCC_COUNT,
        //            SUM(MLCC_COUNT) AS MLCC_COUNT,
        //            SUM(GCC_COUNT) AS GCC_COUNT,
        //            SUM(KCC_COUNT) AS KCC_COUNT
        //        FROM
        //        (
        //            -- SAP_SLCC_FORMY
        //            SELECT  /*+ PARALLEL(SF,8) */
        //                COUNT(*) AS HES_DOWNLOAD,
        //                SUM(CASE WHEN SUBSTR(METERNO,1,2) IN ('90','AL') THEN 1 ELSE 0 END) AS ALLIED_COUNT,
        //                SUM(CASE WHEN SUBSTR(METERNO,1,2)='91' THEN 1 ELSE 0 END) AS KIMBAL_COUNT,
        //                --SUM(CASE WHEN SAP_DEPARTMENT='SLCC' THEN 1 ELSE 0 END) AS SLCC_COUNT,
        //                SUM(CASE WHEN (SAP_DEPARTMENT='SLCC' OR SAP_DEPARTMENT IS NULL) THEN 1 ELSE 0 END) AS SLCC_COUNT,
        //                SUM(CASE WHEN SAP_DEPARTMENT='MLCC' AND CYCLE<>'0N' THEN 1 ELSE 0 END) AS MLCC_COUNT,
        //                SUM(CASE WHEN SAP_DEPARTMENT='GCC' THEN 1 ELSE 0 END) AS GCC_COUNT,
        //                SUM(CASE WHEN (SAP_DEPARTMENT = 'MLCC' AND CYCLE = '0N') OR CYCLE IN ('KA','KC','KG') THEN 1 ELSE 0 END) AS KCC_COUNT
        //            FROM RCMPA.SMART_METER_BILLING_DATA
        //          WHERE --SAP_COMPANY = 'BRPL'
        //          --AND READING_MONTH = TO_CHAR(SYSDATE,'YYYYMM')
        //         --AND SAP_MR_REASON_CODE = '01' 
        //          --AND CSTS_CD = 'R'
        //          --AND METERNO NOT LIKE '%D%'
        //          (
        //               (SUBSTR(METERNO,1,2) = '91' AND LENGTH(METERNO)=8)
        //            OR (SUBSTR(METERNO,1,2) = '90' AND LENGTH(METERNO)=8)
        //            OR (SUBSTR(METERNO,1,2) = 'AL' AND LENGTH(METERNO)=10)
        //          )
        //          )
  
        //         ";
        //        using (OracleCommand cmd = new OracleCommand(query, conn))
        //        {
        //            using (OracleDataReader dr = cmd.ExecuteReader())
        //            {
        //                while (dr.Read())
        //                {
        //                    departmentWiseData.Add(new TempDashHesDownload
        //                    {
        //                       HesDownload = dr["HES_DOWNLOAD"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_DOWNLOAD"]),
        //                       AlliedCount = dr["ALLIED_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_COUNT"]),
        //                       KimbalCount = dr["KIMBAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_COUNT"]),
        //                       SLCCount = dr["SLCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SLCC_COUNT"]),
        //                       MLCCCount = dr["MLCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MLCC_COUNT"]),
        //                       KCCount = dr["KCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KCC_COUNT"]),
        //                       GCCount = dr["GCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["GCC_COUNT"])
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    return departmentWiseData;
        //}

        //public List<TempDashHesFailed> TempHESFailed()
        //{
        //    List<TempDashHesFailed> departmentWiseData = new List<TempDashHesFailed>();
        //    using (OracleConnection conn = _db.GetConnection())
        //    {
        //        conn.Open();
        //        string query = @"SELECT
        //                COUNT(DISTINCT METERNO) AS HES_FAILED,
        //                COUNT(DISTINCT CASE
        //                    WHEN SUBSTR(METERNO,1,2) IN ('90','AL')
        //                    THEN METERNO
        //                END) AS ALLIED_COUNT,
        //                COUNT(DISTINCT CASE
        //                    WHEN SUBSTR(METERNO,1,2) = '91'
        //                    THEN METERNO
        //                END) AS KIMBAL_COUNT,
        //                COUNT(DISTINCT CASE
        //                    WHEN SAP_DEPARTMENT = 'SLCC'
        //                      OR SAP_DEPARTMENT IS NULL
        //                    THEN METERNO
        //                END) AS SLCC_COUNT,
        //                COUNT(DISTINCT CASE
        //                    WHEN SAP_DEPARTMENT = 'MLCC'
        //                     AND CYCLE <> '0N'
        //                    THEN METERNO
        //                END) AS MLCC_COUNT,
        //                COUNT(DISTINCT CASE
        //                    WHEN SAP_DEPARTMENT = 'GCC'
        //                    THEN METERNO
        //                END) AS GCC_COUNT,
        //                COUNT(DISTINCT CASE
        //                    WHEN (SAP_DEPARTMENT = 'MLCC' AND CYCLE = '0N')
        //                      OR CYCLE IN ('KA','KC','KG')
        //                    THEN METERNO
        //                END) AS KCC_COUNT
        //            FROM RCMPA.SMART_METER_SCHEDULER_LOGS
        //            WHERE
        //            (
        //                   (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
        //                OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
        //                OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
        //            )
        //            AND MESSAGE NOT LIKE 'Data%'";
        //        using (OracleCommand cmd = new OracleCommand(query, conn))
        //        {
        //            using (OracleDataReader dr = cmd.ExecuteReader())
        //            {
        //                while (dr.Read())
        //                {
        //                    departmentWiseData.Add(new TempDashHesFailed
        //                    {
        //                        HesFailed = dr["HES_FAILED"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_FAILED"]),
        //                        AlliedCount = dr["ALLIED_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_COUNT"]),
        //                        KimbalCount = dr["KIMBAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_COUNT"]),
        //                        SLCCount = dr["SLCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SLCC_COUNT"]),
        //                        MLCCCount = dr["MLCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MLCC_COUNT"]),
        //                        KCCount = dr["KCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KCC_COUNT"]),
        //                        GCCount = dr["GCC_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["GCC_COUNT"])
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    return departmentWiseData;
        //}
    }
}
