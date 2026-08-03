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
        public TotalMeterSummary GetMeterSummary()
        {
            TotalMeterSummary Summary  = new TotalMeterSummary();

            using (OracleConnection  con = _db.GetConnection())
            {
                con.Open();

                string query = @"	
                   SELECT /*+ PARALLEL(8) */
                        SUM(TOTAL_METERS) AS TOTAL_METERS,
                        SUM(ALLIED_COUNT) AS ALLIED_COUNT,
                        SUM(KIMBAL_COUNT) AS KIMBAL_COUNT,
                        SUM(SLCC_COUNT) AS SLCC_COUNT,
                        SUM(MLCC_COUNT) AS MLCC_COUNT,
                        SUM(GCC_COUNT) AS GCC_COUNT,
                        SUM(KCC_COUNT) AS KCC_COUNT
                    FROM
                    (
                        -- SAP_SLCC_FORMY
                        SELECT  /*+ PARALLEL(SF,8) */
                            COUNT(*) AS TOTAL_METERS,
                            SUM(CASE WHEN SUBSTR(METERNO,1,2) IN ('90','AL') THEN 1 ELSE 0 END) AS ALLIED_COUNT,
                            SUM(CASE WHEN SUBSTR(METERNO,1,2)='91' THEN 1 ELSE 0 END) AS KIMBAL_COUNT,
                            SUM(CASE WHEN SAP_DEPARTMENT='SLCC' THEN 1 ELSE 0 END) AS SLCC_COUNT,
                            0 AS MLCC_COUNT,
                            0 AS GCC_COUNT,
                            0 AS KCC_COUNT
                        FROM RCMPA.SAP_SLCC_FORMY
                      WHERE SAP_COMPANY = 'BRPL'
                      AND READING_MONTH = TO_CHAR(SYSDATE,'YYYYMM')
                      AND SAP_MRO_DOWNLOAD_DATE >= trunc(SYSDATE -1 ,'MM')
                      AND SAP_MRO_DOWNLOAD_DATE < trunc(SYSDATE)
                      AND NOT (
                        TRUNC(SAP_MRO_DOWNLOAD_DATE) = TO_DATE('23/07/2026','DD/MM/YYYY')
                        AND SAP_DEPARTMENT = 'GCC'
                    )
                     AND SAP_MR_REASON_CODE = '01' 
                      AND CSTS_CD = 'R'
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2) = '91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2) = '90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2) = 'AL' AND LENGTH(METERNO)=10)
                      )
                        UNION ALL
                        -- SAP_FORMY
                        SELECT /*+ PARALLEL(F,8) */
                            COUNT(*) AS TOTAL_METERS,
                            SUM(CASE WHEN SUBSTR(METERNO,1,2) IN ('90','AL') THEN 1 ELSE 0 END),
                            SUM(CASE WHEN SUBSTR(METERNO,1,2)='91' THEN 1 ELSE 0 END),
                            SUM(CASE WHEN SAP_DEPARTMENT='SLCC' THEN 1 ELSE 0 END),
                            SUM(CASE WHEN SAP_DEPARTMENT='MLCC' AND CYCLE<>'0N' THEN 1 ELSE 0 END),
                            SUM(CASE WHEN SAP_DEPARTMENT='GCC' THEN 1 ELSE 0 END),
                            SUM(CASE WHEN SAP_DEPARTMENT='MLCC' AND CYCLE='0N' THEN 1 ELSE 0 END)
                        FROM RCMPA.SAP_FORMY
                    WHERE SAP_COMPANY = 'BRPL'
                      AND READING_MONTH = TO_CHAR(SYSDATE,'YYYYMM')
                      AND SAP_MRO_DOWNLOAD_DATE >= trunc(SYSDATE -1 ,'MM')
                      AND SAP_MRO_DOWNLOAD_DATE < trunc(SYSDATE)
                      AND SAP_MRO_DOWNLOAD_DATE <> to_date('23/07/2026','dd/mm/yyyy')
                      AND SAP_MR_REASON_CODE = '01' 
                      AND CSTS_CD = 'R'
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2) = '91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2) = '90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2) = 'AL' AND LENGTH(METERNO)=10)
                    ))
                    ";

                using (OracleCommand cmd = new OracleCommand(query,con))
                {
                   using(OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if(dr.Read())
                        {
                            Summary.TotalMeter = dr["TOTAL_METERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_METERS"]);
                            Summary.AlliedCount = dr["ALLIED_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_COUNT"]);
                            Summary.KimbalCount = dr["KIMBAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_COUNT"]);
                        }
                    }
                }
            }
            return Summary;
        }

        // Get Meter Download Summary Allied + Kimbal including all department for the current month till day - 1
        public MeterReceivedSummary GetMeterReceivedDownloadSummary()
        {
            MeterReceivedSummary Summary = new MeterReceivedSummary();

            using(OracleConnection con  = _db.GetConnection())
            {
                con.Open();
                string query = @"
                    SELECT /*+ PARALLEL(8) */
                    SUM(TOTAL_METERS) TOTAL_METERS,
                    SUM(HES_DOWNLOAD) HES_DOWNLOAD,
                    SUM(MANUAL) MANUAL,
                    SUM(PENDING) PENDING,
                    SUM(MISMATCH) MISMATCH
                FROM
                (
                    -- SINGLE PHASE
                    SELECT /*+ PARALLEL(SF,8) */
                        COUNT(*) TOTAL_METERS,
                        SUM(
                            CASE
                                WHEN MTR_READ_MODE='1'
                                AND USER_ID LIKE 'HES%'
                                THEN 1
                                ELSE 0
                            END
                        ) HES_DOWNLOAD,
                        SUM(
                            CASE
                                WHEN MTR_READ_MODE='0'
                                  OR (MTR_READ_MODE IS NULL
                                      AND READING_DATE IS NOT NULL)
                                THEN 1
                                ELSE 0
                            END
                        ) MANUAL,
                        SUM(
                            CASE
                                WHEN READING_DATE IS NULL
                                THEN 1
                                ELSE 0
                            END
                        ) PENDING,
                        SUM(
                            CASE
                                WHEN MTR_READ_MODE='1'
                                 AND (
                                        NEW_MTR_NO IS NOT NULL
                                     OR MTR_CORR_STS IS NOT NULL
                                     OR MTR_NO_CORR IS NOT NULL
                                 )
                                THEN 1
                                ELSE 0
                            END
                        ) MISMATCH
                 FROM RCMPA.SAP_slcc_FORMY
                WHERE SAP_COMPANY = 'BRPL'
                  AND READING_MONTH = TO_CHAR(SYSDATE, 'YYYYMM')
                  AND SAP_MRO_DOWNLOAD_DATE >= trunc(SYSDATE - 1,'MM')
                  AND SAP_MRO_DOWNLOAD_DATE < trunc(SYSDATE)
                  AND NOT (
                    TRUNC(SAP_MRO_DOWNLOAD_DATE) = TO_DATE('23/07/2026','DD/MM/YYYY')
                    AND SAP_DEPARTMENT = 'GCC'
                )
                AND SAP_MR_REASON_CODE = '01' 
                  AND CSTS_CD = 'R'
                  AND METERNO NOT LIKE '%D%'
                  AND (
                       (SUBSTR(METERNO,1,2) = '91' AND LENGTH(METERNO) = 8)
                    OR (SUBSTR(METERNO,1,2) = '90' AND LENGTH(METERNO) = 8)
                    OR (SUBSTR(METERNO,1,2) = 'AL' AND LENGTH(METERNO) = 10)
                  )
                    UNION ALL
                    -- THREE PHASE
                    SELECT /*+ PARALLEL(F,8) */
                        COUNT(*) TOTAL_METERS,
                        SUM(
                            CASE
                                WHEN MTR_READ_MODE='1'
                                 AND USER_ID LIKE 'HES%'
                                THEN 1
                                ELSE 0
                            END
                        ),
                        SUM(
                            CASE
                                WHEN MTR_READ_MODE='0'
                                  OR (MTR_READ_MODE IS NULL
                                      AND READING_DATE IS NOT NULL)
                                THEN 1
                                ELSE 0
                            END
                        ),
                        SUM(
                            CASE
                                WHEN READING_DATE IS NULL
                                THEN 1
                                ELSE 0
                            END
                        ),
                        SUM(
                            CASE
                                WHEN MTR_READ_MODE='1'
                                 AND (
                                        NEW_MTR_NO IS NOT NULL
                                     OR MTR_CORR_STS IS NOT NULL
                                     OR MTR_NO_CORR IS NOT NULL
                                 )
                                THEN 1
                                ELSE 0
                            END
                        )
                  FROM RCMPA.SAP_FORMY
                WHERE SAP_COMPANY = 'BRPL'
                  AND READING_MONTH = TO_CHAR(SYSDATE,'YYYYMM')
                  AND SAP_MRO_DOWNLOAD_DATE >= TRUNC(SYSDATE - 1,'MM')
                  AND SAP_MRO_DOWNLOAD_DATE < TRUNC(SYSDATE)
                  AND SAP_MRO_DOWNLOAD_DATE <> to_date('23/07/2026','dd/mm/yyyy')
                  AND SAP_MR_REASON_CODE = '01' 
                  AND CSTS_CD = 'R'
                  AND METERNO NOT LIKE '%D%'
                  AND (
                       (SUBSTR(METERNO,1,2) = '91' AND LENGTH(METERNO)=8)
                    OR (SUBSTR(METERNO,1,2) = '90' AND LENGTH(METERNO)=8)
                    OR (SUBSTR(METERNO,1,2) = 'AL' AND LENGTH(METERNO)=10)
                  )) ";


                using(OracleCommand cmd =  new OracleCommand(query,con))
                {
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if(dr.Read())
                        {
                          Summary.totalMetersCount = dr["TOTAL_METERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_METERS"]);
                          Summary.hesDownloadCount = dr["HES_DOWNLOAD"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_DOWNLOAD"]);
                          Summary.mismatchCount = dr["MISMATCH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MISMATCH"]);
                          Summary.pendingCount = dr["PENDING"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PENDING"]);
                          Summary.manualForwardinCount = dr["MANUAL"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MANUAL"]);
                          Summary.hesDownloadPercentage = Summary.totalMetersCount == 0 ? 0 : 
                                Math.Round((decimal)Summary.hesDownloadCount * 100 / Summary.totalMetersCount, 2);

                        }
                    }
                }
            }
            return Summary;
        }

        // Get Meter Download Detailed Summary Allied + Kimbal including all department for the current month till day - 1
        public List<MeterDownloadDetailedSummary> MeterDetailedSummary()
        {
            List<MeterDownloadDetailedSummary> SummaryList = new List<MeterDownloadDetailedSummary>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"SELECT /*+ PARALLEL(8) */
                   METERNO,
                   SAP_DEPARTMENT,
                   CASE
                        WHEN SUBSTR(METERNO,1,2) IN ('90','AL') THEN 'ALLIED'
                        WHEN SUBSTR(METERNO,1,2) = '91' THEN 'KIMBAL'
                   END AS METER_TYPE,
                   CASE
                       WHEN READING_DATE IS NULL THEN 'PENDING'
                       WHEN MTR_READ_MODE = '1'
                            AND (NEW_MTR_NO IS NOT NULL
                              OR MTR_CORR_STS IS NOT NULL
                              OR MTR_NO_CORR IS NOT NULL)
                            THEN 'MISMATCH'
                       WHEN MTR_READ_MODE = '0'
                            OR (MTR_READ_MODE IS NULL AND READING_DATE IS NOT NULL)
                            THEN 'MANUAL'
                   END AS STATUS
            FROM RCMPA.SAP_SLCC_FORMY
            WHERE SAP_COMPANY = 'BRPL'
              AND READING_MONTH = TO_CHAR(SYSDATE,'YYYYMM')
              AND SAP_MRO_DOWNLOAD_DATE >= TRUNC(SYSDATE-1,'MM')
              AND SAP_MRO_DOWNLOAD_DATE < TRUNC(SYSDATE)
              AND NOT (
                    TRUNC(SAP_MRO_DOWNLOAD_DATE)=TO_DATE('23/07/2026','DD/MM/YYYY')
                    AND SAP_DEPARTMENT='GCC'
              )
              AND SAP_MR_REASON_CODE='01'
              AND CSTS_CD='R'
              AND METERNO NOT LIKE '%D%'
              AND (
                   (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
              )
              AND (
                   READING_DATE IS NULL
                OR (MTR_READ_MODE='0'
                    OR (MTR_READ_MODE IS NULL AND READING_DATE IS NOT NULL))
                OR (MTR_READ_MODE='1'
                    AND (NEW_MTR_NO IS NOT NULL
                      OR MTR_CORR_STS IS NOT NULL
                      OR MTR_NO_CORR IS NOT NULL))
              )
            UNION ALL
            SELECT /*+ PARALLEL(8) */
                   METERNO,
                   SAP_DEPARTMENT,
                   CASE
                        WHEN SUBSTR(METERNO,1,2) IN ('90','AL') THEN 'ALLIED'
                        WHEN SUBSTR(METERNO,1,2) = '91' THEN 'KIMBAL'
                   END AS METER_TYPE,
                   CASE
                       WHEN READING_DATE IS NULL THEN 'PENDING'
                       WHEN MTR_READ_MODE='1'
                            AND (NEW_MTR_NO IS NOT NULL
                              OR MTR_CORR_STS IS NOT NULL
                              OR MTR_NO_CORR IS NOT NULL)
                            THEN 'MISMATCH'
                       WHEN MTR_READ_MODE='0'
                            OR (MTR_READ_MODE IS NULL AND READING_DATE IS NOT NULL)
                            THEN 'MANUAL'
                   END AS STATUS
            FROM RCMPA.SAP_FORMY
            WHERE SAP_COMPANY='BRPL'
              AND READING_MONTH=TO_CHAR(SYSDATE,'YYYYMM')
              AND SAP_MRO_DOWNLOAD_DATE>=TRUNC(SYSDATE-1,'MM')
              AND SAP_MRO_DOWNLOAD_DATE<TRUNC(SYSDATE)
              AND SAP_MRO_DOWNLOAD_DATE<>TO_DATE('23/07/2026','DD/MM/YYYY')
              AND SAP_MR_REASON_CODE='01'
              AND CSTS_CD='R'
              AND METERNO NOT LIKE '%D%'
              AND (
                   (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
              )
              AND (
                   READING_DATE IS NULL
                OR (MTR_READ_MODE='0'
                    OR (MTR_READ_MODE IS NULL AND READING_DATE IS NOT NULL))
                OR (MTR_READ_MODE='1'
                    AND (NEW_MTR_NO IS NOT NULL
                      OR MTR_CORR_STS IS NOT NULL
                      OR MTR_NO_CORR IS NOT NULL))
              )";

                using(OracleCommand cmd  = new OracleCommand(query,con))
                {
                    using(OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            SummaryList.Add(new MeterDownloadDetailedSummary
                            {
                                MeterNumber = dr["METERNO"].ToString(),
                                SapDepartment = dr["SAP_DEPARTMENT"].ToString(),
                                MeterType = dr["METER_TYPE"].ToString(),
                                Status = dr["STATUS"].ToString()
                                
                            });
                        }
                    }
                }
                return SummaryList;
            }
        
        }
        // get reading trend date wise for the current month till day - 1
        public List<ReadingTrendDateWise> GetReadingTrend()
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
                      AND READING_MONTH = TO_CHAR(SYSDATE, 'YYYYMM')
                      AND READING_DATE IS NOT NULL
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                      )
                      AND CSTS_CD = 'R'
                      AND NOT (
                            TRUNC(SAP_MRO_DOWNLOAD_DATE) = TO_DATE('23/07/2026','DD/MM/YYYY')
                            AND SAP_DEPARTMENT = 'GCC'
                      )
                    UNION ALL
                    SELECT /*+ PARALLEL(F,8) */ READING_DATE
                    FROM RCMPA.SAP_FORMY
                    WHERE SAP_COMPANY = 'BRPL'
                      AND READING_MONTH = TO_CHAR(SYSDATE, 'YYYYMM')
                      AND READING_DATE IS NOT NULL
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                      )
                      AND CSTS_CD = 'R'
                      AND NOT (
                            TRUNC(SAP_MRO_DOWNLOAD_DATE) = TO_DATE('23/07/2026','DD/MM/YYYY')
                            AND SAP_DEPARTMENT = 'GCC'
                      )
                )
                GROUP BY READING_DATE
                ORDER BY READING_DATE
                ";
                using(OracleCommand cmd = new OracleCommand(query,con))
                {
                    using(OracleDataReader dr = cmd.ExecuteReader())
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
        public List<DepartmentWiseSummary> GetDepartmentSummary()
        {
            List<DepartmentWiseSummary> departmentWiseData = new List<DepartmentWiseSummary>();
            using(OracleConnection conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT /*+ PARALLEL(8) */
                    DEPARTMENT,
                    SUM(TOTAL_METERS)    AS TOTAL_METERS,
                    SUM(HES_DOWNLOAD)    AS HES_DOWNLOAD,
                    SUM(MANUAL)          AS MANUAL,
                    SUM(PENDING)         AS PENDING,
                    SUM(MISMATCH)        AS MISMATCH,
                    SUM(MANUAL + MISMATCH) AS NON_COMMUNICATION
                FROM
                (
                    SELECT /*+ PARALLEL(SF,8) */
                        SAP_DEPARTMENT AS DEPARTMENT,
                        COUNT(*) AS TOTAL_METERS,
                        SUM(CASE
                                WHEN MTR_READ_MODE='1'
                                 AND USER_ID LIKE 'HES%'
                                THEN 1 ELSE 0
                            END) AS HES_DOWNLOAD,
                        SUM(CASE
                                WHEN MTR_READ_MODE='0'
                                  OR (MTR_READ_MODE IS NULL
                                      AND READING_DATE IS NOT NULL)
                                THEN 1 ELSE 0
                            END) AS MANUAL,
                        SUM(CASE
                                WHEN READING_DATE IS NULL
                                THEN 1 ELSE 0
                            END) AS PENDING,
                        SUM(CASE
                                WHEN MTR_READ_MODE='1'
                                 AND (
                                        NEW_MTR_NO IS NOT NULL
                                     OR MTR_CORR_STS IS NOT NULL
                                     OR MTR_NO_CORR IS NOT NULL
                                 )
                                THEN 1 ELSE 0
                            END) AS MISMATCH
                    FROM RCMPA.SAP_SLCC_FORMY
                    WHERE SAP_COMPANY='BRPL'
                      AND READING_MONTH=TO_CHAR(SYSDATE,'YYYYMM')
                      AND SAP_MRO_DOWNLOAD_DATE>=TRUNC(SYSDATE-1,'MM')
                      AND SAP_MRO_DOWNLOAD_DATE<TRUNC(SYSDATE)
                      AND NOT (
                            TRUNC(SAP_MRO_DOWNLOAD_DATE)=TO_DATE('23/07/2026','DD/MM/YYYY')
                        AND SAP_DEPARTMENT='GCC'
                      )
                      AND SAP_MR_REASON_CODE='01'
                      AND CSTS_CD='R'
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                      )
                    GROUP BY SAP_DEPARTMENT
                    UNION ALL
                    SELECT /*+ PARALLEL(F,8) */
                        CASE
                            WHEN SAP_DEPARTMENT='MLCC' AND CYCLE='0N' THEN 'KCC'
                            WHEN SAP_DEPARTMENT='MLCC' THEN 'MLCC'
                            ELSE SAP_DEPARTMENT
                        END AS DEPARTMENT,
                        COUNT(*) AS TOTAL_METERS,
                        SUM(CASE
                                WHEN MTR_READ_MODE='1'
                                 AND USER_ID LIKE 'HES%'
                                THEN 1 ELSE 0
                            END),
                        SUM(CASE
                                WHEN MTR_READ_MODE='0'
                                  OR (MTR_READ_MODE IS NULL
                                      AND READING_DATE IS NOT NULL)
                                THEN 1 ELSE 0
                            END),
                        SUM(CASE
                                WHEN READING_DATE IS NULL
                                THEN 1 ELSE 0
                            END),
                        SUM(CASE
                                WHEN MTR_READ_MODE='1'
                                 AND (
                                        NEW_MTR_NO IS NOT NULL
                                     OR MTR_CORR_STS IS NOT NULL
                                     OR MTR_NO_CORR IS NOT NULL
                                 )
                                THEN 1 ELSE 0
                            END)
                    FROM RCMPA.SAP_FORMY
                    WHERE SAP_COMPANY='BRPL'
                      AND READING_MONTH=TO_CHAR(SYSDATE,'YYYYMM')
                      AND SAP_MRO_DOWNLOAD_DATE>=TRUNC(SYSDATE-1,'MM')
                      AND SAP_MRO_DOWNLOAD_DATE<TRUNC(SYSDATE)
                      AND SAP_MRO_DOWNLOAD_DATE<>TO_DATE('23/07/2026','DD/MM/YYYY')
                      AND SAP_MR_REASON_CODE='01'
                      AND CSTS_CD='R'
                      AND METERNO NOT LIKE '%D%'
                      AND (
                           (SUBSTR(METERNO,1,2)='91' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='90' AND LENGTH(METERNO)=8)
                        OR (SUBSTR(METERNO,1,2)='AL' AND LENGTH(METERNO)=10)
                      )
                    GROUP BY
                        CASE
                            WHEN SAP_DEPARTMENT='MLCC' AND CYCLE='0N' THEN 'KCC'
                            WHEN SAP_DEPARTMENT='MLCC' THEN 'MLCC'
                            ELSE SAP_DEPARTMENT
                        END
                )
                GROUP BY DEPARTMENT
                ORDER BY DEPARTMENT";
                using(OracleCommand cmd = new OracleCommand(query,conn))
                {
                    using(OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            departmentWiseData.Add(new DepartmentWiseSummary
                            {
                                Department = dr["DEPARTMENT"].ToString(),
                                TotalMeters = Convert.ToInt32(dr["TOTAL_METERS"]),
                                HesDownload = Convert.ToInt32(dr["HES_DOWNLOAD"]),
                                Manual = Convert.ToInt32(dr["MANUAL"]),
                                Pending = Convert.ToInt32(dr["PENDING"]),
                                Mismatch = Convert.ToInt32(dr["MISMATCH"]),
                                NonCom = Convert.ToInt32(dr["NON_COMMUNICATION"])
                            });
                        }
                    }
                }
            }
            return departmentWiseData;
        }
    }
}
