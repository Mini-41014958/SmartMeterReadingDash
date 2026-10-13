using Microsoft.AspNetCore.Identity;
using Oracle.ManagedDataAccess.Client;
using SmartMeterReadingDash.Models.Dashboard;
using System.Data;
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

        //Test connection
        public string Testconnection()
        {
            using var connection = _db.GetConnection();

            connection.Open();

            return connection.State.ToString();
        }

        // Get Meter Summary Allied + Kimbal including all department BRPL
        public TotalMeterSummary GetMeterSummary(string ReadingMonth, UserAccessScope scope)
        {
            TotalMeterSummary Summary = new TotalMeterSummary();

            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();

                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */ TRIM(REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL)) AS READING_MONTH
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL) IS NOT NULL
                ),
                BILLING_DATA AS
                (
                    SELECT METERNO, READING_MONTH, BILLMONTH FROM RCMPA.SMART_METER_BILLING_DATA_SEP
                    UNION ALL
                    SELECT METERNO, READING_MONTH, BILLMONTH FROM RCMPA.SMART_METER_BILLING_DATA
                ),
                DOWNLOAD_BASE AS
                (
                    SELECT SM.METERNO, SM.READING_MONTH, SM.BILLMONTH,
                           CASE
                               WHEN SM.SAP_DEPARTMENT = 'MLCC' AND SM.CYCLE = '0N' THEN 'KCC'
                               WHEN SM.CYCLE IN ('KA', 'KC', 'KG') THEN 'KCC'
                               WHEN SM.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                               ELSE UPPER(TRIM(SM.SAP_DEPARTMENT))
                           END AS DEPARTMENT
                    FROM
                    (
                        SELECT METERNO, READING_MONTH, BILLMONTH, SAP_DEPARTMENT, CYCLE FROM RCMPA.SMART_METER_BILLING_DATA_SEP
                        UNION ALL
                        SELECT METERNO, READING_MONTH, BILLMONTH, SAP_DEPARTMENT, CYCLE FROM RCMPA.SMART_METER_BILLING_DATA
                    ) SM
                    WHERE SM.METERNO NOT LIKE '%D%'
                      AND
                      (
                             SM.METERNO LIKE '91______'
                          OR SM.METERNO LIKE '90______'
                          OR SM.METERNO LIKE 'AL________'
                          OR SM.METERNO LIKE 'KI________'
                          OR SM.METERNO LIKE 'RSE%'
                      )
                ),
                DOWNLOAD_FILTERED AS
                (
                    SELECT /*+ MATERIALIZE */ D.METERNO, D.DEPARTMENT FROM DOWNLOAD_BASE D
                    WHERE (:IS_SUPERADMIN = 1 OR D.DEPARTMENT = UPPER(:DEPARTMENT))
                      AND
                      (
                          (
                              D.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                              AND EXISTS
                              (
                                  SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = D.BILLMONTH
                              )
                          )
                          OR
                          (
                              D.DEPARTMENT = 'SLCC'
                              AND EXISTS
                              (
                                  SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = D.READING_MONTH
                              )
                          )
                      )
                ),
                DOWNLOAD AS
                (
                    SELECT /*+ PARALLEL(8) */ COUNT(DISTINCT METERNO) AS DOWNLOAD_COUNT,
                        COUNT(DISTINCT CASE
                            WHEN (METERNO LIKE '90%' OR METERNO LIKE 'AL%')
                             AND METERNO NOT LIKE '9026%'
                             AND METERNO NOT LIKE 'RSE%'
                            THEN METERNO
                        END) AS ALLIED_DOWNLOAD,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE '91%'
                              OR METERNO LIKE 'KI%'
                              OR METERNO LIKE '9026%'
                            THEN METERNO
                        END) AS KIMBAL_DOWNLOAD,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'RSE%'
                            THEN METERNO
                        END) AS SECURE_DOWNLOAD,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'AL91%'
                              OR METERNO LIKE 'KI91%'
                              OR METERNO LIKE '9150%'
                            THEN METERNO
                        END) AS PHASE_1PH,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE '9026%'
                              OR METERNO LIKE 'KI90%'
                              OR METERNO LIKE 'AL90%'
                              OR METERNO LIKE 'RSE%'
                            THEN METERNO
                        END) AS PHASE_3PH,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'AL91%'
                              OR METERNO LIKE '9008%'
                              OR METERNO LIKE '9027%'
                            THEN METERNO
                        END) AS ALLIED_1PH,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'AL90%'
                            THEN METERNO
                        END) AS ALLIED_3PH,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'KI91%'
                              OR METERNO LIKE '9150%'
                            THEN METERNO
                        END) AS KIMBAL_1PH,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'KI90%'
                              OR METERNO LIKE '9026%'
                            THEN METERNO
                        END) AS KIMBAL_3PH,
                        COUNT(DISTINCT CASE
                            WHEN METERNO LIKE 'RSE%'
                            THEN METERNO
                        END) AS SECURE_3PH
                    FROM DOWNLOAD_FILTERED
                ),
                FAILED_BASE AS
                (
                    SELECT L.METERNO, L.READING_MONTH, L.BILLMONTH,
                           CASE
                               WHEN L.SAP_DEPARTMENT = 'MLCC' AND L.CYCLE = '0N' THEN 'KCC'
                               WHEN L.CYCLE IN ('KA', 'KC', 'KG') THEN 'KCC'
                               WHEN L.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                               ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                           END AS DEPARTMENT
                    FROM RCMPA.SMART_METER_SCHEDULER_LOGS L
                    WHERE
                    (
                           L.METERNO LIKE '91______'
                        OR L.METERNO LIKE '90______'
                        OR L.METERNO LIKE 'AL________'
                        OR L.METERNO LIKE 'KI________'
                        OR L.METERNO LIKE 'RSE%'
                    )
                      AND L.MESSAGE NOT LIKE 'Data%'
                ),
                FAILED_FILTERED AS
                (
                    SELECT F.METERNO, F.READING_MONTH,  F.BILLMONTH, F.DEPARTMENT FROM FAILED_BASE F
                    WHERE
                          (:IS_SUPERADMIN = 1 OR F.DEPARTMENT = UPPER(:DEPARTMENT))
                      AND
                      (
                          (
                              F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                              AND EXISTS
                              (
                                  SELECT 1 FROM MONTHS M  WHERE M.READING_MONTH = F.BILLMONTH
                              )
                          )
                          OR
                          (
                              F.DEPARTMENT = 'SLCC'
                              AND EXISTS
                              (
                                  SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = F.READING_MONTH
                              )
                          )
                      )
                ),
                FAILED AS
                (
                    SELECT /*+ PARALLEL(8) */ COUNT(DISTINCT F.METERNO) AS FAILED_COUNT,
                        COUNT(DISTINCT CASE
                            WHEN (F.METERNO LIKE '90%' OR F.METERNO LIKE 'AL%')
                             AND F.METERNO NOT LIKE '9026%'
                             AND F.METERNO NOT LIKE 'RSE%'
                            THEN F.METERNO
                        END) AS ALLIED_FAILED,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE '91%'
                              OR F.METERNO LIKE 'KI%'
                              OR F.METERNO LIKE '9026%'
                            THEN F.METERNO
                        END) AS KIMBAL_FAILED,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'RSE%'
                            THEN F.METERNO
                        END) AS SECURE_FAILED,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'AL91%'
                              OR F.METERNO LIKE 'KI91%'
                              OR F.METERNO LIKE '9150%'
                            THEN F.METERNO
                        END) AS PHASE_1PH,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE '9026%'
                              OR F.METERNO LIKE 'KI90%'
                              OR F.METERNO LIKE 'AL90%'
                              OR F.METERNO LIKE 'RSE%'
                            THEN F.METERNO
                        END) AS PHASE_3PH,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'AL91%'
                              OR F.METERNO LIKE '9008%'
                              OR F.METERNO LIKE '9027%'
                            THEN F.METERNO
                        END) AS ALLIED_1PH,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'AL90%'
                            THEN F.METERNO
                        END) AS ALLIED_3PH,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'KI91%'
                              OR F.METERNO LIKE '9150%'
                            THEN F.METERNO
                        END) AS KIMBAL_1PH,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'KI90%'
                              OR F.METERNO LIKE '9026%'
                            THEN F.METERNO
                        END) AS KIMBAL_3PH,
                        COUNT(DISTINCT CASE
                            WHEN F.METERNO LIKE 'RSE%'
                            THEN F.METERNO
                        END) AS SECURE_3PH
                    FROM FAILED_FILTERED F
                    WHERE NOT EXISTS
                    (
                        SELECT 1 FROM BILLING_DATA B WHERE B.METERNO = F.METERNO
                          AND
                          (
                              (
                                  F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC') AND B.BILLMONTH = F.BILLMONTH
                              )
                              OR
                              (
                                  F.DEPARTMENT = 'SLCC' AND B.READING_MONTH = F.READING_MONTH
                              )
                          )
                    )
                ),
                FINAL_DATA AS
                (
                    SELECT  NVL(D.DOWNLOAD_COUNT, 0) + NVL(F.FAILED_COUNT, 0) AS TOTALMETERS, NVL(D.ALLIED_DOWNLOAD, 0)  + NVL(F.ALLIED_FAILED, 0) AS ALLIEDCOUNT,
                        NVL(D.KIMBAL_DOWNLOAD, 0) + NVL(F.KIMBAL_FAILED, 0) AS KIMBALCOUNT,
                        NVL(D.SECURE_DOWNLOAD, 0)  + NVL(F.SECURE_FAILED, 0) AS SECURECOUNT,
                        NVL(D.PHASE_1PH, 0) + NVL(F.PHASE_1PH, 0) AS PHASE_1PH,
                        NVL(D.PHASE_3PH, 0)  + NVL(F.PHASE_3PH, 0) AS PHASE_3PH,
                        NVL(D.ALLIED_1PH, 0)  + NVL(F.ALLIED_1PH, 0) AS ALLIED_1PH,
                        NVL(D.ALLIED_3PH, 0) + NVL(F.ALLIED_3PH, 0) AS ALLIED_3PH,
                        NVL(D.KIMBAL_1PH, 0) + NVL(F.KIMBAL_1PH, 0) AS KIMBAL_1PH,
                        NVL(D.KIMBAL_3PH, 0)  + NVL(F.KIMBAL_3PH, 0) AS KIMBAL_3PH,
                        NVL(D.SECURE_3PH, 0)  + NVL(F.SECURE_3PH, 0) AS SECURE_3PH
                    FROM DOWNLOAD D CROSS JOIN FAILED F
                )
                SELECT TOTALMETERS, ALLIEDCOUNT, KIMBALCOUNT, SECURECOUNT, PHASE_1PH, PHASE_3PH, ALLIED_1PH, ALLIED_3PH, KIMBAL_1PH, KIMBAL_3PH,SECURE_3PH FROM FINAL_DATA";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add("READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;

                    cmd.Parameters.Add("IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;

                    cmd.Parameters.Add("DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department)
                            ? string.Empty
                            : scope.Department.Trim().ToUpperInvariant();


                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            Summary.TotalMeter = dr["TOTALMETERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTALMETERS"]);

                            Summary.AlliedCount = dr["ALLIEDCOUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIEDCOUNT"]);

                            Summary.KimbalCount = dr["KIMBALCOUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBALCOUNT"]);

                            Summary.Allied_1PhCount = dr["ALLIED_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_1PH"]);

                            Summary.Allied_3PhCount = dr["ALLIED_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_3PH"]);

                            Summary.Kimbal_1PhCount = dr["KIMBAL_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_1PH"]);

                            Summary.kimbal_3PhCount = dr["KIMBAL_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_3PH"]);

                            Summary.total_1PhCount = dr["PHASE_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PHASE_1PH"]);

                            Summary.total_3PhCount = dr["PHASE_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PHASE_3PH"]);

                            Summary.SecureCount = dr["SECURECOUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SECURECOUNT"]);

                            Summary.Secure_3PhCount = dr["SECURE_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SECURE_3PH"]);

                            Summary.Secure_1PhCount = 0; // Assuming Secure_1PhCount is not available in the query result, set it to 0 or handle accordingly
                        }
                    }
                }
            }

            return Summary;
        }

        // Get Meter Summary Allied + Kimbal including all department BYPL
        public TotalMeterSummaryBypl GetByplTotalMeterSummary(string readingMonth, UserAccessScope scope)
        {
            TotalMeterSummaryBypl totalMeterSummary = new TotalMeterSummaryBypl();

            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();

                string query = @"WITH MONTHS (READING_MONTH) AS
                (
                    SELECT TRIM( REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL) )
                    FROM DUAL CONNECT BY REGEXP_SUBSTR(:READING_MONTH,'[^,]+', 1, LEVEL) IS NOT NULL
                ),
                ALL_LOG_DATA AS
                (
                    SELECT TRIM(S.METERNO) AS METERNO, TRIM(S.BILL_MONTH) AS BILL_MONTH, S.IS_FAILED
                    FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S WHERE S.BILL_MONTH IN
                    (
                        SELECT READING_MONTH
                        FROM MONTHS
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER(
                            CASE
                                WHEN S.DEPARTMENT = 'MLCC' AND S.CYCLE = '0N'
                                THEN 'KCC'
                                WHEN S.CYCLE IN ('KA', 'KC', 'KG')
                                THEN 'KCC'
                                WHEN S.DEPARTMENT IS NULL
                                THEN 'SLCC'
                                ELSE TRIM(S.DEPARTMENT)
                            END
                        ) = UPPER(:DEPARTMENT)
                    )
                    UNION ALL
                    SELECT TRIM(K.METERNO) AS METERNO, TRIM(K.BILL_MONTH) AS BILL_MONTH, K.IS_FAILED
                    FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                    WHERE K.BILL_MONTH IN
                    (
                        SELECT READING_MONTH
                        FROM MONTHS
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER(
                            CASE
                                WHEN K.DEPARTMENT = 'MLCC' AND K.CYCLE = '0N'
                                THEN 'KCC'
                                WHEN K.CYCLE IN ('KA', 'KC', 'KG')
                                THEN 'KCC'
                                WHEN K.DEPARTMENT IS NULL
                                THEN 'SLCC'
                                ELSE TRIM(K.DEPARTMENT)
                            END
                        ) = UPPER(:DEPARTMENT)
                    )
                ),
                DISTINCT_DATA AS
                (
                    SELECT DISTINCT METERNO, BILL_MONTH, IS_FAILED FROM ALL_LOG_DATA WHERE METERNO IS NOT NULL
                ),
                DISTINCT_METERS AS
                (
                    SELECT DISTINCT METERNO FROM DISTINCT_DATA
                ),
                METER_STATUS AS
                (
                    SELECT METERNO,  MAX(IS_FAILED) AS IS_FAILED FROM DISTINCT_DATA GROUP BY METERNO
                ),
                SUMMARY AS
                (
                    SELECT COUNT(DISTINCT METERNO) AS TOTAL_METERS,
                        COUNT(
                            DISTINCT CASE
                                WHEN
                                    METERNO LIKE '92%'
                                    OR METERNO LIKE 'AL%'
                                    OR METERNO LIKE '99%'
                                THEN METERNO
                            END
                        ) AS ALLIED_COUNT,
                        COUNT(
                            DISTINCT CASE
                                WHEN
                                    METERNO LIKE '92%'
                                    OR METERNO LIKE '99%'
                                    OR METERNO LIKE 'AL92%'
                                    OR METERNO LIKE 'AL99%'
                                THEN METERNO
                            END
                        ) AS ALLIED_1PH,
                        COUNT(
                            DISTINCT CASE
                                WHEN METERNO LIKE 'AL97%'
                                THEN METERNO
                            END
                        ) AS ALLIED_3PH,
                        COUNT(
                            DISTINCT CASE
                                WHEN
                                    METERNO LIKE 'KI%'
                                    OR METERNO LIKE '97%'
                                    OR METERNO LIKE '98%'
                                THEN METERNO
                            END
                        ) AS KIMBAL_COUNT,
                        COUNT(
                            DISTINCT CASE
                                WHEN
                                    METERNO LIKE '98%'
                                    OR METERNO LIKE 'KI98%'
                                THEN METERNO
                            END
                        ) AS KIMBAL_1PH,
                        COUNT(
                            DISTINCT CASE
                                WHEN
                                    METERNO LIKE '97%'
                                    OR METERNO LIKE 'KI97%'
                                THEN METERNO
                            END
                        ) AS KIMBAL_3PH
                    FROM DISTINCT_METERS
                )
                SELECT TOTAL_METERS,ALLIED_COUNT, ALLIED_1PH, ALLIED_3PH, KIMBAL_COUNT, KIMBAL_1PH, KIMBAL_3PH FROM SUMMARY";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("READING_MONTH", OracleDbType.Varchar2).Value = readingMonth;
                    cmd.Parameters.Add("IS_SUPERADMIN", OracleDbType.Int32).Value = scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add("DEPARTMENT", OracleDbType.Varchar2).Value = (scope.Department ?? string.Empty).Trim().ToUpper();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalMeterSummary.TotalMeter = reader["TOTAL_METERS"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TOTAL_METERS"]);

                            totalMeterSummary.AlliedCount = reader["ALLIED_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ALLIED_COUNT"]);

                            totalMeterSummary.Allied_1PhCount = reader["ALLIED_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ALLIED_1PH"]);

                            totalMeterSummary.Allied_3PhCount = reader["ALLIED_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ALLIED_3PH"]);

                            totalMeterSummary.KimbalCount = reader["KIMBAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["KIMBAL_COUNT"]);

                            totalMeterSummary.Kimbal_1PhCount = reader["KIMBAL_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(reader["KIMBAL_1PH"]);

                            totalMeterSummary.Kimbal_3PhCount = reader["KIMBAL_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(reader["KIMBAL_3PH"]);
                        }
                    }
                }
            }

            return totalMeterSummary;
        }

        // Get Meter Download/Failed Summary Allied + Kimbal 
        public MeterReceivedSummary GetMeterReceivedDownloadSummary(string ReadingMonth, UserAccessScope scope)
        {
            MeterReceivedSummary Summary = new MeterReceivedSummary();

            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();

                string query = @"
WITH MONTHS AS
(
    SELECT DISTINCT
           TRIM(REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL)) AS READING_MONTH
    FROM DUAL
    CONNECT BY REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL) IS NOT NULL
),
BILL_RAW AS
(
    SELECT METERNO, READING_MONTH, BILLMONTH,
           MACHINE_ID, SAP_DEPARTMENT, CYCLE
    FROM RCMPA.SMART_METER_BILLING_DATA_SEP
    WHERE METERNO LIKE '91______'
       OR METERNO LIKE '90______'
       OR METERNO LIKE 'AL________'
       OR METERNO LIKE 'KI________'
       OR METERNO LIKE 'RSE%'

    UNION ALL

    SELECT METERNO, READING_MONTH, BILLMONTH,
           MACHINE_ID, SAP_DEPARTMENT, CYCLE
    FROM RCMPA.SMART_METER_BILLING_DATA
    WHERE METERNO LIKE '91______'
       OR METERNO LIKE '90______'
       OR METERNO LIKE 'AL________'
       OR METERNO LIKE 'KI________'
       OR METERNO LIKE 'RSE%'
),
BILL_NORMALIZED AS
(
    SELECT
        B.METERNO,
        B.READING_MONTH,
        B.BILLMONTH,
        B.MACHINE_ID,
        CASE
            WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N'
                THEN 'KCC'
            WHEN B.CYCLE IN ('KA', 'KC', 'KG')
                THEN 'KCC'
            WHEN B.SAP_DEPARTMENT IS NULL
                THEN 'SLCC'
            ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
        END AS DEPARTMENT
    FROM BILL_RAW B
),
BILL AS
(
    SELECT B.METERNO, B.READING_MONTH, B.BILLMONTH,
           B.MACHINE_ID, B.DEPARTMENT
    FROM BILL_NORMALIZED B
    WHERE
        (:IS_SUPERADMIN = 1 OR B.DEPARTMENT = UPPER(:DEPARTMENT))
        AND
        (
            (
                B.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                AND EXISTS
                (
                    SELECT 1
                    FROM MONTHS M
                    WHERE M.READING_MONTH = B.BILLMONTH
                )
            )
            OR
            (
                B.DEPARTMENT = 'SLCC'
                AND EXISTS
                (
                    SELECT 1
                    FROM MONTHS M
                    WHERE M.READING_MONTH = B.READING_MONTH
                )
            )
        )
),
DL_METERS AS
(
    SELECT
        TRIM(METERNO) AS METERNO,
        MAX(
            CASE
                WHEN MACHINE_ID IS NOT NULL THEN 1
                ELSE 0
            END
        ) AS IS_CMRI
    FROM BILL
    GROUP BY TRIM(METERNO)
),
DL AS
(
    SELECT
        COUNT(CASE WHEN IS_CMRI = 0 THEN 1 END) AS HES_DOWNLOAD,
        COUNT(CASE WHEN IS_CMRI = 1 THEN 1 END) AS CMRI_DOWNLOAD
    FROM DL_METERS
),
FAILED_BASE AS
(
    SELECT
        L.METERNO,
        L.READING_MONTH,
        L.BILLMONTH,
        CASE
            WHEN L.SAP_DEPARTMENT = 'MLCC' AND L.CYCLE = '0N'
                THEN 'KCC'
            WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                THEN 'KCC'
            WHEN L.SAP_DEPARTMENT IS NULL
                THEN 'SLCC'
            ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
        END AS DEPARTMENT
    FROM RCMPA.SMART_METER_SCHEDULER_LOGS L
    WHERE
        (
               L.METERNO LIKE '91______'
            OR L.METERNO LIKE '90______'
            OR L.METERNO LIKE 'AL________'
            OR L.METERNO LIKE 'KI________'
            OR L.METERNO LIKE 'RSE%'
        )
        AND L.MESSAGE NOT LIKE 'Data%'
),
FAILED_FILTERED AS
(
    SELECT F.METERNO, F.READING_MONTH, F.BILLMONTH, F.DEPARTMENT
    FROM FAILED_BASE F
    WHERE
        (:IS_SUPERADMIN = 1 OR F.DEPARTMENT = UPPER(:DEPARTMENT))
        AND
        (
            (
                F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                AND EXISTS
                (
                    SELECT 1
                    FROM MONTHS M
                    WHERE M.READING_MONTH = F.BILLMONTH
                )
            )
            OR
            (
                F.DEPARTMENT = 'SLCC'
                AND EXISTS
                (
                    SELECT 1
                    FROM MONTHS M
                    WHERE M.READING_MONTH = F.READING_MONTH
                )
            )
        )
),
FAILED_METERS AS
(
    SELECT TRIM(F.METERNO) AS METERNO
    FROM FAILED_FILTERED F
    WHERE F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
      AND NOT EXISTS
      (
          SELECT 1
          FROM BILL B
          WHERE B.METERNO = F.METERNO
            AND B.BILLMONTH = F.BILLMONTH
      )

    UNION

    SELECT TRIM(F.METERNO) AS METERNO
    FROM FAILED_FILTERED F
    WHERE F.DEPARTMENT = 'SLCC'
      AND NOT EXISTS
      (
          SELECT 1
          FROM BILL B
          WHERE B.METERNO = F.METERNO
            AND B.READING_MONTH = F.READING_MONTH
      )
),
FAILED AS
(
    SELECT COUNT(*) AS HES_FAILED
    FROM FAILED_METERS
),
FINAL_DATA AS
(
    SELECT
        NVL(D.HES_DOWNLOAD, 0) AS HES_DOWNLOAD,
        NVL(D.CMRI_DOWNLOAD, 0) AS CMRI_DOWNLOAD,
        NVL(F.HES_FAILED, 0) AS HES_FAILED,
        NVL(D.HES_DOWNLOAD, 0)
          + NVL(D.CMRI_DOWNLOAD, 0)
          + NVL(F.HES_FAILED, 0) AS TOTAL_METERS
    FROM DL D
    CROSS JOIN FAILED F
)
SELECT
    TOTAL_METERS,
    HES_DOWNLOAD,
    CMRI_DOWNLOAD,
    HES_FAILED,

    ROUND(
        NVL(HES_DOWNLOAD * 100 / NULLIF(TOTAL_METERS, 0), 0),
        2
    ) AS HES_DOWNLOAD_PERCENTAGE,

    ROUND(
        NVL(CMRI_DOWNLOAD * 100 / NULLIF(TOTAL_METERS, 0), 0),
        2
    ) AS CMRI_DOWNLOAD_PERCENTAGE,

    ROUND(
        NVL(HES_FAILED * 100 / NULLIF(TOTAL_METERS, 0), 0),
        2
    ) AS HES_FAILED_PERCENTAGE,

    ROUND(
        NVL(
            (HES_DOWNLOAD + CMRI_DOWNLOAD)
            * 100 / NULLIF(TOTAL_METERS, 0),
            0
        ),
        2
    ) AS TOTAL_DOWNLOAD_PERCENTAGE

FROM FINAL_DATA";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add(
                        "READING_MONTH",
                        OracleDbType.Varchar2
                    ).Value = ReadingMonth;

                    cmd.Parameters.Add(
                        "IS_SUPERADMIN",
                        OracleDbType.Int32
                    ).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;

                    cmd.Parameters.Add(
                        "DEPARTMENT",
                        OracleDbType.Varchar2
                    ).Value =
                        scope == null || string.IsNullOrWhiteSpace(scope.Department)
                            ? string.Empty
                            : scope.Department.Trim().ToUpperInvariant();

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            Summary.totalMetersCount =
                                dr["TOTAL_METERS"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(dr["TOTAL_METERS"]);

                            Summary.hesDownloadCount =
                                dr["HES_DOWNLOAD"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(dr["HES_DOWNLOAD"]);

                            Summary.cmriDownloadCount =
                                dr["CMRI_DOWNLOAD"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(dr["CMRI_DOWNLOAD"]);

                            Summary.manualForwardinCount =
                                dr["HES_FAILED"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(dr["HES_FAILED"]);

                            Summary.hesDownloadPercentage =
                                dr["HES_DOWNLOAD_PERCENTAGE"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(dr["HES_DOWNLOAD_PERCENTAGE"]);

                            Summary.cmriDownloadPercentage =
                                dr["CMRI_DOWNLOAD_PERCENTAGE"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(dr["CMRI_DOWNLOAD_PERCENTAGE"]);

                            Summary.hesFailedPercentage =
                                dr["HES_FAILED_PERCENTAGE"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(dr["HES_FAILED_PERCENTAGE"]);

                            Summary.totalDownloadPercentage =
                                dr["TOTAL_DOWNLOAD_PERCENTAGE"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(dr["TOTAL_DOWNLOAD_PERCENTAGE"]);
                        }
                    }
                }
            }

            return Summary;
        }


        //Get Meter Download/Failed Summary Allied + Kimbal BYPL
        public List<MeterReceivedSummaryBypl> GetMeterReceivedSummaryBypl(string readingMonth, UserAccessScope scope)
        {
            List<MeterReceivedSummaryBypl> meterReceivedSummaryBypl = new List<MeterReceivedSummaryBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
            (
                SELECT TRIM( REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1, LEVEL))
                FROM DUAL CONNECT BY REGEXP_SUBSTR(:READING_MONTH,'[^,]+',1,LEVEL) IS NOT NULL
            ),
            SLCC_DATA AS
            (
                SELECT /*+ PARALLEL(S,8) */ TRIM(S.METERNO) AS METERNO, TRIM(S.BILL_MONTH) AS BILL_MONTH, S.IS_FAILED FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                WHERE S.BILL_MONTH IN
                (
                    SELECT M.READING_MONTH FROM MONTHS M
                )
                AND
                (
                    :IS_SUPERADMIN = 1
                    OR UPPER(
                        CASE
                            WHEN S.DEPARTMENT = 'MLCC' AND S.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN S.CYCLE IN ('KA', 'KC', 'KG')
                            THEN 'KCC'
                            WHEN S.DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE TRIM(S.DEPARTMENT)
                        END
                    ) = UPPER(:DEPARTMENT)
                )
            ),
            KCC_GCC_DATA AS
            (
                SELECT /*+ PARALLEL(K,8) */ TRIM(K.METERNO) AS METERNO, TRIM(K.BILL_MONTH) AS BILL_MONTH, K.IS_FAILED FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                WHERE K.BILL_MONTH IN
                (
                    SELECT M.READING_MONTH FROM MONTHS M
                )
                AND
                (
                    :IS_SUPERADMIN = 1
                    OR UPPER(
                        CASE
                            WHEN K.DEPARTMENT = 'MLCC' AND K.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN K.CYCLE IN ('KA', 'KC', 'KG')
                            THEN 'KCC'
                            WHEN K.DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE TRIM(K.DEPARTMENT)
                        END
                    ) = UPPER(:DEPARTMENT)
                )
            ),
            BASE_DATA AS
            (
                SELECT METERNO, BILL_MONTH, IS_FAILED FROM SLCC_DATA
                UNION ALL
                SELECT METERNO, BILL_MONTH, IS_FAILED FROM KCC_GCC_DATA
            ),
            DISTINCT_METER_STATUS AS
            (
                SELECT METERNO, BILL_MONTH, MAX(IS_FAILED) AS IS_FAILED FROM BASE_DATA
                WHERE METERNO IS NOT NULL GROUP BY METERNO, BILL_MONTH
            ),
            METER_STATUS AS
            (
                SELECT METERNO, MAX(IS_FAILED) AS IS_FAILED FROM DISTINCT_METER_STATUS GROUP BY METERNO
            ),
            SUMMARY AS
            (
                SELECT /*+ PARALLEL(8) */
                    COUNT(
                        DISTINCT CASE WHEN IS_FAILED = 0 THEN METERNO
                        END
                    ) AS HES_DOWNLOAD,
                    COUNT(
                        DISTINCT CASE  WHEN IS_FAILED = 1 THEN METERNO
                        END
                    ) AS HES_FAILED
                FROM METER_STATUS
            )
            SELECT HES_DOWNLOAD, HES_FAILED,
                (HES_DOWNLOAD + HES_FAILED) AS TOTAL,
                ROUND(
                    HES_DOWNLOAD * 100 /
                    NULLIF(
                        HES_DOWNLOAD + HES_FAILED,
                        0
                    ),
                    2
                ) AS DOWNLOAD_PERCENTAGE,
                ROUND(
                    HES_FAILED * 100 /
                    NULLIF(
                        HES_DOWNLOAD + HES_FAILED,
                        0
                    ),
                    2
                ) AS FAILED_PERCENTAGE                                                 
            FROM SUMMARY";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("READING_MONTH", OracleDbType.Varchar2).Value = readingMonth;
                    cmd.Parameters.Add("IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add("DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            meterReceivedSummaryBypl.Add(new MeterReceivedSummaryBypl()
                            {
                                totalMetersCount = dr["TOTAL"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL"]),

                                hesDownloadCount = dr["HES_DOWNLOAD"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_DOWNLOAD"]),

                                hesFailedCount = dr["HES_FAILED"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_FAILED"]),

                                hesDownloadPercentage = dr["DOWNLOAD_PERCENTAGE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DOWNLOAD_PERCENTAGE"]),

                                hesFailedPercentage = dr["FAILED_PERCENTAGE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FAILED_PERCENTAGE"])
                            });
                        }
                    }
                }
                return meterReceivedSummaryBypl;
            }
        }

        //BRPL Download Failed Meter Detailed Summary
        public List<MeterDownloadDetailedSummary> MeterDetailedSummary(string ReadingMonth, UserAccessScope scope)
        {
            List<MeterDownloadDetailedSummary> SummaryList = new List<MeterDownloadDetailedSummary>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */ TRIM( REGEXP_SUBSTR( :READING_MONTH,'[^,]+', 1,LEVEL)) AS READING_MONTH
                    FROM DUAL CONNECT BY REGEXP_SUBSTR(:READING_MONTH, '[^,]+',1, LEVEL) IS NOT NULL
                ),
                FAILURE_BASE AS
                (
                    SELECT L.METERNO, L.CONS_REF, L.MESSAGE, L.ENTRY_DATE, L.READING_MONTH, L.BILLMONTH, L.SAP_DEPARTMENT AS ORIGINAL_DEPARTMENT,  L.CYCLE,
                        CASE
                            WHEN L.SAP_DEPARTMENT = 'MLCC' AND L.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                            THEN 'KCC'
                            WHEN L.SAP_DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                        END AS DEPARTMENT
                    FROM RCMPA.SMART_METER_SCHEDULER_LOGS L
                    WHERE
                    (
                           L.METERNO LIKE '91______'
                        OR L.METERNO LIKE '90______'
                        OR L.METERNO LIKE 'AL________'
                        OR L.METERNO LIKE 'KI________'
                        OR L.METERNO LIKE 'RSE%'        
                    )
                    AND L.MESSAGE NOT LIKE 'Data%'
                ),
                FILTERED_FAILURES AS
                (
                    SELECT F.METERNO, F.CONS_REF,F.MESSAGE,F.ENTRY_DATE, F.READING_MONTH,F.BILLMONTH,F.ORIGINAL_DEPARTMENT, F.CYCLE, F.DEPARTMENT FROM FAILURE_BASE F WHERE
                    (
                        :IS_SUPERADMIN = 1 OR F.DEPARTMENT = UPPER(:DEPARTMENT)
                    )
                    AND
                    (
                        (
                            F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                            AND EXISTS
                            (
                                SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = F.BILLMONTH
                            )
                        )
                        OR
                        (
                            F.DEPARTMENT IN ('SLCC')
                            AND EXISTS
                            (
                                SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = F.READING_MONTH
                            )
                        )
                    )
                ),
                LATEST_FAILURE AS
                (
                    SELECT F.METERNO, F.CONS_REF, F.MESSAGE, F.ENTRY_DATE, F.READING_MONTH,  F.BILLMONTH,  F.ORIGINAL_DEPARTMENT,  F.DEPARTMENT,  F.CYCLE,
                        MIN(TRUNC(F.ENTRY_DATE)) OVER ( PARTITION BY F.METERNO,  F.READING_MONTH ) AS DOWNLOAD_FAILED_SINCE,
                        TRUNC(SYSDATE) - MIN(TRUNC(F.ENTRY_DATE)) OVER  ( PARTITION BY  F.METERNO, F.READING_MONTH) AS DOWNLOAD_FAILED_DAYS,
                        ROW_NUMBER() OVER (PARTITION BY F.METERNO, F.READING_MONTH  ORDER BY F.ENTRY_DATE DESC ) AS RN
                    FROM FILTERED_FAILURES F
                ),
                FAILURE_DATA AS
                (
                    SELECT L.METERNO, L.CONS_REF, L.MESSAGE, L.ENTRY_DATE,L.READING_MONTH, L.BILLMONTH,L.ORIGINAL_DEPARTMENT, L.DEPARTMENT,L.CYCLE, L.DOWNLOAD_FAILED_SINCE, L.DOWNLOAD_FAILED_DAYS,
                        CASE
                            WHEN UPPER(L.MESSAGE) LIKE '%SYSTEM TITLE%'
                            THEN 'System Title Mismatch'
                            WHEN UPPER(L.MESSAGE) LIKE '%TCP%'
                            THEN 'TCP Connection Failed'
                            WHEN UPPER(L.MESSAGE) LIKE '%NO DATA%'
                            THEN 'No Data Found'
                            ELSE 'Others Failure Reason'
                        END AS FAILURE_REASON
                    FROM LATEST_FAILURE L WHERE L.RN = 1
                    AND NOT EXISTS
                    (
                        SELECT 1 FROM RCMPA.SMART_METER_BILLING_DATA_SEP B WHERE B.METERNO = L.METERNO
                        AND
                        (
                            (
                                L.DEPARTMENT IN ('GCC', 'MLCC', 'KCC') AND B.BILLMONTH = L.BILLMONTH
                            )
                            OR
                            (
                                L.DEPARTMENT IN ('SLCC')
                                AND B.READING_MONTH = L.READING_MONTH
                            )
                        )
                    )
                    AND NOT EXISTS
                    (
                        SELECT 1 FROM RCMPA.SMART_METER_BILLING_DATA B WHERE B.METERNO = L.METERNO
                        AND
                        (
                            (
                                L.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')  AND B.BILLMONTH = L.BILLMONTH
                            )
                            OR
                            (
                                L.DEPARTMENT IN ('SLCC')  AND B.READING_MONTH = L.READING_MONTH
                            )
                        )
                    )
                ),
                FAILURE_KEYS AS
                (
                    SELECT DISTINCT CONS_REF, READING_MONTH FROM FAILURE_DATA WHERE CONS_REF IS NOT NULL
                ),
                SLCC_DATA AS
                (
                    SELECT CONS_REF, READING_MONTH, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK, FATHER_NAME
                    FROM
                    (
                        SELECT  S.CONS_REF, S.READING_MONTH,S.SAP_DIVISION, S.SAP_SEQ_NO, S.ADD1,  S.ADD2, S.ADD3,S.LAND_MARK, S.FATHER_NAME,
                        ROW_NUMBER() OVER  (PARTITION BY  S.CONS_REF, S.READING_MONTH ORDER BY S.ROWID) AS RN
                        FROM RCMPA.SAP_SLCC_FORMY S INNER JOIN FAILURE_KEYS K ON K.CONS_REF = S.CONS_REF AND K.READING_MONTH = S.READING_MONTH
                    ) WHERE RN = 1
                ),
                FORMY_DATA AS
                (
                    SELECT CONS_REF, READING_MONTH, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK, FATHER_NAME FROM
                    (
                        SELECT FM.CONS_REF, FM.READING_MONTH, FM.SAP_DIVISION, FM.SAP_SEQ_NO, FM.ADD1, FM.ADD2, FM.ADD3,FM.LAND_MARK, FM.FATHER_NAME,
                        ROW_NUMBER() OVER( PARTITION BY FM.CONS_REF, FM.READING_MONTH ORDER BY  FM.ROWID ) AS RN
                        FROM RCMPA.SAP_FORMY FM
                        INNER JOIN FAILURE_KEYS K ON K.CONS_REF = FM.CONS_REF  AND K.READING_MONTH = FM.READING_MONTH
                    ) WHERE RN = 1
                )
                SELECT  F.METERNO, F.CONS_REF, F.DEPARTMENT AS SAP_DEPARTMENT, F.CYCLE,
                 NVL( S.SAP_DIVISION, FM.SAP_DIVISION) AS SAP_DIVISION,
                 NVL(S.SAP_SEQ_NO, FM.SAP_SEQ_NO) AS SAP_SEQ_NO,
                 RTRIM(
                        NVL(S.ADD1, FM.ADD1) || ', ' ||
                        NVL(S.ADD2, FM.ADD2) || ', ' ||
                        NVL(S.ADD3, FM.ADD3) || ', ' ||
                        NVL(S.LAND_MARK, FM.LAND_MARK) || ', ' ||
                        NVL(S.FATHER_NAME, FM.FATHER_NAME),
                        ', '
                    ) AS ADDRESS,
                    CASE
                        WHEN SUBSTR(F.METERNO, 1, 2) IN ('90', 'AL')
                        THEN 'ALLIED'
                        WHEN SUBSTR(F.METERNO, 1, 2) IN ('91', 'KI')
                        THEN 'KIMBAL'
                        WHEN F.METERNO LIKE 'RSE%'
                        THEN 'SECURE'
                        ELSE 'UNKNOWN'
                    END AS METER_TYPE,
                    CASE
                        WHEN SUBSTR(F.METERNO, 1, 4) IN ('AL91', 'KI91', '9150', '9008', '9027')
                        THEN '1PH'
                        WHEN SUBSTR(F.METERNO, 1, 4) IN ('AL90', 'KI90', '9026','RSE7')
                        THEN '3PH'
                        ELSE 'UNKNOWN'
                    END AS PHASE_TYPE,
                    F.FAILURE_REASON, F.MESSAGE AS SCHEDULER_MESSAGE, F.ENTRY_DATE,  F.READING_MONTH, F.DOWNLOAD_FAILED_SINCE, F.DOWNLOAD_FAILED_DAYS FROM FAILURE_DATA F
                    LEFT JOIN SLCC_DATA S ON S.CONS_REF = F.CONS_REF AND S.READING_MONTH = F.READING_MONTH 
                    LEFT JOIN FORMY_DATA FM ON FM.CONS_REF = F.CONS_REF AND FM.READING_MONTH = F.READING_MONTH
                    ORDER BY  F.DEPARTMENT,  NVL(S.SAP_DIVISION, FM.SAP_DIVISION),  NVL(S.SAP_SEQ_NO, FM.SAP_SEQ_NO), F.METERNO";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    cmd.Parameters.Add("IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add("DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {

                        while (dr.Read())
                        {
                            SummaryList.Add(new MeterDownloadDetailedSummary
                            {
                                MeterNumber = dr["METERNO"].ToString(),

                                SapDepartment = dr["SAP_DEPARTMENT"].ToString(),

                                Cycle = dr["CYCLE"].ToString(),

                                MeterType = dr["METER_TYPE"].ToString(),

                                Phase = dr["PHASE_TYPE"].ToString(),

                                ConsRef = dr["CONS_REF"].ToString(),

                                SapDivision = dr["SAP_DIVISION"].ToString(),

                                Address = dr["ADDRESS"].ToString(),

                                SapSeqNo = dr["SAP_SEQ_NO"].ToString(),

                                SchedulerMessage = dr["SCHEDULER_MESSAGE"] == DBNull.Value ? null : dr["SCHEDULER_MESSAGE"].ToString(),

                                EntryDate = dr["ENTRY_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["ENTRY_DATE"]),

                                DownloadFailedSince = dr["DOWNLOAD_FAILED_SINCE"] == DBNull.Value ? null : Convert.ToDateTime(dr["DOWNLOAD_FAILED_SINCE"]),

                                DownloadFailedDays = dr["DOWNLOAD_FAILED_DAYS"] == DBNull.Value ? null : Convert.ToInt32(dr["DOWNLOAD_FAILED_DAYS"])
                            });
                        }
                    }
                }
                return SummaryList;
            }
        }

        //BYPL Download Failed Meter Detailed Summary
        public List<MeterDownloadDetailedSummaryBypl> GetMeterDownloadDetailedSummaryBypl(string readingMonth, UserAccessScope scope)
        {
            List<MeterDownloadDetailedSummaryBypl> summaryList = new List<MeterDownloadDetailedSummaryBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */ TRIM(REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL)) AS READING_MONTH
                    FROM DUAL CONNECT BY REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL) IS NOT NULL
                ),
                SLCC_FAILED AS
                (
                    SELECT METERNO, READING_MONTH, INSERTED_DATE, REMARKS, NVL(DEPARTMENT, 'SLCC') AS DEPARTMENT, DOWNLOAD_FAILED_SINCE,
                    TRUNC(SYSDATE) - TRUNC(DOWNLOAD_FAILED_SINCE) AS DOWNLOAD_FAILED_DAYS
                    FROM
                    (
                        SELECT /*+ LEADING(M S) USE_NL(S) INDEX(S IDX_SLCC_SM_LOG_FAIL) */ TRIM(S.METERNO) AS METERNO, S.BILL_MONTH AS READING_MONTH,
                            S.INSERTED_DATE, S.REMARKS, S.DEPARTMENT,
                            MIN(TRUNC(S.INSERTED_DATE)) OVER ( PARTITION BY TRIM(S.METERNO), S.BILL_MONTH) AS DOWNLOAD_FAILED_SINCE,
                            ROW_NUMBER() OVER ( PARTITION BY TRIM(S.METERNO), S.BILL_MONTH ORDER BY S.INSERTED_DATE DESC NULLS LAST ) AS RN
                        FROM MONTHS M JOIN RCMPA.SAP_SLCC_SMARTMETER_LOG S ON S.BILL_MONTH = M.READING_MONTH WHERE S.IS_FAILED = '1' AND S.METERNO IS NOT NULL
                          AND
                          (
                              :IS_SUPERADMIN = 1
                              OR
                              UPPER(
                                  CASE
                                      WHEN S.DEPARTMENT = 'MLCC' AND S.CYCLE = '0N'
                                      THEN 'KCC'
                                      WHEN S.CYCLE IN ('KA', 'KC', 'KG')
                                      THEN 'KCC'
                                      WHEN S.DEPARTMENT IS NULL
                                      THEN 'SLCC'
                                      ELSE TRIM(S.DEPARTMENT)
                                  END
                              ) = UPPER(:DEPARTMENT)
                          )
                    )
                    WHERE RN = 1
                ),
                KCC_FAILED AS
                (
                    SELECT METERNO, READING_MONTH, INSERTED_DATE, REMARKS, DEPARTMENT, DOWNLOAD_FAILED_SINCE,
                     TRUNC(SYSDATE) - TRUNC(DOWNLOAD_FAILED_SINCE) AS DOWNLOAD_FAILED_DAYS
                    FROM
                    (
                        SELECT /*+ LEADING(M K) USE_NL(K) INDEX(K IDX_KCC_SM_LOG_FAIL) */ TRIM(K.METERNO) AS METERNO,
                        K.BILL_MONTH AS READING_MONTH, K.INSERTED_DATE, K.REMARKS, K.DEPARTMENT,
        
                        MIN(TRUNC(K.INSERTED_DATE)) OVER ( PARTITION BY TRIM(K.METERNO), K.BILL_MONTH ) AS DOWNLOAD_FAILED_SINCE,
                        ROW_NUMBER() OVER (PARTITION BY TRIM(K.METERNO), K.BILL_MONTH  ORDER BY K.INSERTED_DATE DESC NULLS LAST) AS RN
                        FROM MONTHS M JOIN RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K ON K.BILL_MONTH = M.READING_MONTH
                        WHERE K.IS_FAILED = '1' AND K.METERNO IS NOT NULL
                          AND
                          (
                              :IS_SUPERADMIN = 1
                              OR
                              UPPER(
                                  CASE
                                      WHEN K.DEPARTMENT = 'MLCC'  AND K.CYCLE = '0N'
                                      THEN 'KCC'
                                      WHEN K.CYCLE IN ('KA', 'KC', 'KG')
                                      THEN 'KCC'
                                      WHEN K.DEPARTMENT IS NULL
                                      THEN 'SLCC'
                                      ELSE TRIM(K.DEPARTMENT)
                                  END
                              ) = UPPER(:DEPARTMENT)
                          )
                    )
                    WHERE RN = 1
                ),
                SLCC_METERS AS
                (
                    SELECT /*+ MATERIALIZE */  DISTINCT METERNO FROM SLCC_FAILED
                ),
                KCC_METERS AS
                (
                    SELECT /*+ MATERIALIZE */ DISTINCT METERNO FROM KCC_FAILED
                ),
                SLCC_FORMY_FALLBACK AS
                (
                    SELECT METERNO, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK, FATHER_NAME
                    FROM
                    (
                        SELECT /*+ LEADING(M F) USE_NL(F) INDEX(F IDX_SLCC_FORMY_MTR_MONTH) */  F.METERNO,
                            F.CONS_REF, F.SAP_DIVISION,F.SAP_SEQ_NO, F.ADD1, F.ADD2, F.ADD3, F.LAND_MARK, F.FATHER_NAME,
                            ROW_NUMBER() OVER (PARTITION BY F.METERNO ORDER BY F.READING_MONTH DESC ) AS RN FROM SLCC_METERS M JOIN RCMPA.SAP_SLCC_FORMY F
                            ON F.METERNO = M.METERNO
                    )
                    WHERE RN = 1
                ),
                SAP_FORMY_FALLBACK AS
                (
                    SELECT  METERNO, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK,FATHER_NAME
                    FROM
                    (
                        SELECT /*+ LEADING(M F) USE_NL(F) INDEX(F IDX_SAP_FORMY_MTR_MONTH) */  F.METERNO,
                            F.CONS_REF, F.SAP_DIVISION, F.SAP_SEQ_NO, F.ADD1, F.ADD2, F.ADD3, F.LAND_MARK,F.FATHER_NAME,
                            ROW_NUMBER() OVER (PARTITION BY F.METERNO ORDER BY F.READING_MONTH DESC ) AS RN FROM KCC_METERS M JOIN RCMPA.SAP_FORMY F
                            ON F.METERNO = M.METERNO
                    )
                    WHERE RN = 1
                ),
                SLCC_DATA AS
                (
                    SELECT D.METERNO, D.READING_MONTH, D.DEPARTMENT,  D.REMARKS, D.INSERTED_DATE, D.DOWNLOAD_FAILED_SINCE, D.DOWNLOAD_FAILED_DAYS,
                        COALESCE(
                            NULLIF(TRIM(FM.CONS_REF), ''),
                            NULLIF(TRIM(FL.CONS_REF), '')
                        ) AS CONS_REF,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_DIVISION), ''),
                            NULLIF(TRIM(FL.SAP_DIVISION), '')
                        ) AS SAP_DIVISION,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_SEQ_NO), ''),
                            NULLIF(TRIM(FL.SAP_SEQ_NO), '')
                        ) AS SAP_SEQ_NO,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD1), ''),
                            NULLIF(TRIM(FL.ADD1), '')
                        ) AS ADD1,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD2), ''),
                            NULLIF(TRIM(FL.ADD2), '')
                        ) AS ADD2,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD3), ''),
                            NULLIF(TRIM(FL.ADD3), '')
                        ) AS ADD3,
                        COALESCE(
                            NULLIF(TRIM(FM.LAND_MARK), ''),
                            NULLIF(TRIM(FL.LAND_MARK), '')
                        ) AS LAND_MARK,
                        COALESCE(
                            NULLIF(TRIM(FM.FATHER_NAME), ''),
                            NULLIF(TRIM(FL.FATHER_NAME), '')
                        ) AS FATHER_NAME FROM SLCC_FAILED D LEFT JOIN RCMPA.SAP_SLCC_FORMY FM  ON FM.METERNO = D.METERNO AND FM.READING_MONTH = D.READING_MONTH 
                        LEFT JOIN SLCC_FORMY_FALLBACK FL  ON FL.METERNO = D.METERNO
                ),
                KCC_DATA AS
                (
                    SELECT D.METERNO, D.READING_MONTH, D.DEPARTMENT, D.REMARKS,  D.INSERTED_DATE, D.DOWNLOAD_FAILED_SINCE, D.DOWNLOAD_FAILED_DAYS,
                        COALESCE(
                            NULLIF(TRIM(FM.CONS_REF), ''),
                            NULLIF(TRIM(FL.CONS_REF), '')
                        ) AS CONS_REF,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_DIVISION), ''),
                            NULLIF(TRIM(FL.SAP_DIVISION), '')
                        ) AS SAP_DIVISION,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_SEQ_NO), ''),
                            NULLIF(TRIM(FL.SAP_SEQ_NO), '')
                        ) AS SAP_SEQ_NO,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD1), ''),
                            NULLIF(TRIM(FL.ADD1), '')
                        ) AS ADD1,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD2), ''),
                            NULLIF(TRIM(FL.ADD2), '')
                        ) AS ADD2,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD3), ''),
                            NULLIF(TRIM(FL.ADD3), '')
                        ) AS ADD3,
                        COALESCE(
                            NULLIF(TRIM(FM.LAND_MARK), ''),
                            NULLIF(TRIM(FL.LAND_MARK), '')
                        ) AS LAND_MARK,
                        COALESCE(
                            NULLIF(TRIM(FM.FATHER_NAME), ''),
                            NULLIF(TRIM(FL.FATHER_NAME), '')
                        ) AS FATHER_NAME FROM KCC_FAILED D LEFT JOIN RCMPA.SAP_FORMY FM ON FM.METERNO = D.METERNO AND FM.READING_MONTH = D.READING_MONTH LEFT JOIN SAP_FORMY_FALLBACK FL
                         ON FL.METERNO = D.METERNO
                ),
                BASE_DATA AS
                (
                    SELECT METERNO, READING_MONTH, DEPARTMENT, REMARKS, INSERTED_DATE, DOWNLOAD_FAILED_SINCE, DOWNLOAD_FAILED_DAYS, CONS_REF,SAP_DIVISION, SAP_SEQ_NO,
                    ADD1, ADD2,  ADD3, LAND_MARK,FATHER_NAME, 1 AS SOURCE_PRIORITY FROM SLCC_DATA
                    UNION ALL
                    SELECT  METERNO,READING_MONTH,DEPARTMENT, REMARKS, INSERTED_DATE, DOWNLOAD_FAILED_SINCE, DOWNLOAD_FAILED_DAYS,CONS_REF, SAP_DIVISION, SAP_SEQ_NO,
                    ADD1,ADD2, ADD3, LAND_MARK, FATHER_NAME, 2 AS SOURCE_PRIORITY FROM KCC_DATA
                ),
                DISTINCT_DATA AS
                (
                    SELECT METERNO, READING_MONTH, DEPARTMENT, REMARKS,INSERTED_DATE, DOWNLOAD_FAILED_SINCE,DOWNLOAD_FAILED_DAYS, CONS_REF, SAP_DIVISION, SAP_SEQ_NO,
                    ADD1,ADD2, ADD3, LAND_MARK,FATHER_NAME FROM
                    (
                        SELECT
                            B.*,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY B.METERNO ORDER BY B.INSERTED_DATE DESC NULLS LAST,B.SOURCE_PRIORITY DESC
                            ) AS RN
                        FROM BASE_DATA B WHERE B.METERNO IS NOT NULL
                    ) WHERE RN = 1
                )
                SELECT D.METERNO, D.CONS_REF,
                    CASE
                        WHEN D.METERNO LIKE '92%'
                          OR D.METERNO LIKE '99%'
                          OR D.METERNO LIKE 'AL92%'
                          OR D.METERNO LIKE 'AL99%'
                        THEN '1PH'
                        WHEN D.METERNO LIKE 'AL97%'
                        THEN '3PH'
                        WHEN D.METERNO LIKE '98%'
                          OR D.METERNO LIKE 'KI98%'
                        THEN '1PH'
                        WHEN D.METERNO LIKE '97%'
                          OR D.METERNO LIKE 'KI97%'
                        THEN '3PH'
                        ELSE 'UNKNOWN'
                    END AS PHASE_TYPE,
                    CASE
                        WHEN D.DEPARTMENT = 'MLCC' AND D.METERNO IS NOT NULL
                        THEN 'KCC'
                        WHEN D.DEPARTMENT IS NULL
                        THEN 'SLCC'
                        ELSE D.DEPARTMENT
                    END AS SAP_DEPARTMENT,
                    D.SAP_DIVISION,  D.SAP_SEQ_NO,
                    RTRIM(
                        D.ADD1 || ', ' ||
                        D.ADD2 || ', ' ||
                        D.ADD3 || ', ' ||
                        D.LAND_MARK || ', ' ||
                        D.FATHER_NAME,
                        ', '
                    ) AS ADDRESS,
                    CASE
                        WHEN D.METERNO LIKE '92%'
                          OR D.METERNO LIKE '99%'
                          OR D.METERNO LIKE 'AL%'
                        THEN 'ALLIED'
                        WHEN D.METERNO LIKE 'KI%'
                          OR D.METERNO LIKE '97%'
                          OR D.METERNO LIKE '98%'
                        THEN 'KIMBAL'
                        ELSE 'UNKNOWN'
                    END AS METER_TYPE, D.REMARKS AS FAILURE_REASON, D.INSERTED_DATE, D.DOWNLOAD_FAILED_SINCE, D.DOWNLOAD_FAILED_DAYS FROM DISTINCT_DATA D
                    ORDER BY D.INSERTED_DATE DESC NULLS LAST, D.METERNO";
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add("READING_MONTH", OracleDbType.Varchar2).Value = readingMonth;
                    cmd.Parameters.Add("DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            summaryList.Add(new MeterDownloadDetailedSummaryBypl
                            {
                                MeterNumber = dr["METERNO"].ToString(),

                                SapDepartment = dr["SAP_DEPARTMENT"].ToString(),

                                MeterType = dr["METER_TYPE"].ToString(),

                                Phase = dr["PHASE_TYPE"].ToString(),

                                ConsRef = dr["CONS_REF"].ToString(),

                                SapDivision = dr["SAP_DIVISION"].ToString(),

                                Address = dr["ADDRESS"].ToString(),

                                SapSeqNo = dr["SAP_SEQ_NO"].ToString(),

                                SchedulerMessage = dr["FAILURE_REASON"] == DBNull.Value ? null : dr["FAILURE_REASON"].ToString(),

                                EntryDate = dr["INSERTED_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["INSERTED_DATE"]),

                                DownloadFailedDays = dr["DOWNLOAD_FAILED_DAYS"] == DBNull.Value ? null : Convert.ToInt32(dr["DOWNLOAD_FAILED_DAYS"]),

                                DownloadFailedSince = dr["DOWNLOAD_FAILED_SINCE"] == DBNull.Value ? null : Convert.ToDateTime(dr["DOWNLOAD_FAILED_SINCE"])
                            });
                        }
                    }
                    return summaryList;
                }
            }
        }
        //Get Reading Trend BRPL 
        public List<ReadingTrendDateWise> GetReadingTrend(string ReadingMonth, UserAccessScope scope)
        {
            List<ReadingTrendDateWise> ReadingList = new List<ReadingTrendDateWise>();
            using (OracleConnection con = _db.GetConnection())
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
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
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

        //Get Department Wise Download/Failed Summary BRPL
        public List<DepartmentWiseSummary> GetDepartmentSummary(string ReadingMonth, UserAccessScope scope)
        {
            List<DepartmentWiseSummary> departmentWiseData = new List<DepartmentWiseSummary>();
            using (OracleConnection conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */ TRIM(REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL)) AS READING_MONTH
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL) IS NOT NULL
                ),
                DOWNLOAD_BASE AS
                (
                    SELECT TRIM(B.METERNO) AS METERNO, B.READING_MONTH, B.BILLMONTH,
                        CASE
                            WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N'  THEN 'KCC'
                            WHEN B.CYCLE IN ('KA', 'KC', 'KG')  THEN 'KCC'
                            WHEN B.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                            ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                        END AS DEPARTMENT FROM RCMPA.SMART_METER_BILLING_DATA_SEP B
                    WHERE B.MACHINE_ID IS NULL
                      AND
                      (
                             B.METERNO LIKE '91______'
                          OR B.METERNO LIKE '90______'
                          OR B.METERNO LIKE 'AL________'
                          OR B.METERNO LIKE 'KI________'
                          OR B.METERNO LIKE 'RSE%'
                      )
                    UNION ALL
                    SELECT TRIM(B.METERNO) AS METERNO, B.READING_MONTH, B.BILLMONTH,
                        CASE
                            WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N' THEN 'KCC'
                            WHEN B.CYCLE IN ('KA', 'KC', 'KG') THEN 'KCC'
                            WHEN B.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                            ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                        END AS DEPARTMENT FROM RCMPA.SMART_METER_BILLING_DATA B
                    WHERE B.MACHINE_ID IS NULL
                      AND
                      (
                             B.METERNO LIKE '91______'
                          OR B.METERNO LIKE '90______'
                          OR B.METERNO LIKE 'AL________'
                          OR B.METERNO LIKE 'KI________'
                          OR B.METERNO LIKE 'RSE%'
                      )
                ),
                DOWNLOAD_FILTERED AS
                (
                    SELECT D.METERNO, D.DEPARTMENT FROM DOWNLOAD_BASE D WHERE
                        (
                            :IS_SUPERADMIN = 1  OR D.DEPARTMENT = UPPER(TRIM(:DEPARTMENT))
                        )
                        AND
                        (
                            (
                                D.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                                AND EXISTS
                                (
                                    SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = D.BILLMONTH
                                )
                            )
                            OR
                            (
                                D.DEPARTMENT = 'SLCC'
                                AND EXISTS
                                (
                                    SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = D.READING_MONTH
                                )
                            )
                        )
                ),
                DOWNLOAD AS
                (
                    SELECT D.DEPARTMENT,COUNT(DISTINCT D.METERNO) AS HES_DOWNLOAD FROM DOWNLOAD_FILTERED D GROUP BY D.DEPARTMENT
                ),
                CMRI_BASE AS
                (
                    SELECT TRIM(B.METERNO) AS METERNO, B.READING_MONTH, B.BILLMONTH,
                        CASE
                            WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N' THEN 'KCC'
                            WHEN B.CYCLE IN ('KA', 'KC', 'KG') THEN 'KCC'
                            WHEN B.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                            ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                        END AS DEPARTMENT FROM RCMPA.SMART_METER_BILLING_DATA_SEP B
                    WHERE B.MACHINE_ID IS NOT NULL
                      AND
                      (
                             B.METERNO LIKE '91______'
                          OR B.METERNO LIKE '90______'
                          OR B.METERNO LIKE 'AL________'
                          OR B.METERNO LIKE 'KI________'
                          OR B.METERNO LIKE 'RSE%'
                      )
                    UNION ALL
                    SELECT TRIM(B.METERNO) AS METERNO, B.READING_MONTH, B.BILLMONTH,
                        CASE
                            WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N' THEN 'KCC'
                            WHEN B.CYCLE IN ('KA', 'KC', 'KG') THEN 'KCC'
                            WHEN B.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                            ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                        END AS DEPARTMENT FROM RCMPA.SMART_METER_BILLING_DATA B
                    WHERE B.MACHINE_ID IS NOT NULL
                      AND
                      (
                             B.METERNO LIKE '91______'
                          OR B.METERNO LIKE '90______'
                          OR B.METERNO LIKE 'AL________'
                          OR B.METERNO LIKE 'KI________'
                          OR B.METERNO LIKE 'RSE%'
                      )
                ),
                CMRI_FILTERED AS
                (
                    SELECT  C.METERNO, C.DEPARTMENT FROM CMRI_BASE C
                    WHERE
                        (
                            :IS_SUPERADMIN = 1 OR C.DEPARTMENT = UPPER(TRIM(:DEPARTMENT))
                        )
                        AND
                        (
                            (
                                C.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                                AND EXISTS
                                (
                                    SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = C.BILLMONTH
                                )
                            )
                            OR
                            (
                                C.DEPARTMENT = 'SLCC'
                                AND EXISTS
                                (
                                    SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = C.READING_MONTH
                                )
                            )
                        )
                ),
                CMRI AS
                (
                    SELECT  C.DEPARTMENT, COUNT(DISTINCT C.METERNO) AS CMRI_DOWNLOAD FROM CMRI_FILTERED C GROUP BY C.DEPARTMENT
                ),
                FAILED_BASE AS
                (
                    SELECT TRIM(L.METERNO) AS METERNO, L.READING_MONTH, L.BILLMONTH,
                        CASE
                            WHEN L.SAP_DEPARTMENT = 'MLCC' AND L.CYCLE = '0N'  THEN 'KCC'
                            WHEN L.CYCLE IN ('KA', 'KC', 'KG') THEN 'KCC'
                            WHEN L.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                            ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                        END AS DEPARTMENT FROM RCMPA.SMART_METER_SCHEDULER_LOGS L
                    WHERE
                        (
                               L.METERNO LIKE '91______'
                            OR L.METERNO LIKE '90______'
                            OR L.METERNO LIKE 'AL________'
                            OR L.METERNO LIKE 'KI________'
                            OR L.METERNO LIKE 'RSE%'
                        )
                        AND L.MESSAGE NOT LIKE 'Data%'
                ),
                FAILED_FILTERED AS
                (
                    SELECT F.METERNO, F.READING_MONTH, F.BILLMONTH, F.DEPARTMENT FROM FAILED_BASE F
                    WHERE
                        (
                            :IS_SUPERADMIN = 1 OR F.DEPARTMENT = UPPER(TRIM(:DEPARTMENT))
                        )
                        AND
                        (
                            (
                                F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')
                                AND EXISTS
                                (
                                    SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = F.BILLMONTH
                                )
                            )
                            OR
                            (
                                F.DEPARTMENT = 'SLCC'
                                AND EXISTS
                                (
                                    SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = F.READING_MONTH
                                )
                            )
                        )
                ),
                FAILED_METERS AS
                (
                    SELECT DISTINCT F.METERNO, F.DEPARTMENT FROM FAILED_FILTERED F
                    WHERE NOT EXISTS
                    (
                        SELECT 1 FROM RCMPA.SMART_METER_BILLING_DATA_SEP B WHERE B.METERNO = F.METERNO
                          AND
                          (
                              (
                                  F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC')  AND B.BILLMONTH = F.BILLMONTH
                              )
                              OR
                              (
                                  F.DEPARTMENT = 'SLCC' AND B.READING_MONTH = F.READING_MONTH
                              )
                          )
                    )
                    AND NOT EXISTS
                    (
                        SELECT 1 FROM RCMPA.SMART_METER_BILLING_DATA B WHERE B.METERNO = F.METERNO
                          AND
                          (
                              (
                                  F.DEPARTMENT IN ('GCC', 'MLCC', 'KCC') AND B.BILLMONTH = F.BILLMONTH
                              )
                              OR
                              (
                                  F.DEPARTMENT = 'SLCC' AND B.READING_MONTH = F.READING_MONTH
                              )
                          )
                    )
                ),
                FAILED AS
                (
                    SELECT  F.DEPARTMENT, COUNT(DISTINCT F.METERNO) AS FAILED FROM FAILED_METERS F GROUP BY F.DEPARTMENT
                )
                SELECT COALESCE(D.DEPARTMENT, C.DEPARTMENT, F.DEPARTMENT) AS DEPARTMENT,  NVL(D.HES_DOWNLOAD, 0) AS HESDOWNLOAD,
                    NVL(C.CMRI_DOWNLOAD, 0) AS CMRIDOWNLOAD,  NVL(F.FAILED, 0) AS FAILED FROM DOWNLOAD D FULL OUTER JOIN CMRI C
                    ON D.DEPARTMENT = C.DEPARTMENT FULL OUTER JOIN FAILED F ON COALESCE(D.DEPARTMENT, C.DEPARTMENT) = F.DEPARTMENT
                ORDER BY COALESCE(D.DEPARTMENT, C.DEPARTMENT, F.DEPARTMENT)";
                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(":IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add(":DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

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

                                CmriDownload = Convert.ToInt32(dr["CMRIDOWNLOAD"])
                            });
                        }
                    }
                }
            }
            return departmentWiseData;
        }

        //BYPL Download/Failed Meter Department Wise Summary
        public List<DepartmentWiseSummaryBypl> GetDepartmentWiseSummaryBypl(string ReadingMonth, UserAccessScope scope)
        {
            List<DepartmentWiseSummaryBypl> departmentWiseSummarieBypl = new List<DepartmentWiseSummaryBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                (
                    SELECT TRIM( REGEXP_SUBSTR(:READING_MONTH,'[^,]+', 1,LEVEL ))
                    FROM DUAL CONNECT BY REGEXP_SUBSTR( :READING_MONTH,'[^,]+', 1,LEVEL ) IS NOT NULL
                ),
                SLCC_DATA AS
                (
                    SELECT /*+ PARALLEL(S,8) */ TRIM(S.METERNO) AS METERNO, TRIM(S.BILL_MONTH) AS BILL_MONTH, S.IS_FAILED, NVL(TRIM(S.DEPARTMENT), 'SLCC') AS DEPARTMENT
                    FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                    WHERE S.BILL_MONTH IN
                    (
                        SELECT M.READING_MONTH FROM MONTHS M
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER(NVL(TRIM(S.DEPARTMENT), 'SLCC') ) = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                KCC_GCC_DATA AS
                (
                    SELECT /*+ PARALLEL(K,8) */ TRIM(K.METERNO) AS METERNO, TRIM(K.BILL_MONTH) AS BILL_MONTH,
                    K.IS_FAILED, NVL(TRIM(K.DEPARTMENT), 'KCC/GCC') AS DEPARTMENT
                    FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K WHERE K.BILL_MONTH IN
                    (
                        SELECT M.READING_MONTH FROM MONTHS M
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER( NVL(TRIM(K.DEPARTMENT), 'KCC/GCC')) = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                BASE_DATA AS
                (
                    SELECT METERNO,BILL_MONTH,IS_FAILED,DEPARTMENT, 1 AS SOURCE_PRIORITY FROM SLCC_DATA
                    UNION ALL
                    SELECT METERNO, BILL_MONTH,IS_FAILED, DEPARTMENT, 2 AS SOURCE_PRIORITY FROM KCC_GCC_DATA
                ),
                DISTINCT_DATA AS
                (
                    SELECT METERNO,BILL_MONTH,IS_FAILED, DEPARTMENT
                    FROM
                    (
                        SELECT
                            B.*,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY B.METERNO, B.BILL_MONTH, UPPER(TRIM(B.DEPARTMENT))
                                ORDER BY  B.IS_FAILED DESC,B.SOURCE_PRIORITY DESC
                            ) AS RN
                        FROM BASE_DATA B WHERE B.METERNO IS NOT NULL
                    )
                    WHERE RN = 1
                ),
                SUMMARY AS
                (
                    SELECT
                        DEPARTMENT,
                        COUNT(
                            DISTINCT CASE WHEN IS_FAILED = 0
                                THEN METERNO
                            END
                        ) AS HES_DOWNLOAD,
                        COUNT(
                            DISTINCT CASE WHEN IS_FAILED = 1
                                THEN METERNO
                            END
                        ) AS HES_FAILED
                    FROM DISTINCT_DATA GROUP BY DEPARTMENT
                )
                SELECT
                    DEPARTMENT, HES_DOWNLOAD, HES_FAILED,(HES_DOWNLOAD + HES_FAILED) AS TOTAL,
                    ROUND(
                        HES_DOWNLOAD * 100 /
                        NULLIF(
                            HES_DOWNLOAD + HES_FAILED,
                            0
                        ),
                        2
                    ) AS DOWNLOAD_PERCENTAGE,
                    ROUND(
                        HES_FAILED * 100 /
                        NULLIF(
                            HES_DOWNLOAD + HES_FAILED,
                            0
                        ),
                        2
                    ) AS FAILED_PERCENTAGE
                FROM SUMMARY ORDER BY DEPARTMENT";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(":IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add(":DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            departmentWiseSummarieBypl.Add(new DepartmentWiseSummaryBypl
                            {
                                Department = dr["DEPARTMENT"].ToString(),

                                HesDownload = dr["HES_DOWNLOAD"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_DOWNLOAD"]),

                                Failed = dr["HES_FAILED"] == DBNull.Value ? 0 : Convert.ToInt32(dr["HES_FAILED"]),

                            });
                        }
                    }
                }
                return departmentWiseSummarieBypl;
            }
        }

        //Failure Reason Counts BRPL
        public List<FailureReasonCount> FailureReasonCounts(string ReadingMonth, UserAccessScope scope)
        {
            List<FailureReasonCount> failureReasonCounts = new List<FailureReasonCount>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */  TRIM( REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1,  LEVEL )  ) AS READING_MONTH
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1,  LEVEL ) IS NOT NULL
                ),
                BILLING_DATA AS
                (
                    SELECT  METERNO, READING_MONTH, BILLMONTH FROM RCMPA.SMART_METER_BILLING_DATA_SEP
                    UNION ALL
                    SELECT METERNO, READING_MONTH,BILLMONTH FROM RCMPA.SMART_METER_BILLING_DATA
                ),
                LATEST_FAILURE AS
                (
                    SELECT L.METERNO, L.CONS_REF,  L.MESSAGE, L.ENTRY_DATE, L.READING_MONTH, L.BILLMONTH,
                        CASE
                            WHEN L.SAP_DEPARTMENT = 'MLCC'  AND L.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                            THEN 'KCC'
                            WHEN L.SAP_DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                        END AS DEPARTMENT,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY L.METERNO, L.READING_MONTH ORDER BY  L.ENTRY_DATE DESC
                        ) AS RN
                    FROM RCMPA.SMART_METER_SCHEDULER_LOGS L
                    WHERE
                    (
                           L.METERNO LIKE '91______'
                        OR L.METERNO LIKE '90______'
                        OR L.METERNO LIKE 'AL________'
                        OR L.METERNO LIKE 'KI________'
                        OR L.METERNO LIKE 'RSE%'
                    )
                    AND L.MESSAGE NOT LIKE 'Data%'
                    AND
                    (
                        (
                            CASE
                                WHEN L.SAP_DEPARTMENT = 'MLCC'  AND L.CYCLE = '0N'
                                THEN 'KCC'
                                WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                                THEN 'KCC'
                                WHEN L.SAP_DEPARTMENT IS NULL
                                THEN 'SLCC'
                                ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                            END IN ('GCC', 'MLCC', 'KCC')
                            AND EXISTS
                            (
                                SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = L.BILLMONTH
                            )
                        )
                        OR
                        (
                            CASE
                                WHEN L.SAP_DEPARTMENT = 'MLCC' AND L.CYCLE = '0N'
                                THEN 'KCC'
                                WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                                THEN 'KCC'
                                WHEN L.SAP_DEPARTMENT IS NULL
                                THEN 'SLCC'
                                ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                            END IN ('SLCC')
                            AND EXISTS
                            (
                                SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = L.READING_MONTH
                            )
                        )
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR
                        CASE
                            WHEN L.SAP_DEPARTMENT = 'MLCC' AND L.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                            THEN 'KCC'
                            WHEN L.SAP_DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE UPPER(TRIM(L.SAP_DEPARTMENT))
                        END = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                FAILURE_DATA AS
                (
                    SELECT L.METERNO, L.READING_MONTH, L.BILLMONTH, L.DEPARTMENT,
                        CASE
                            WHEN UPPER(L.MESSAGE) LIKE '%SYSTEM TITLE%'
                            THEN 'System Title Mismatch'
                            WHEN UPPER(L.MESSAGE) LIKE '%TCP%'
                            THEN 'TCP Connection Failed'
                            WHEN UPPER(L.MESSAGE) LIKE '%NO DATA%'
                            THEN 'No Data Found'
                            ELSE 'Others Failure Reason'
                        END AS FAILURE_REASON FROM LATEST_FAILURE L WHERE L.RN = 1
                    AND NOT EXISTS
                    (
                        SELECT /*+ INDEX(B IDX_SM_BILLING_METER_BILLMONTH) */  1 FROM RCMPA.SMART_METER_BILLING_DATA_SEP B
                        WHERE B.METERNO = L.METERNO
                        AND
                        (
                            (
                                L.DEPARTMENT IN ('GCC', 'MLCC', 'KCC') AND B.BILLMONTH = L.BILLMONTH
                            )
                            OR
                            (
                                L.DEPARTMENT IN ('SLCC')  AND B.READING_MONTH = L.READING_MONTH
                            )
                        )
                    )
                    AND NOT EXISTS
                    (
                        SELECT /*+ INDEX(B IDX_SM_BILLING_METER_BILLMONTH) */ 1 FROM RCMPA.SMART_METER_BILLING_DATA B
                        WHERE B.METERNO = L.METERNO
                        AND
                        (
                            (
                                L.DEPARTMENT IN ('GCC', 'MLCC', 'KCC') AND B.BILLMONTH = L.BILLMONTH
                            )
                            OR
                            (
                                L.DEPARTMENT IN ('SLCC') AND B.READING_MONTH = L.READING_MONTH
                            )
                        )
                    )
                ) SELECT FAILURE_REASON,  COUNT(*) AS TOTAL_COUNT FROM FAILURE_DATA GROUP BY FAILURE_REASON ORDER BY TOTAL_COUNT DESC";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add("DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    cmd.Parameters.Add("READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            failureReasonCounts.Add(new FailureReasonCount
                            {
                                FeilureReason = dr["FAILURE_REASON"].ToString(),

                                count = dr["TOTAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_COUNT"])
                            });
                        }
                    }
                }
                return failureReasonCounts;
            }
        }

        //Failure Reason Counts BYPL
        public List<FailureReasonCountBypl> FailureReasonCountsBYPL(string ReadingMonth, UserAccessScope scope)
        {
            List<FailureReasonCountBypl> failureReasonCounts = new List<FailureReasonCountBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                (
                    SELECT TRIM( REGEXP_SUBSTR(:READING_MONTH, '[^,]+', 1, LEVEL) )
                    FROM DUAL CONNECT BY REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1,LEVEL) IS NOT NULL
                ),
                SLCC_DATA AS
                (
                    SELECT /*+ PARALLEL(S,8) */ TRIM(S.METERNO) AS METERNO, TRIM(S.BILL_MONTH) AS BILL_MONTH,
                    S.IS_FAILED, S.REMARKS, NVL(TRIM(S.DEPARTMENT), 'SLCC') AS DEPARTMENT, 1 AS SOURCE_PRIORITY 
                    FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                    WHERE S.BILL_MONTH IN
                    (
                        SELECT M.READING_MONTH FROM MONTHS M
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER(NVL(TRIM(S.DEPARTMENT), 'SLCC')) = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                KCC_GCC_DATA AS
                (
                    SELECT /*+ PARALLEL(K,8) */ TRIM(K.METERNO) AS METERNO, TRIM(K.BILL_MONTH) AS BILL_MONTH,
                    K.IS_FAILED, K.REMARKS, NVL(TRIM(K.DEPARTMENT), 'KCC/GCC') AS DEPARTMENT, 2 AS SOURCE_PRIORITY
                    FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                    WHERE K.BILL_MONTH IN
                    (
                        SELECT M.READING_MONTH FROM MONTHS M
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER( NVL(TRIM(K.DEPARTMENT), 'KCC/GCC')) = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                BASE_DATA AS
                (
                    SELECT METERNO, BILL_MONTH, IS_FAILED,REMARKS, DEPARTMENT,SOURCE_PRIORITY
                    FROM SLCC_DATA
                    UNION ALL
                    SELECT METERNO, BILL_MONTH, IS_FAILED,REMARKS, DEPARTMENT,SOURCE_PRIORITY
                    FROM KCC_GCC_DATA
                ),
                DISTINCT_DATA AS
                (
                    SELECT METERNO,BILL_MONTH,IS_FAILED, REMARKS,DEPARTMENT
                    FROM
                    (
                        SELECT
                            B.*,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY B.METERNO, B.BILL_MONTH,UPPER(TRIM(B.DEPARTMENT))
                                ORDER BY  B.IS_FAILED DESC,  B.SOURCE_PRIORITY DESC
                            ) AS RN
                        FROM BASE_DATA B WHERE B.METERNO IS NOT NULL
                    )
                    WHERE RN = 1
                ),
                FAILED_DATA AS
                (
                    SELECT METERNO, BILL_MONTH, REMARKS, DEPARTMENT FROM DISTINCT_DATA WHERE IS_FAILED = 1
                ),
                FAILURE_DATA AS
                (
                    SELECT METERNO,DEPARTMENT,
                        CASE
                            WHEN REMARKS IS NULL
                            THEN 'Unknown Failure'
                            WHEN REMARKS LIKE
                                 '%Smart  Meter date  is older then FormY SAP_MRO_DOWNLOAD_DATE for the meter no%'
                            THEN 'Date older then FormY'
                            WHEN REMARKS LIKE
                                 '%Data not found in form_y%'
                            THEN 'No Data Found in FORMY'
                            WHEN REMARKS LIKE
                                 '%Data not found in HES%'
                            THEN 'No Data Found'
                            ELSE 'Others Failure Reason'
                        END AS FAILURE_REASON
                    FROM FAILED_DATA
                )
                SELECT DEPARTMENT, FAILURE_REASON,COUNT(DISTINCT METERNO) AS TOTAL_COUNT FROM FAILURE_DATA
                GROUP BY DEPARTMENT, FAILURE_REASON ORDER BY DEPARTMENT, TOTAL_COUNT DESC";
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    cmd.Parameters.Add(":IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add(":DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            failureReasonCounts.Add(new FailureReasonCountBypl
                            {
                                FeilureReason = dr["FAILURE_REASON"].ToString(),

                                count = dr["TOTAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_COUNT"])
                            });
                        }
                    }
                }
                return failureReasonCounts;
            }
        }

        //BRPL HES DOWNLOAD METERS Detailed Summary
        public List<HesDownloadMeter> HesDownloadMeterList(string readingMonth,UserAccessScope scope)
        {
            List<HesDownloadMeter> hesDownloadMeters = new List<HesDownloadMeter>();

            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();

                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */ TRIM( REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1, LEVEL )) AS READING_MONTH
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR(:READING_MONTH,'[^,]+',1, LEVEL ) IS NOT NULL
                ),
                BILLING_DATA AS
                (
                    SELECT METERNO, CONS_REF, READING_MONTH, BILLMONTH, SAP_DEPARTMENT, CYCLE, ENTRY_DATE
                    FROM RCMPA.SMART_METER_BILLING_DATA_SEP
                    UNION ALL
                    SELECT METERNO, CONS_REF, READING_MONTH, BILLMONTH, SAP_DEPARTMENT, CYCLE, ENTRY_DATE
                    FROM RCMPA.SMART_METER_BILLING_DATA
                ),
                LATEST_DOWNLOAD AS
                (
                    SELECT TRIM(B.METERNO) AS METERNO, B.CONS_REF, B.READING_MONTH, B.BILLMONTH,
                        CASE
                            WHEN B.SAP_DEPARTMENT = 'MLCC'  AND B.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN B.CYCLE IN ('KA','KC','KG')
                            THEN 'KCC'
                            WHEN B.SAP_DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                        END AS SAP_DEPARTMENT,
                        B.CYCLE,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY  TRIM(B.METERNO), B.READING_MONTH ORDER BY B.ENTRY_DATE DESC
                        ) AS RN
                    FROM BILLING_DATA B
                    WHERE
                    (
                           B.METERNO LIKE '91______'
                        OR B.METERNO LIKE '90______'
                        OR B.METERNO LIKE 'AL________'
                        OR B.METERNO LIKE 'KI________'
                        OR B.METERNO LIKE 'RSE%'
                    )
                    AND
                    (
                        (
                            CASE
                                WHEN B.SAP_DEPARTMENT = 'MLCC'  AND B.CYCLE = '0N'
                                THEN 'KCC'
                                WHEN B.CYCLE IN ('KA','KC','KG')
                                THEN 'KCC'
                                WHEN B.SAP_DEPARTMENT IS NULL
                                THEN 'SLCC'
                                ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                            END IN ('GCC','MLCC','KCC')
                            AND EXISTS
                            (
                                SELECT 1 FROM MONTHS M  WHERE M.READING_MONTH = B.BILLMONTH
                            )
                        )
                        OR
                        (
                            CASE
                                WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N'
                                THEN 'KCC'
                                WHEN B.CYCLE IN ('KA','KC','KG')
                                THEN 'KCC'
                                WHEN B.SAP_DEPARTMENT IS NULL
                                THEN 'SLCC'
                                ELSE UPPER(TRIM(B.SAP_DEPARTMENT))
                            END IN ('SLCC')
                            AND EXISTS
                            (
                                SELECT 1 FROM MONTHS M WHERE M.READING_MONTH = B.READING_MONTH
                            )
                        )
                    )
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR
                        CASE
                            WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N'
                            THEN 'KCC'
                            WHEN B.CYCLE IN ('KA','KC','KG')
                            THEN 'KCC'
                            WHEN B.SAP_DEPARTMENT IS NULL
                            THEN 'SLCC'
                            ELSE UPPER(TRIM(B.SAP_DEPARTMENT)) END = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                DOWNLOAD_DATA AS
                (
                    SELECT METERNO, CONS_REF, READING_MONTH, BILLMONTH, SAP_DEPARTMENT, CYCLE FROM LATEST_DOWNLOAD  WHERE RN = 1
                ),
                SLCC_DATA AS
                (
                    SELECT  CONS_REF,  READING_MONTH, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK,  FATHER_NAME
                    FROM
                    (
                        SELECT S.CONS_REF, S.READING_MONTH, S.SAP_DIVISION, S.SAP_SEQ_NO, S.ADD1, S.ADD2, S.ADD3, S.LAND_MARK, S.FATHER_NAME,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY  S.CONS_REF, S.READING_MONTH ORDER BY S.ROWID
                            ) AS RN
                        FROM RCMPA.SAP_SLCC_FORMY S INNER JOIN DOWNLOAD_DATA D ON D.CONS_REF = S.CONS_REF AND D.READING_MONTH = S.READING_MONTH
                    ) WHERE RN = 1
                ),
                FORMY_DATA AS
                (
                    SELECT  CONS_REF, READING_MONTH, SAP_DIVISION, SAP_SEQ_NO, ADD1,  ADD2, ADD3, LAND_MARK, FATHER_NAME
                    FROM
                    (
                        SELECT FM.CONS_REF, FM.READING_MONTH,  FM.SAP_DIVISION,  FM.SAP_SEQ_NO, FM.ADD1, FM.ADD2,  FM.ADD3, FM.LAND_MARK, FM.FATHER_NAME,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY FM.CONS_REF, FM.READING_MONTH ORDER BY FM.ROWID
                            ) AS RN FROM RCMPA.SAP_FORMY_PREV FM
                        INNER JOIN DOWNLOAD_DATA D ON D.CONS_REF = FM.CONS_REF AND D.READING_MONTH = FM.READING_MONTH
                        WHERE TO_DATE( D.READING_MONTH, 'YYYYMM' ) = ADD_MONTHS(  TRUNC(SYSDATE,'MM'),-1 )
                    ) WHERE RN = 1
                    UNION ALL
                    SELECT CONS_REF, READING_MONTH, SAP_DIVISION, SAP_SEQ_NO, ADD1,ADD2, ADD3, LAND_MARK, FATHER_NAME
                    FROM
                    (
                        SELECT FM.CONS_REF, FM.READING_MONTH, FM.SAP_DIVISION, FM.SAP_SEQ_NO, FM.ADD1, FM.ADD2, FM.ADD3, FM.LAND_MARK, FM.FATHER_NAME,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY FM.CONS_REF,  FM.READING_MONTH  ORDER BY FM.ROWID
                            ) AS RN
                        FROM RCMPA.SAP_FORMY FM
                        INNER JOIN DOWNLOAD_DATA D ON D.CONS_REF = FM.CONS_REF AND D.READING_MONTH = FM.READING_MONTH
                        WHERE TO_DATE(  D.READING_MONTH, 'YYYYMM' ) <> ADD_MONTHS(TRUNC(SYSDATE,'MM'),  -1 )
                    ) WHERE RN = 1
                )
                SELECT D.METERNO, D.CONS_REF, D.SAP_DEPARTMENT, D.CYCLE,
                    NVL( S.SAP_DIVISION, FM.SAP_DIVISION ) AS SAP_DIVISION,
                    NVL( S.SAP_SEQ_NO, FM.SAP_SEQ_NO ) AS SAP_SEQ_NO,
                    RTRIM
                    (
                        NVL(S.ADD1, FM.ADD1) || ', ' ||  NVL(S.ADD2, FM.ADD2) || ', ' ||  NVL(S.ADD3, FM.ADD3) || ', ' ||
                        NVL(S.LAND_MARK, FM.LAND_MARK) || ', ' ||  NVL(S.FATHER_NAME, FM.FATHER_NAME), ', '
                    ) AS ADDRESS,
                    CASE
                        WHEN SUBSTR(D.METERNO,1,2) IN ('90','AL')
                        THEN 'ALLIED'
                        WHEN SUBSTR(D.METERNO,1,2) IN ('91','KI')
                        THEN 'KIMBAL'
                        WHEN D.METERNO LIKE 'RSE%'
                        THEN 'RSE'
                        ELSE 'UNKNOWN'
                    END AS METER_TYPE,
                    CASE
                        WHEN SUBSTR(D.METERNO,1,4) IN
                        (
                            'AL91',
                            'KI91',
                            '9150',
                            '9008',
                            '9027'
                        )
                        THEN '1PH'
                        WHEN SUBSTR(D.METERNO,1,4) IN
                        (
                            'AL90',
                            'KI90',
                            '9026',
                            'RSE7'    
                        )
                        THEN '3PH'
                        ELSE 'UNKNOWN'
                    END AS PHASE_TYPE
                FROM DOWNLOAD_DATA D LEFT JOIN SLCC_DATA S ON S.CONS_REF = D.CONS_REF AND S.READING_MONTH = D.READING_MONTH
                LEFT JOIN FORMY_DATA FM ON FM.CONS_REF = D.CONS_REF AND FM.READING_MONTH = D.READING_MONTH
                ORDER BY D.SAP_DEPARTMENT, D.CYCLE, NVL(S.SAP_DIVISION, FM.SAP_DIVISION), NVL(S.SAP_SEQ_NO, FM.SAP_SEQ_NO), D.METERNO";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add(  ":IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin? 1: 0;

                    cmd.Parameters.Add( ":DEPARTMENT", OracleDbType.Varchar2).Value =scope == null || string.IsNullOrWhiteSpace(scope.Department) ? string.Empty
                            : scope.Department.Trim().ToUpperInvariant();

                    cmd.Parameters.Add( ":READING_MONTH", OracleDbType.Varchar2).Value = readingMonth ?? string.Empty;

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            hesDownloadMeters.Add(
                                new HesDownloadMeter
                                {
                                    MeterNumber =  dr["METERNO"]?.ToString(),

                                    SapDepartment =  dr["SAP_DEPARTMENT"]?.ToString(),

                                    Cycle = dr["CYCLE"]?.ToString(),

                                    MeterType = dr["METER_TYPE"]?.ToString(),

                                    Phase =  dr["PHASE_TYPE"]?.ToString(),

                                    ConsRef = dr["CONS_REF"]?.ToString(),

                                    SapDivision = dr["SAP_DIVISION"]?.ToString(),

                                    Address =  dr["ADDRESS"]?.ToString(),

                                    SapSeqNo = dr["SAP_SEQ_NO"]?.ToString()
                                }
                            );
                        }
                    }
                }
            }

            return hesDownloadMeters;
        }

        //HES Download Meter Detailed Summary BYPL
        public List<HesDownloadMeterBypl> GetHesDownloadBypl(string readingMonth, UserAccessScope scope)
        {
            List<HesDownloadMeterBypl> hesDownloadMetersBypl = new List<HesDownloadMeterBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */TRIM(  REGEXP_SUBSTR(:READING_MONTH,'[^,]+',1, LEVEL)) AS READING_MONTH
                    FROM DUAL CONNECT BY REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1, LEVEL) IS NOT NULL
                ),
                SLCC_ALL_DATA AS
                (
                    SELECT /*+ PARALLEL(S,8) */  TRIM(S.METERNO) AS METERNO, TRIM(S.BILL_MONTH) AS BILL_MONTH, S.IS_FAILED, S.INSERTED_DATE, NVL(TRIM(S.DEPARTMENT),'SLCC' ) AS DEPARTMENT
                    FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                    WHERE S.BILL_MONTH IN
                    (
                        SELECT M.READING_MONTH FROM MONTHS M
                    )
                    AND S.METERNO IS NOT NULL
                    AND
                    (
                        :IS_SUPERADMIN = 1
                        OR UPPER( NVL(TRIM(S.DEPARTMENT), 'SLCC') ) = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                KCC_ALL_DATA AS
                (
                    SELECT /*+ PARALLEL(K,8) */ TRIM(K.METERNO) AS METERNO, TRIM(K.BILL_MONTH) AS BILL_MONTH, K.IS_FAILED, K.INSERTED_DATE, NVL( TRIM(K.DEPARTMENT), 'KCC/GCC') AS DEPARTMENT
                    FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                    WHERE K.BILL_MONTH IN
                    (
                        SELECT M.READING_MONTH  FROM MONTHS M
                    )
                    AND K.METERNO IS NOT NULL
                    AND
                    (
                        :IS_SUPERADMIN = 1
                         OR UPPER(NVL(TRIM(K.DEPARTMENT), 'KCC/GCC')) = UPPER(TRIM(:DEPARTMENT))
                    )
                ),
                BASE_DATA AS
                (
                    SELECT METERNO,BILL_MONTH, IS_FAILED, INSERTED_DATE, DEPARTMENT, 1 AS SOURCE_PRIORITY FROM SLCC_ALL_DATA
                    UNION ALL
                    SELECT METERNO,BILL_MONTH,IS_FAILED,INSERTED_DATE, DEPARTMENT, 2 AS SOURCE_PRIORITY FROM KCC_ALL_DATA
                ),
                DISTINCT_METER_MONTH AS
                (
                    SELECT METERNO, BILL_MONTH, MAX(IS_FAILED) AS IS_FAILED FROM BASE_DATA
                    GROUP BY METERNO, BILL_MONTH
                ),
                METER_STATUS AS
                (
                    SELECT METERNO, MAX(IS_FAILED) AS IS_FAILED FROM DISTINCT_METER_MONTH GROUP BY METERNO
                ),
                DOWNLOADED_METERS AS
                (
                    SELECT METERNO FROM METER_STATUS WHERE IS_FAILED = 0
                ),
                SLCC_DOWNLOADED AS
                (
                    SELECT S.METERNO, S.READING_MONTH, S.INSERTED_DATE, S.DEPARTMENT
                    FROM
                    (
                        SELECT D.METERNO, D.BILL_MONTH AS READING_MONTH,D.INSERTED_DATE,D.DEPARTMENT,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY D.METERNO, D.BILL_MONTH
                                ORDER BY D.INSERTED_DATE DESC NULLS LAST
                            ) AS RN
                        FROM SLCC_ALL_DATA D JOIN DOWNLOADED_METERS DM ON DM.METERNO = D.METERNO WHERE D.IS_FAILED = 0
                    ) S
                    WHERE S.RN = 1
                ),
                KCC_DOWNLOADED AS
                (
                    SELECT K.METERNO,K.READING_MONTH, K.INSERTED_DATE,K.DEPARTMENT
                    FROM
                    (
                        SELECT D.METERNO, D.BILL_MONTH AS READING_MONTH, D.INSERTED_DATE,D.DEPARTMENT,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY D.METERNO, D.BILL_MONTH ORDER BY  D.INSERTED_DATE DESC NULLS LAST
                            ) AS RN
                        FROM KCC_ALL_DATA D JOIN DOWNLOADED_METERS DM ON DM.METERNO = D.METERNO WHERE D.IS_FAILED = 0
                    ) K
                    WHERE K.RN = 1
                ),
                SLCC_METERS AS
                (
                    SELECT /*+ MATERIALIZE */ DISTINCT METERNO FROM SLCC_DOWNLOADED
                ),
                KCC_METERS AS
                (
                    SELECT /*+ MATERIALIZE */ DISTINCT METERNO FROM KCC_DOWNLOADED
                ),
                SLCC_FORMY_FALLBACK AS
                (
                    SELECT METERNO, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2,ADD3, LAND_MARK,FATHER_NAME
                    FROM
                    (
                        SELECT  /*+ LEADING(M F)  USE_NL(F) INDEX(F IDX_SLCC_FORMY_MTR_MONTH) */
                            F.METERNO, F.CONS_REF, F.SAP_DIVISION, F.SAP_SEQ_NO,  F.ADD1, F.ADD2,F.ADD3,F.LAND_MARK,  F.FATHER_NAME,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY F.METERNO ORDER BY F.READING_MONTH DESC
                            ) AS RN
                        FROM SLCC_METERS M JOIN RCMPA.SAP_SLCC_FORMY F ON F.METERNO = M.METERNO
                    )
                    WHERE RN = 1
                ),
                SAP_FORMY_FALLBACK AS
                (
                    SELECT METERNO, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1,ADD2, ADD3,LAND_MARK,FATHER_NAME
                    FROM
                    (
                        SELECT  /*+ LEADING(M F) USE_NL(F) INDEX(F IDX_SAP_FORMY_MTR_MONTH) */
                            F.METERNO, F.CONS_REF,F.SAP_DIVISION,F.SAP_SEQ_NO,F.ADD1,F.ADD2,F.ADD3,F.LAND_MARK, F.FATHER_NAME,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY F.METERNO ORDER BY F.READING_MONTH DESC
                            ) AS RN
                        FROM KCC_METERS M JOIN RCMPA.SAP_FORMY F ON F.METERNO = M.METERNO
                    ) WHERE RN = 1
                ),
                SLCC_DATA AS
                (
                    SELECT D.METERNO, D.READING_MONTH,D.DEPARTMENT, D.INSERTED_DATE,
                        COALESCE(
                            NULLIF(TRIM(FM.CONS_REF), ''),
                            NULLIF(TRIM(FL.CONS_REF), '')
                        ) AS CONS_REF,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_DIVISION), ''),
                            NULLIF(TRIM(FL.SAP_DIVISION), '')
                        ) AS SAP_DIVISION,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_SEQ_NO), ''),
                            NULLIF(TRIM(FL.SAP_SEQ_NO), '')
                        ) AS SAP_SEQ_NO,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD1), ''),
                            NULLIF(TRIM(FL.ADD1), '')
                        ) AS ADD1,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD2), ''),
                            NULLIF(TRIM(FL.ADD2), '')
                        ) AS ADD2,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD3), ''),
                            NULLIF(TRIM(FL.ADD3), '')
                        ) AS ADD3,
                        COALESCE(
                            NULLIF(TRIM(FM.LAND_MARK), ''),
                            NULLIF(TRIM(FL.LAND_MARK), '')
                        ) AS LAND_MARK,
                        COALESCE(
                            NULLIF(TRIM(FM.FATHER_NAME), ''),
                            NULLIF(TRIM(FL.FATHER_NAME), '')
                        ) AS FATHER_NAME
                    FROM SLCC_DOWNLOADED D LEFT JOIN RCMPA.SAP_SLCC_FORMY FM ON FM.METERNO = D.METERNO AND FM.READING_MONTH = D.READING_MONTH
                    LEFT JOIN SLCC_FORMY_FALLBACK FL ON FL.METERNO = D.METERNO
                ),
                KCC_DATA AS
                (
                    SELECT D.METERNO, D.READING_MONTH, D.DEPARTMENT,D.INSERTED_DATE,
                        COALESCE(
                            NULLIF(TRIM(FM.CONS_REF), ''),
                            NULLIF(TRIM(FL.CONS_REF), '')
                        ) AS CONS_REF,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_DIVISION), ''),
                            NULLIF(TRIM(FL.SAP_DIVISION), '')
                        ) AS SAP_DIVISION,
                        COALESCE(
                            NULLIF(TRIM(FM.SAP_SEQ_NO), ''),
                            NULLIF(TRIM(FL.SAP_SEQ_NO), '')
                        ) AS SAP_SEQ_NO,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD1), ''),
                            NULLIF(TRIM(FL.ADD1), '')
                        ) AS ADD1,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD2), ''),
                            NULLIF(TRIM(FL.ADD2), '')
                        ) AS ADD2,
                        COALESCE(
                            NULLIF(TRIM(FM.ADD3), ''),
                            NULLIF(TRIM(FL.ADD3), '')
                        ) AS ADD3,
                        COALESCE(
                            NULLIF(TRIM(FM.LAND_MARK), ''),
                            NULLIF(TRIM(FL.LAND_MARK), '')
                        ) AS LAND_MARK,
                        COALESCE(
                            NULLIF(TRIM(FM.FATHER_NAME), ''),
                            NULLIF(TRIM(FL.FATHER_NAME), '')
                        ) AS FATHER_NAME
                    FROM KCC_DOWNLOADED D LEFT JOIN RCMPA.SAP_FORMY FM ON FM.METERNO = D.METERNO AND FM.READING_MONTH = D.READING_MONTH
                    LEFT JOIN SAP_FORMY_FALLBACK FL ON FL.METERNO = D.METERNO
                ),
                FINAL_DATA AS
                (
                    SELECT METERNO, READING_MONTH, DEPARTMENT, INSERTED_DATE, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1,ADD2,ADD3,LAND_MARK, FATHER_NAME, 1 AS SOURCE_PRIORITY FROM SLCC_DATA
                    UNION ALL
                    SELECT METERNO, READING_MONTH, DEPARTMENT, INSERTED_DATE, CONS_REF, SAP_DIVISION, SAP_SEQ_NO,ADD1,ADD2,ADD3, LAND_MARK, FATHER_NAME,2 AS SOURCE_PRIORITY FROM KCC_DATA
                ),
                DISTINCT_FINAL_DATA AS
                (
                    SELECT METERNO, READING_MONTH, DEPARTMENT, INSERTED_DATE, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK,FATHER_NAME
                    FROM
                    (
                        SELECT
                            F.*,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY F.METERNO
                                ORDER BY F.INSERTED_DATE DESC NULLS LAST,F.SOURCE_PRIORITY DESC
                            ) AS RN
                        FROM FINAL_DATA F
                    ) WHERE RN = 1
                )
                SELECT D.METERNO, D.CONS_REF,
                    CASE
                        WHEN D.METERNO LIKE '92%'
                          OR D.METERNO LIKE '99%'
                          OR D.METERNO LIKE 'AL92%'
                          OR D.METERNO LIKE 'AL99%'
                        THEN '1PH'
                        WHEN D.METERNO LIKE 'AL97%'
                        THEN '3PH'
                        WHEN D.METERNO LIKE '98%'
                          OR D.METERNO LIKE 'KI98%'
                        THEN '1PH'
                        WHEN D.METERNO LIKE '97%'
                          OR D.METERNO LIKE 'KI97%'
                        THEN '3PH'
                        ELSE 'UNKNOWN'
                    END AS PHASE_TYPE, D.DEPARTMENT AS SAP_DEPARTMENT, D.SAP_DIVISION, D.SAP_SEQ_NO,
                    RTRIM(
                        D.ADD1 || ', ' ||
                        D.ADD2 || ', ' ||
                        D.ADD3 || ', ' ||
                        D.LAND_MARK || ', ' ||
                        D.FATHER_NAME,
                        ', '
                    ) AS ADDRESS,
                    CASE
                        WHEN D.METERNO LIKE '92%'
                          OR D.METERNO LIKE '99%'
                          OR D.METERNO LIKE 'AL%'
                        THEN 'ALLIED'
                        WHEN D.METERNO LIKE 'KI%'
                          OR D.METERNO LIKE '97%'
                          OR D.METERNO LIKE '98%'
                        THEN 'KIMBAL'
                        ELSE 'UNKNOWN'
                    END AS METER_TYPE, D.READING_MONTH, D.INSERTED_DATE
                FROM DISTINCT_FINAL_DATA D ORDER BY D.INSERTED_DATE DESC NULLS LAST, D.METERNO";
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(":IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add(":DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = readingMonth;

                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            hesDownloadMetersBypl.Add(new HesDownloadMeterBypl
                            {
                                MeterNumber = dr["METERNO"].ToString(),

                                SapDepartment = dr["SAP_DEPARTMENT"].ToString(),

                                MeterType = dr["METER_TYPE"].ToString(),

                                Phase = dr["PHASE_TYPE"].ToString(),

                                ConsRef = dr["CONS_REF"].ToString(),

                                SapDivision = dr["SAP_DIVISION"].ToString(),

                                Address = dr["ADDRESS"].ToString(),

                                SapSeqNo = dr["SAP_SEQ_NO"].ToString()
                            });
                        }
                    }
                }
            }
            return hesDownloadMetersBypl;
        }

        public List<CMRIDownload> GetCMRIDownload(string readingMonth, UserAccessScope scope)
        {
            List<CMRIDownload> cmriDownloads = new List<CMRIDownload>();
            using(OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH BILLING_BASE AS
            (
                /*+ MATERIALIZE */ SELECT B.CONS_REF, TRIM(B.METERNO)  AS METERNO,  B.CYCLE, B.BILLMONTH, B.SAP_DEPARTMENT AS BILLING_DEPARTMENT,
                    CASE
                        WHEN B.SAP_DEPARTMENT = 'MLCC' AND B.CYCLE = '0N' THEN 'KCC'
                        WHEN B.CYCLE IN ('KA','KC','KG') THEN 'KCC'
                        WHEN B.SAP_DEPARTMENT IS NULL THEN 'SLCC'
                        ELSE TRIM(B.SAP_DEPARTMENT)
                    END AS DISPLAY_DEPARTMENT
                FROM RCMPA.SMART_METER_BILLING_DATA B WHERE B.BILLMONTH  = :BILLMONTH AND B.MACHINE_ID IS NOT NULL
                 AND B.METERNO    IS NOT NULL
            ),
            BILLING_FILTERED AS
            (
                SELECT * FROM BILLING_BASE  WHERE :IS_SUPERADMIN = 1 OR UPPER(DISPLAY_DEPARTMENT) = UPPER(TRIM(:DEPARTMENT))
            ),
            BILLING_METERS AS
            (
                SELECT /*+ MATERIALIZE */ DISTINCT METERNO FROM BILLING_FILTERED
            ),
            FORMY_ALL AS
            (
                SELECT /*+ PARALLEL(F,8) NO_MERGE */ F.METERNO, F.CONS_REF, F.SAP_DIVISION, F.SAP_SEQ_NO, F.ADD1, F.ADD2, F.ADD3, F.LAND_MARK,
                    F.READING_MONTH,  1 AS SOURCE_PRIORITY FROM RCMPA.SAP_FORMY F
                WHERE EXISTS
                (
                    SELECT 1 FROM BILLING_METERS B WHERE B.METERNO = F.METERNO
                )
                UNION ALL
                SELECT /*+ PARALLEL(F,8) NO_MERGE */ F.METERNO, F.CONS_REF, F.SAP_DIVISION, F.SAP_SEQ_NO, F.ADD1, F.ADD2, F.ADD3, F.LAND_MARK, F.READING_MONTH,
                 2 AS SOURCE_PRIORITY FROM RCMPA.SAP_SLCC_FORMY F
                WHERE EXISTS
                (
                    SELECT 1 FROM BILLING_METERS B WHERE B.METERNO = F.METERNO
                )
            ),
            FORMY_DATA AS
            (
                SELECT METERNO,
                    MAX(CONS_REF) KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS CONS_REF,
                    MAX(SAP_DIVISION) KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS SAP_DIVISION,
                    MAX(SAP_SEQ_NO)  KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS SAP_SEQ_NO,
                    MAX(ADD1)KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS ADD1,
                    MAX(ADD2)KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS ADD2,
                    MAX(ADD3) KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS ADD3,
                    MAX(LAND_MARK) KEEP (DENSE_RANK FIRST ORDER BY
                    CASE WHEN READING_MONTH = :BILLMONTH THEN 0 ELSE 1 END,
                    SOURCE_PRIORITY,
                    READING_MONTH DESC NULLS LAST) AS LAND_MARK
                FROM FORMY_ALL
                GROUP BY METERNO
            )
            SELECT /*+ PARALLEL(4) */
                COALESCE( NULLIF(TRIM(F.CONS_REF), ''), NULLIF(TRIM(B.CONS_REF), '')) AS CONS_REF,
                B.METERNO, B.CYCLE, F.SAP_DIVISION AS DIVISION, B.DISPLAY_DEPARTMENT AS DEPARTMENT,
                RTRIM(
                    NVL(TRIM(F.ADD1), '') ||
                    CASE WHEN TRIM(F.ADD2) IS NOT NULL THEN ', ' || TRIM(F.ADD2) ELSE '' END ||
                    CASE WHEN TRIM(F.ADD3) IS NOT NULL THEN ', ' || TRIM(F.ADD3) ELSE '' END ||
                    CASE WHEN TRIM(F.LAND_MARK) IS NOT NULL THEN ', ' || TRIM(F.LAND_MARK) ELSE '' END,
                    ', '
                ) AS ADDRESS, F.SAP_SEQ_NO FROM BILLING_FILTERED B LEFT JOIN FORMY_DATA F ON F.METERNO = B.METERNO ORDER BY B.METERNO";

                using(OracleCommand cmd = new OracleCommand(query,con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(":BILLMONTH", OracleDbType.Varchar2).Value = readingMonth;
                    cmd.Parameters.Add(":IS_SUPERADMIN", OracleDbType.Int32).Value = scope != null && scope.IsSuperAdmin ? 1 : 0;
                    cmd.Parameters.Add(":DEPARTMENT", OracleDbType.Varchar2).Value = scope == null || string.IsNullOrWhiteSpace(scope.Department) ?
                        string.Empty : scope.Department.Trim().ToUpperInvariant();

                    using(OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            cmriDownloads.Add(new CMRIDownload
                            {
                                cons_ref = dr["CONS_REF"]?.ToString(),
                                meterno = dr["METERNO"]?.ToString(),
                                Cycle = dr["CYCLE"]?.ToString(),
                                Division = dr["DIVISION"]?.ToString(),
                                Department = dr["DEPARTMENT"]?.ToString(),
                                Address = dr["ADDRESS"]?.ToString(),
                                SapSeqNo = dr["SAP_SEQ_NO"]?.ToString()
                            });
                        }
                    }
                }
            }
            return cmriDownloads;
        }
    }
}