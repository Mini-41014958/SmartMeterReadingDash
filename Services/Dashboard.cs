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

        public TotalMeterSummary GetMeterSummary(string ReadingMonth)
        {
            TotalMeterSummary Summary  = new TotalMeterSummary();

            using (OracleConnection  con = _db.GetConnection())
            {
                con.Open();

                string query = @"WITH MONTHS AS
                (
                    SELECT REGEXP_SUBSTR(
                               :READING_MONTH,
                               '[^,]+',
                               1,
                               LEVEL
                           ) AS READING_MONTH
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR(
                                   :READING_MONTH,
                                   '[^,]+',
                                   1,
                                   LEVEL
                               ) IS NOT NULL
                ),
                DOWNLOAD AS
                (
                    SELECT
                        SUM(DOWNLOAD_COUNT)  AS DOWNLOAD_COUNT,
                        SUM(ALLIED_DOWNLOAD) AS ALLIED_DOWNLOAD,
                        SUM(KIMBAL_DOWNLOAD) AS KIMBAL_DOWNLOAD,
                        SUM(PHASE_1PH)       AS PHASE_1PH,
                        SUM(PHASE_3PH)       AS PHASE_3PH,
                        SUM(ALLIED_1PH)     AS ALLIED_1PH,
                        SUM(ALLIED_3PH)     AS ALLIED_3PH,
                        SUM(KIMBAL_1PH)     AS KIMBAL_1PH,
                        SUM(KIMBAL_3PH)     AS KIMBAL_3PH
                    FROM
                    (
                        SELECT
                            SM.READING_MONTH,
                            COUNT(*) AS DOWNLOAD_COUNT,
                            SUM(
                                CASE
                                    WHEN (SM.METERNO LIKE '90%'
                                      OR SM.METERNO LIKE 'AL%')
                                      AND SM.METERNO NOT LIKE '9026%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS ALLIED_DOWNLOAD,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE '91%'
                                      OR SM.METERNO LIKE 'KI%'
                                      OR SM.METERNO LIKE '9026%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS KIMBAL_DOWNLOAD,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE 'AL91%'
                                      OR SM.METERNO LIKE 'KI91%'
                                      OR SM.METERNO LIKE '9150%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS PHASE_1PH,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE '9026%'
                                      OR SM.METERNO LIKE 'KI90%'
                                      OR SM.METERNO LIKE 'AL90%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS PHASE_3PH,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE 'AL91%'
                                    OR SM.METERNO LIKE '9008%'
                                    OR SM.METERNO LIKE '9027%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS ALLIED_1PH,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE 'AL90%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS ALLIED_3PH,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE 'KI91%'
                                      OR SM.METERNO LIKE '9150%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS KIMBAL_1PH,
                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE 'KI90%'
                                      OR SM.METERNO LIKE '9026%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS KIMBAL_3PH
                        FROM RCMPA.SMART_METER_BILLING_DATA SM
                        WHERE SM.METERNO NOT LIKE '%D%'
                          AND
                          (
                                SM.METERNO LIKE '91______'
                             OR SM.METERNO LIKE '90______'
                             OR SM.METERNO LIKE 'AL________'
                             OR SM.METERNO LIKE 'KI________'
                          )
                          AND SM.READING_MONTH IN
                          (
                              SELECT M.READING_MONTH
                              FROM MONTHS M
                          )
                        GROUP BY SM.READING_MONTH
                    )
                ),
                FAILED AS
                (
                    SELECT
                        SUM(FAILED_COUNT)  AS FAILED_COUNT,
                        SUM(ALLIED_FAILED) AS ALLIED_FAILED,
                        SUM(KIMBAL_FAILED) AS KIMBAL_FAILED,
                        SUM(PHASE_1PH)     AS PHASE_1PH,
                        SUM(PHASE_3PH)     AS PHASE_3PH,
                        SUM(ALLIED_1PH)   AS ALLIED_1PH,
                        SUM(ALLIED_3PH)   AS ALLIED_3PH,
                        SUM(KIMBAL_1PH)   AS KIMBAL_1PH,
                        SUM(KIMBAL_3PH)   AS KIMBAL_3PH
                    FROM
                    (
                        SELECT
                            L.READING_MONTH,
                            COUNT(DISTINCT L.METERNO) AS FAILED_COUNT,
                            COUNT(
                                DISTINCT CASE
                                    WHEN
                                    ( L.METERNO LIKE '90%'
                                      OR L.METERNO LIKE 'AL%')
                                      AND L.METERNO NOT LIKE '9026%'
                                    THEN L.METERNO
                                END
                            ) AS ALLIED_FAILED,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE '91%'
                                      OR L.METERNO LIKE 'KI%'
                                      OR L.METERNO LIKE '9026%'
                                    THEN L.METERNO
                                END
                            ) AS KIMBAL_FAILED,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE 'AL91%'
                                      OR L.METERNO LIKE 'KI91%'
                                      OR L.METERNO LIKE '9150%'
                                    THEN L.METERNO
                                END
                            ) AS PHASE_1PH,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE '9026%'
                                      OR L.METERNO LIKE 'KI90%'
                                      OR L.METERNO LIKE 'AL90%'
                                    THEN L.METERNO
                                END
                            ) AS PHASE_3PH,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE 'AL91%'
                                    OR L.METERNO LIKE '9008%'
                                    OR L.METERNO LIKE '9027%'
                                    THEN L.METERNO
                                END
                            ) AS ALLIED_1PH,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE 'AL90%'
                                    THEN L.METERNO
                                END
                            ) AS ALLIED_3PH,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE 'KI91%'
                                      OR L.METERNO LIKE '9150%'
                                    THEN L.METERNO
                                END
                            ) AS KIMBAL_1PH,
                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE 'KI90%'
                                      OR L.METERNO LIKE '9026%'
                                    THEN L.METERNO
                                END
                            ) AS KIMBAL_3PH
                        FROM RCMPA.SMART_METER_SCHEDULER_LOGS L
                        WHERE
                          (
                                L.METERNO LIKE '91______'
                             OR L.METERNO LIKE '90______'
                             OR L.METERNO LIKE 'AL________'
                             OR L.METERNO LIKE 'KI________'
                          )
                          AND L.MESSAGE NOT LIKE 'Data%'
                          AND L.READING_MONTH IN
                          (
                              SELECT M.READING_MONTH
                              FROM MONTHS M
                          )
                          AND NOT EXISTS
                          (
                              SELECT /*+ INDEX(B IDX_SM_BILLING_CONSREF_MONTH) */
                                     1
                              FROM RCMPA.SMART_METER_BILLING_DATA B
                              WHERE B.CONS_REF = L.CONS_REF
                                AND B.READING_MONTH = L.READING_MONTH
                          )
                        GROUP BY L.READING_MONTH
                    )
                )
                SELECT
                    D.DOWNLOAD_COUNT + F.FAILED_COUNT AS TOTALMETERS,
                    D.ALLIED_DOWNLOAD + F.ALLIED_FAILED AS ALLIEDCOUNT,
                    D.KIMBAL_DOWNLOAD + F.KIMBAL_FAILED AS KIMBALCOUNT,
                    D.PHASE_1PH + F.PHASE_1PH AS PHASE_1PH,
                    D.PHASE_3PH + F.PHASE_3PH AS PHASE_3PH,
                    D.ALLIED_1PH + F.ALLIED_1PH AS ALLIED_1PH,
                    D.ALLIED_3PH + F.ALLIED_3PH AS ALLIED_3PH,
                    D.KIMBAL_1PH + F.KIMBAL_1PH AS KIMBAL_1PH,
                    D.KIMBAL_3PH + F.KIMBAL_3PH AS KIMBAL_3PH
                FROM DOWNLOAD D
                CROSS JOIN FAILED F";
              

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
                            Summary.Allied_1PhCount = dr["ALLIED_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_1PH"]);
                            Summary.Allied_3PhCount = dr["ALLIED_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_3PH"]);
                            Summary.Kimbal_1PhCount = dr["KIMBAL_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_1PH"]);
                            Summary.kimbal_3PhCount = dr["KIMBAL_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_3PH"]);
                            Summary.total_1PhCount = dr["PHASE_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PHASE_1PH"]);
                            Summary.total_3PhCount = dr["PHASE_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PHASE_3PH"]);
                        }
                    }
                }
            }
            return Summary;
        }
        //BYPL
        public TotalMeterSummaryBypl GetByplTotalMeterSummary(string ReadingMonth)
        {
            TotalMeterSummaryBypl totalMeterSummary = new TotalMeterSummaryBypl();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                    (
                        SELECT TRIM(
                                   REGEXP_SUBSTR(
                                       :READING_MONTH,
                                       '[^,]+',
                                       1,
                                       LEVEL
                                   )
                               )
                        FROM DUAL
                        CONNECT BY REGEXP_SUBSTR(
                                       :READING_MONTH,
                                       '[^,]+',
                                       1,
                                       LEVEL
                                   ) IS NOT NULL
                    ),

                    ALL_LOG_DATA AS
                    (
                        SELECT /*+ PARALLEL(S,8) */
                               TRIM(S.METERNO) AS METERNO,
                               TRIM(S.BILL_MONTH) AS BILL_MONTH,
                               S.IS_FAILED
                        FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                        WHERE S.BILL_MONTH IN
                        (
                            SELECT READING_MONTH
                            FROM MONTHS
                        )

                        UNION

                        SELECT /*+ PARALLEL(K,8) */
                               TRIM(K.METERNO) AS METERNO,
                               TRIM(K.BILL_MONTH) AS BILL_MONTH,
                               K.IS_FAILED
                        FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                        WHERE K.BILL_MONTH IN
                        (
                            SELECT READING_MONTH
                            FROM MONTHS
                        )
                    ),

                    DISTINCT_DATA AS
                    (
                        SELECT DISTINCT
                               METERNO,
                               BILL_MONTH,
                               IS_FAILED
                        FROM ALL_LOG_DATA
                        WHERE METERNO IS NOT NULL
                    ),

                    SUMMARY AS
                    (
                        SELECT

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                    THEN METERNO
                                END
                            ) AS TOTAL_METERS,

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                     AND (
                                            METERNO LIKE '92%'
                                         OR METERNO LIKE 'AL%'
                                         OR METERNO LIKE '99%'
                                     )
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                     AND (
                                            METERNO LIKE '92%'
                                         OR METERNO LIKE 'AL%'
                                         OR METERNO LIKE '99%'
                                     )
                                    THEN METERNO
                                END
                            ) AS ALLIED_COUNT,

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                     AND (
                                            METERNO LIKE '92%'
                                         OR METERNO LIKE '99%'
                                         OR METERNO LIKE 'AL92%'
                                         OR METERNO LIKE 'AL99%'
                                     )
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                     AND (
                                            METERNO LIKE '92%'
                                         OR METERNO LIKE '99%'
                                         OR METERNO LIKE 'AL92%'
                                         OR METERNO LIKE 'AL99%'
                                     )
                                    THEN METERNO
                                END
                            ) AS ALLIED_1PH,

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                     AND METERNO LIKE 'AL97%'
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                     AND METERNO LIKE 'AL97%'
                                    THEN METERNO
                                END
                            ) AS ALLIED_3PH,

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                     AND (
                                            METERNO LIKE 'KI%'
                                         OR METERNO LIKE '97%'
                                         OR METERNO LIKE '98%'
                                     )
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                     AND (
                                            METERNO LIKE 'KI%'
                                         OR METERNO LIKE '97%'
                                         OR METERNO LIKE '98%'
                                     )
                                    THEN METERNO
                                END
                            ) AS KIMBAL_COUNT,

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                     AND (
                                            METERNO LIKE '98%'
                                         OR METERNO LIKE 'KI98%'
                                     )
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                     AND (
                                            METERNO LIKE '98%'
                                         OR METERNO LIKE 'KI98%'
                                     )
                                    THEN METERNO
                                END
                            ) AS KIMBAL_1PH,

                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                     AND (
                                            METERNO LIKE '97%'
                                         OR METERNO LIKE 'KI97%'
                                     )
                                    THEN METERNO
                                END
                            )
                            +
                            COUNT(DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                     AND (
                                            METERNO LIKE '97%'
                                         OR METERNO LIKE 'KI97%'
                                     )
                                    THEN METERNO
                                END
                            ) AS KIMBAL_3PH

                        FROM DISTINCT_DATA
                    )

                    SELECT
                        TOTAL_METERS,
                        ALLIED_COUNT,
                        ALLIED_1PH,
                        ALLIED_3PH,
                        KIMBAL_COUNT,
                        KIMBAL_1PH,
                        KIMBAL_3PH
                    FROM SUMMARY";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            totalMeterSummary.TotalMeter = dr["TOTAL_METERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_METERS"]);
                            totalMeterSummary.AlliedCount = dr["ALLIED_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_COUNT"]);
                            totalMeterSummary.Allied_1PhCount = dr["ALLIED_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_1PH"]);
                            totalMeterSummary.Allied_3PhCount = dr["ALLIED_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_3PH"]);
                            totalMeterSummary.KimbalCount = dr["KIMBAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_COUNT"]);
                            totalMeterSummary.Kimbal_1PhCount = dr["KIMBAL_1PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_1PH"]);
                            totalMeterSummary.Kimbal_3PhCount = dr["KIMBAL_3PH"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_3PH"]);
                        }
                    }
                }
            } return totalMeterSummary;
        }


        // Get Meter Download Summary Allied + Kimbal including all department for the current month till day - 1
        public MeterReceivedSummary GetMeterReceivedDownloadSummary(string ReadingMonth)
        {
            MeterReceivedSummary Summary = new MeterReceivedSummary();

            using(OracleConnection con  = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                (
                    SELECT REGEXP_SUBSTR(
                               :READING_MONTH,
                               '[^,]+',
                               1,
                               LEVEL
                           )
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR(
                                   :READING_MONTH,
                                   '[^,]+',
                                   1,
                                   LEVEL
                               ) IS NOT NULL
                ),

                DOWNLOAD AS
                (
                    SELECT /*+ PARALLEL(SM,8) */
                           COUNT(*) AS HES_DOWNLOAD

                    FROM RCMPA.SMART_METER_BILLING_DATA SM

                    WHERE
                    (
                           SM.METERNO LIKE '91______'
                        OR SM.METERNO LIKE '90______'
                        OR SM.METERNO LIKE 'AL________'
                        OR SM.METERNO LIKE 'KI________'
                    )

                    AND SM.READING_MONTH IN
                    (
                        SELECT M.READING_MONTH
                        FROM MONTHS M
                    )
                ),

                FAILED AS
                (
                    SELECT
                        SUM(HES_FAILED) AS HES_FAILED

                    FROM
                    (
                        SELECT /*+ PARALLEL(L,8) */

                            L.READING_MONTH,

                            COUNT(DISTINCT L.METERNO) AS HES_FAILED

                        FROM RCMPA.SMART_METER_SCHEDULER_LOGS L

                        WHERE
                        (
                               L.METERNO LIKE '91______'
                            OR L.METERNO LIKE '90______'
                            OR L.METERNO LIKE 'AL________'
                            OR L.METERNO LIKE 'KI________'
                        )

                        AND L.MESSAGE NOT LIKE 'Data%'

                        AND L.READING_MONTH IN
                        (
                            SELECT M.READING_MONTH
                            FROM MONTHS M
                        )

                        AND NOT EXISTS
                        (
                            SELECT /*+ INDEX(B IDX_SM_BILLING_CONSREF_MONTH) */
                                   1

                            FROM RCMPA.SMART_METER_BILLING_DATA B

                            WHERE B.CONS_REF = L.CONS_REF
                              AND B.READING_MONTH = L.READING_MONTH
                        )

                        GROUP BY L.READING_MONTH
                    )
                )

                SELECT /*+ PARALLEL(8) */

                       (D.HES_DOWNLOAD + F.HES_FAILED) AS TOTAL_METERS,

                       D.HES_DOWNLOAD,

                       F.HES_FAILED,

                       ROUND(
                           D.HES_DOWNLOAD * 100 /
                           NULLIF(
                               D.HES_DOWNLOAD + F.HES_FAILED,
                               0
                           ),
                           2
                       ) AS HES_DOWNLOAD_PERCENTAGE,

                       ROUND(
                           F.HES_FAILED * 100 /
                           NULLIF(
                               D.HES_DOWNLOAD + F.HES_FAILED,
                               0
                           ),
                           2
                       ) AS HES_FAILED_PERCENTAGE

                FROM DOWNLOAD D
                CROSS JOIN FAILED F";


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
        //BYPL
        public List<MeterReceivedSummaryBypl> GetMeterReceivedSummaryBypl(string readingMonth)
        {
            List<MeterReceivedSummaryBypl> meterReceivedSummaryBypl = new List<MeterReceivedSummaryBypl>();
            using(OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                    (
                        SELECT TRIM(
                                   REGEXP_SUBSTR(
                                       :READING_MONTH,
                                       '[^,]+',
                                       1,
                                       LEVEL
                                   )
                               )
                        FROM DUAL
                        CONNECT BY REGEXP_SUBSTR(
                                       :READING_MONTH,
                                       '[^,]+',
                                       1,
                                       LEVEL
                                   ) IS NOT NULL
                    ),
                    SLCC_DATA AS
                    (
                        SELECT /*+ PARALLEL(S,8) */
                               TRIM(S.METERNO) AS METERNO,
                               TRIM(S.BILL_MONTH) AS BILL_MONTH,
                               S.IS_FAILED
                        FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                        WHERE S.BILL_MONTH IN
                        (
                            SELECT M.READING_MONTH
                            FROM MONTHS M
                        )
                    ),
                    KCC_GCC_DATA AS
                    (
                        SELECT /*+ PARALLEL(K,8) */
                               TRIM(K.METERNO) AS METERNO,
                               TRIM(K.BILL_MONTH) AS BILL_MONTH,
                               K.IS_FAILED
                        FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                        WHERE K.BILL_MONTH IN
                        (
                            SELECT M.READING_MONTH
                            FROM MONTHS M
                        )
                    ),
                    BASE_DATA AS
                    (
                        SELECT
                               METERNO,
                               BILL_MONTH,
                               IS_FAILED
                        FROM SLCC_DATA

                        UNION

                        SELECT
                               METERNO,
                               BILL_MONTH,
                               IS_FAILED
                        FROM KCC_GCC_DATA
                    ),
                    DISTINCT_DATA AS
                    (
                        SELECT DISTINCT
                               METERNO,
                               BILL_MONTH,
                               IS_FAILED
                        FROM BASE_DATA
                        WHERE METERNO IS NOT NULL
                    ),
                    SUMMARY AS
                    (
                        SELECT /*+ PARALLEL(8) */

                               /* Downloaded */
                               COUNT(
                                   DISTINCT
                                   CASE
                                       WHEN IS_FAILED = 0
                                       THEN METERNO
                                   END
                               ) AS HES_DOWNLOAD,

                               /* Failed */
                               COUNT(
                                   DISTINCT
                                   CASE
                                       WHEN IS_FAILED = 1
                                       THEN METERNO
                                   END
                               ) AS HES_FAILED

                        FROM DISTINCT_DATA
                    )
                    SELECT
                        HES_DOWNLOAD,
                        HES_FAILED,
                        (
                            HES_DOWNLOAD
                            + HES_FAILED
                        ) AS TOTAL,
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
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = readingMonth;
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
                } return meterReceivedSummaryBypl;
            }
        }

        public List<MeterDownloadDetailedSummary> MeterDetailedSummary(string ReadingMonth)
        {
            List<MeterDownloadDetailedSummary> SummaryList = new List<MeterDownloadDetailedSummary>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH LATEST_FAILURE AS
                    (
                        SELECT
                            METERNO,
                            CONS_REF,
                            MESSAGE,
                            ENTRY_DATE,
                            READING_MONTH,
                            SAP_DEPARTMENT,
                            CYCLE,

                            ROW_NUMBER() OVER
                            (
                                PARTITION BY METERNO, READING_MONTH
                                ORDER BY ENTRY_DATE DESC
                            ) AS RN

                        FROM RCMPA.SMART_METER_SCHEDULER_LOGS

                        WHERE MESSAGE NOT LIKE 'Data%'

                          AND READING_MONTH IN
                          (
                              SELECT TRIM(
                                         REGEXP_SUBSTR(
                                             :READING_MONTH,
                                             '[^,]+',
                                             1,
                                             LEVEL
                                         )
                                     )
                              FROM DUAL
                              CONNECT BY REGEXP_SUBSTR(
                                             :READING_MONTH,
                                             '[^,]+',
                                             1,
                                             LEVEL
                                         ) IS NOT NULL
                          )

                          AND
                          (
                                (SUBSTR(METERNO, 1, 2) = '91'
                                 AND LENGTH(METERNO) = 8)

                             OR (SUBSTR(METERNO, 1, 2) = '90'
                                 AND LENGTH(METERNO) = 8)

                             OR (SUBSTR(METERNO, 1, 2) = 'AL'
                                 AND LENGTH(METERNO) = 10)
                             OR (SUBSTR(METERNO, 1, 2) = 'KI'
                                 AND LENGTH(METERNO) = 10)
                          )
                    ),

                    FAILURE_DATA AS
                    (
                        SELECT DISTINCT
                            L.METERNO,
                            L.CONS_REF,
                            L.MESSAGE,
                            L.ENTRY_DATE,
                            L.READING_MONTH,
                            L.SAP_DEPARTMENT,
                            L.CYCLE,

                            CASE
                                WHEN UPPER(L.MESSAGE) LIKE '%SYSTEM TITLE%'
                                    THEN 'System Title Mismatch'

                                WHEN UPPER(L.MESSAGE) LIKE '%TCP%'
                                    THEN 'TCP Connection Failed'

                                WHEN UPPER(L.MESSAGE) LIKE '%NO DATA%'
                                    THEN 'No Data Found'

                                ELSE 'Others Failure Reason'
                            END AS FAILURE_REASON

                        FROM LATEST_FAILURE L

                        WHERE L.RN = 1

                          AND NOT EXISTS
                          (
                              SELECT 1
                              FROM RCMPA.SMART_METER_BILLING_DATA B
                              WHERE B.CONS_REF = L.CONS_REF
                                AND B.READING_MONTH = L.READING_MONTH
                          )
                    )

                   SELECT DISTINCT
                    F.METERNO,
                    F.CONS_REF,

                    CASE
                        WHEN F.SAP_DEPARTMENT = 'MLCC'
                             AND F.CYCLE = '0N'
                            THEN 'KCC'
                        ELSE NVL(F.SAP_DEPARTMENT, 'SLCC')
                    END AS SAP_DEPARTMENT,

                    NVL(S.SAP_DIVISION, FM.SAP_DIVISION) AS SAP_DIVISION,

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
                    END AS METER_TYPE,
                    CASE
                    WHEN SUBSTR(F.METERNO, 1, 4) = 'AL91'
                        THEN '1PH'
                    WHEN SUBSTR(F.METERNO, 1, 4) = 'AL90'
                        THEN '3PH'
                    WHEN SUBSTR(F.METERNO, 1, 4) = 'KI91'
                        THEN '1PH'
                    WHEN SUBSTR(F.METERNO, 1, 4) = '9150'
                        THEN '1PH'    
                    WHEN SUBSTR(F.METERNO, 1, 4) = '9008'
                        THEN '1PH'       
                    WHEN SUBSTR(F.METERNO, 1, 4) = '9027'
                        THEN '1PH'
                    WHEN SUBSTR(F.METERNO, 1, 4) = 'KI90'
                        THEN '3PH'
                    WHEN SUBSTR(F.METERNO, 1, 4) = '9026'
                        THEN '3PH'
                    ELSE 'UNKNOWN'
                END AS PHASE_TYPE,
                    F.FAILURE_REASON,
                    F.MESSAGE AS SCHEDULER_MESSAGE,
                    F.ENTRY_DATE,
                    F.READING_MONTH
                FROM FAILURE_DATA F
                LEFT JOIN RCMPA.SAP_SLCC_FORMY S
                    ON S.CONS_REF = F.CONS_REF
                    AND S.READING_MONTH = F.READING_MONTH
                LEFT JOIN RCMPA.SAP_FORMY FM
                    ON FM.CONS_REF = F.CONS_REF
                    AND FM.READING_MONTH = F.READING_MONTH
                ORDER BY
                    CASE
                        WHEN F.SAP_DEPARTMENT = 'MLCC'
                             AND F.CYCLE = '0N'
                            THEN 'KCC'
                        ELSE NVL(F.SAP_DEPARTMENT, 'SLCC')
                    END,

                    NVL(S.SAP_DIVISION, FM.SAP_DIVISION),
                    NVL(S.SAP_SEQ_NO, FM.SAP_SEQ_NO),
                    F.METERNO";

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
                                Phase = dr["PHASE_TYPE"].ToString(),
                                ConsRef = dr["CONS_REF"].ToString(),
                                SapDivision = dr["SAP_DIVISION"].ToString(),
                                Address = dr["ADDRESS"].ToString(),
                                SapSeqNo = dr["SAP_SEQ_NO"].ToString(),
                                SchedulerMessage = dr["SCHEDULER_MESSAGE"] == DBNull.Value ? null : dr["SCHEDULER_MESSAGE"].ToString(),
                                EntryDate = dr["ENTRY_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["ENTRY_DATE"])

                            });
                        }
                    }
                }
                return SummaryList;
            }
        
        }

        public List<MeterDownloadDetailedSummaryBypl> GetMeterDownloadDetailedSummaryBypl(string readingMonth)
        {
            List<MeterDownloadDetailedSummaryBypl> summaryList = new List<MeterDownloadDetailedSummaryBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH
                MONTHS AS
                (
                    SELECT /*+ MATERIALIZE */ TRIM( REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1, LEVEL )) AS READING_MONTH
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR( :READING_MONTH,'[^,]+', 1, LEVEL ) IS NOT NULL
                ),
                SLCC_FAILED AS
                (
                    SELECT
                        METERNO,
                        READING_MONTH,
                        INSERTED_DATE,
                        REMARKS,
                        NVL(DEPARTMENT, 'SLCC') AS DEPARTMENT
                    FROM
                    (
                        SELECT
                            /*+
                                LEADING(M S)
                                USE_NL(S)
                                INDEX(S IDX_SLCC_SM_LOG_FAIL)
                            */
                            S.METERNO,
                            S.BILL_MONTH AS READING_MONTH,
                            S.INSERTED_DATE,
                            S.REMARKS,
                            S.DEPARTMENT,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY S.METERNO, S.BILL_MONTH
                                ORDER BY S.INSERTED_DATE DESC NULLS LAST
                            ) AS RN
                        FROM MONTHS M
                        JOIN RCMPA.SAP_SLCC_SMARTMETER_LOG S
                          ON S.BILL_MONTH = M.READING_MONTH

                        WHERE S.IS_FAILED = '1'
                          AND S.METERNO IS NOT NULL
                    )
                    WHERE RN = 1
                ),
                KCC_FAILED AS
                (
                    SELECT
                        METERNO,
                        READING_MONTH,
                        INSERTED_DATE,
                        REMARKS,
                        DEPARTMENT
                    FROM
                    (
                        SELECT
                            /*+
                                LEADING(M K)
                                USE_NL(K)
                                INDEX(K IDX_KCC_SM_LOG_FAIL)
                            */
                            K.METERNO,
                            K.BILL_MONTH AS READING_MONTH,
                            K.INSERTED_DATE,
                            K.REMARKS,
                            K.DEPARTMENT,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY K.METERNO, K.BILL_MONTH
                                ORDER BY K.INSERTED_DATE DESC NULLS LAST
                            ) AS RN
                        FROM MONTHS M
                        JOIN RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                          ON K.BILL_MONTH = M.READING_MONTH

                        WHERE K.IS_FAILED = '1'
                          AND K.METERNO IS NOT NULL
                    )
                    WHERE RN = 1
                ),
                SLCC_METERS AS
                (
                    SELECT /*+ MATERIALIZE */
                           DISTINCT METERNO
                    FROM SLCC_FAILED
                ),
                KCC_METERS AS
                (
                    SELECT /*+ MATERIALIZE */
                           DISTINCT METERNO
                    FROM KCC_FAILED
                ),
                SLCC_FORMY_FALLBACK AS
                (
                    SELECT METERNO, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2, ADD3, LAND_MARK, FATHER_NAME
                    FROM
                    (
                        SELECT
                            /*+
                                LEADING(M F)
                                USE_NL(F)
                                INDEX(F IDX_SLCC_FORMY_MTR_MONTH)
                            */
                            F.METERNO,
                            F.CONS_REF,
                            F.SAP_DIVISION,
                            F.SAP_SEQ_NO,
                            F.ADD1,
                            F.ADD2,
                            F.ADD3,
                            F.LAND_MARK,
                            F.FATHER_NAME,

                            ROW_NUMBER() OVER
                            (
                                PARTITION BY F.METERNO
                                ORDER BY F.READING_MONTH DESC
                            ) AS RN

                        FROM SLCC_METERS M
                        JOIN RCMPA.SAP_SLCC_FORMY F
                          ON F.METERNO = M.METERNO
                    )
                    WHERE RN = 1
                ),
                SAP_FORMY_FALLBACK AS
                (
                    SELECT METERNO, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, ADD1, ADD2,  ADD3, LAND_MARK, FATHER_NAME
                    FROM
                    (
                        SELECT
                            /*+
                                LEADING(M F)
                                USE_NL(F)
                                INDEX(F IDX_SAP_FORMY_MTR_MONTH)
                            */
                            F.METERNO,
                            F.CONS_REF,
                            F.SAP_DIVISION,
                            F.SAP_SEQ_NO,
                            F.ADD1,
                            F.ADD2,
                            F.ADD3,
                            F.LAND_MARK,
                            F.FATHER_NAME,

                            ROW_NUMBER() OVER
                            (
                                PARTITION BY F.METERNO
                                ORDER BY F.READING_MONTH DESC
                            ) AS RN

                        FROM KCC_METERS M
                        JOIN RCMPA.SAP_FORMY F
                          ON F.METERNO = M.METERNO
                    )
                    WHERE RN = 1
                ),
                SLCC_DATA AS
                (
                    SELECT
                        /*+
                            LEADING(D)
                            USE_NL(FM FL)
                            INDEX(FM IDX_SLCC_FORMY_MTR_MONTH)
                            INDEX(FL IDX_SLCC_FORMY_MTR_MONTH)
                        */
                        D.METERNO,
                        D.READING_MONTH,
                        D.DEPARTMENT,
                        D.REMARKS,
                        D.INSERTED_DATE,
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
                    FROM SLCC_FAILED D
                    LEFT JOIN RCMPA.SAP_SLCC_FORMY FM
                      ON FM.METERNO = D.METERNO
                     AND FM.READING_MONTH = D.READING_MONTH
                    LEFT JOIN SLCC_FORMY_FALLBACK FL
                      ON FL.METERNO = D.METERNO
                ),
                KCC_DATA AS
                (
                    SELECT
                        /*+
                            LEADING(D)
                            USE_NL(FM FL)
                            INDEX(FM IDX_SAP_FORMY_MTR_MONTH)
                            INDEX(FL IDX_SAP_FORMY_MTR_MONTH)
                        */
                        D.METERNO,
                        D.READING_MONTH,
                        D.DEPARTMENT,
                        D.REMARKS,
                        D.INSERTED_DATE,

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

                    FROM KCC_FAILED D
                    LEFT JOIN RCMPA.SAP_FORMY FM
                      ON FM.METERNO = D.METERNO
                     AND FM.READING_MONTH = D.READING_MONTH

                    LEFT JOIN SAP_FORMY_FALLBACK FL
                      ON FL.METERNO = D.METERNO
                ),
                FINAL_DATA AS
                (
                    SELECT METERNO, READING_MONTH, DEPARTMENT, REMARKS, INSERTED_DATE, CONS_REF, SAP_DIVISION, SAP_SEQ_NO, 
                    ADD1, ADD2, ADD3, LAND_MARK, FATHER_NAME
                    FROM SLCC_DATA
                    UNION ALL
                    SELECT METERNO, READING_MONTH, DEPARTMENT,REMARKS, INSERTED_DATE,CONS_REF, SAP_DIVISION, SAP_SEQ_NO,
                    ADD1, ADD2, ADD3,  LAND_MARK, FATHER_NAME
                    FROM KCC_DATA
                )
                SELECT
                    D.METERNO,
                    D.CONS_REF,
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
                    D.DEPARTMENT AS SAP_DEPARTMENT,
                    D.SAP_DIVISION,
                    D.SAP_SEQ_NO,
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
                    END AS METER_TYPE,
                    D.REMARKS AS FAILURE_REASON,
                    D.INSERTED_DATE
                FROM FINAL_DATA D
                ORDER BY
                    D.INSERTED_DATE DESC NULLS LAST,
                    D.METERNO";

                using(OracleCommand cmd = new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = readingMonth;

                    using(OracleDataReader dr = cmd.ExecuteReader())
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
                                EntryDate = dr["INSERTED_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["INSERTED_DATE"])
                            });
                        }
                    }
                    return summaryList;
                }
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
                string query = @"WITH MONTHS (READING_MONTH) AS
                    (
                        SELECT TRIM(
                                   REGEXP_SUBSTR(
                                       :READING_MONTH,
                                       '[^,]+',
                                       1,
                                       LEVEL
                                   )
                               )
                        FROM DUAL
                        CONNECT BY REGEXP_SUBSTR(
                                       :READING_MONTH,
                                       '[^,]+',
                                       1,
                                       LEVEL
                                   ) IS NOT NULL
                    ),

                    DOWNLOAD AS
                    (
                        SELECT /*+ PARALLEL(B,8) */

                               CASE
                                   WHEN B.SAP_DEPARTMENT = 'MLCC'
                                        AND B.CYCLE = '0N'
                                       THEN 'KCC'

                                   WHEN B.CYCLE IN ('KA', 'KC', 'KG')
                                       THEN 'KCC'

                                   WHEN B.SAP_DEPARTMENT IS NULL
                                       THEN 'SLCC'

                                   ELSE B.SAP_DEPARTMENT
                               END AS DEPARTMENT,

                               COUNT(*) AS HES_DOWNLOAD

                        FROM RCMPA.SMART_METER_BILLING_DATA B

                        WHERE
                        (
                               B.METERNO LIKE '91______'
                            OR B.METERNO LIKE '90______'
                            OR B.METERNO LIKE 'AL________'
                            OR B.METERNO LIKE 'KI________'
                        )

                        AND B.READING_MONTH IN
                        (
                            SELECT M.READING_MONTH
                            FROM MONTHS M
                        )

                        GROUP BY
                            CASE
                                WHEN B.SAP_DEPARTMENT = 'MLCC'
                                     AND B.CYCLE = '0N'
                                    THEN 'KCC'

                                WHEN B.CYCLE IN ('KA', 'KC', 'KG')
                                    THEN 'KCC'

                                WHEN B.SAP_DEPARTMENT IS NULL
                                    THEN 'SLCC'

                                ELSE B.SAP_DEPARTMENT
                            END
                    ),

                    FAILED AS
                    (
                        SELECT
                            DEPARTMENT,
                            COUNT(*) AS FAILED

                        FROM
                        (
                            SELECT /*+ PARALLEL(L,8) */

                                   L.METERNO,
                                   L.CONS_REF,
                                   L.READING_MONTH,

                                   MAX(
                                       CASE
                                           WHEN L.SAP_DEPARTMENT = 'MLCC'
                                                AND L.CYCLE = '0N'
                                               THEN 'KCC'

                                           WHEN L.CYCLE IN ('KA', 'KC', 'KG')
                                               THEN 'KCC'

                                           WHEN L.SAP_DEPARTMENT IS NULL
                                               THEN 'SLCC'

                                           ELSE L.SAP_DEPARTMENT
                                       END
                                   ) AS DEPARTMENT

                            FROM RCMPA.SMART_METER_SCHEDULER_LOGS L

                            WHERE
                            (
                                   L.METERNO LIKE '91______'
                                OR L.METERNO LIKE '90______'
                                OR L.METERNO LIKE 'AL________'
                                OR L.METERNO LIKE 'KI________'
                            )

                            AND L.MESSAGE NOT LIKE 'Data%'

                            AND L.READING_MONTH IN
                            (
                                SELECT M.READING_MONTH
                                FROM MONTHS M
                            )

                            AND NOT EXISTS
                            (
                                SELECT /*+ INDEX(B IDX_BILLING_CONSREF_MONTH) */
                                       1

                                FROM RCMPA.SMART_METER_BILLING_DATA B

                                WHERE B.CONS_REF = L.CONS_REF
                                  AND B.READING_MONTH = L.READING_MONTH
                            )

                            GROUP BY
                                L.METERNO,
                                L.CONS_REF,
                                L.READING_MONTH
                        )

                        GROUP BY DEPARTMENT
                    )

                    SELECT /*+ PARALLEL(8) */

                           COALESCE(
                               D.DEPARTMENT,
                               F.DEPARTMENT
                           ) AS DEPARTMENT,

                           NVL(
                               D.HES_DOWNLOAD,
                               0
                           ) AS HESDOWNLOAD,

                           NVL(
                               F.FAILED,
                               0
                           ) AS FAILED

                    FROM DOWNLOAD D

                    FULL OUTER JOIN FAILED F
                        ON D.DEPARTMENT = F.DEPARTMENT

                    ORDER BY DEPARTMENT";
                using(OracleCommand cmd = new OracleCommand(query,conn))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    //Console.WriteLine(query);
                    //Console.WriteLine($"READING_MONTH = {ReadingMonth}");
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

        //BYPL
        public List<DepartmentWiseSummaryBypl> GetDepartmentWiseSummaryBypl(string ReadingMonth)
        {
            List<DepartmentWiseSummaryBypl> departmentWiseSummarieBypl = new List<DepartmentWiseSummaryBypl>();
            using(OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                     (
                      SELECT TRIM(REGEXP_SUBSTR( :READING_MONTH,'[^,]+',1,LEVEL ))          
                      FROM DUAL CONNECT BY REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1, LEVEL ) IS NOT NULL
                    ),
                    SLCC_DATA AS
                    (
                        SELECT /*+ PARALLEL(S,8) */
                               TRIM(S.METERNO) AS METERNO,
                               TRIM(S.BILL_MONTH) AS BILL_MONTH,
                               S.IS_FAILED,
                               NVL(TRIM(S.DEPARTMENT), 'SLCC') AS DEPARTMENT
                        FROM RCMPA.SAP_SLCC_SMARTMETER_LOG S
                        WHERE S.BILL_MONTH IN
                        (
                            SELECT M.READING_MONTH
                            FROM MONTHS M
                        )
                    ),
                    KCC_GCC_DATA AS
                    (
                        SELECT /*+ PARALLEL(K,8) */
                               TRIM(K.METERNO) AS METERNO,
                               TRIM(K.BILL_MONTH) AS BILL_MONTH,
                               K.IS_FAILED,
                               NVL(TRIM(K.DEPARTMENT), 'KCC/GCC') AS DEPARTMENT
                        FROM RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                        WHERE K.BILL_MONTH IN
                        (
                            SELECT M.READING_MONTH
                            FROM MONTHS M
                        )
                    ),
                    BASE_DATA AS
                    (
                        SELECT METERNO, BILL_MONTH, IS_FAILED, DEPARTMENT
                        FROM SLCC_DATA
                        UNION
                        SELECT METERNO, BILL_MONTH, IS_FAILED, DEPARTMENT
                        FROM KCC_GCC_DATA
                    ),
                    DISTINCT_DATA AS
                    (
                        SELECT DISTINCT METERNO,BILL_MONTH, IS_FAILED, DEPARTMENT
                        FROM BASE_DATA
                        WHERE METERNO IS NOT NULL
                    ),
                    SUMMARY AS
                    (
                        SELECT
                            DEPARTMENT,
                            /* Downloaded */
                            COUNT(
                                DISTINCT
                                CASE
                                    WHEN IS_FAILED = 0
                                    THEN METERNO
                                END
                            ) AS HES_DOWNLOAD,
                            /* Failed */
                            COUNT(
                                DISTINCT
                                CASE
                                    WHEN IS_FAILED = 1
                                    THEN METERNO
                                END
                            ) AS HES_FAILED
                        FROM DISTINCT_DATA
                        GROUP BY DEPARTMENT
                    )
                    SELECT
                        DEPARTMENT,
                        HES_DOWNLOAD,
                        HES_FAILED,
                        (HES_DOWNLOAD + HES_FAILED) AS TOTAL,
                        ROUND( HES_DOWNLOAD * 100 / NULLIF( HES_DOWNLOAD + HES_FAILED, 0),2) AS DOWNLOAD_PERCENTAGE,
                        ROUND( HES_FAILED * 100 / NULLIF( HES_DOWNLOAD + HES_FAILED, 0 ),2) AS FAILED_PERCENTAGE
                    FROM SUMMARY
                    ORDER BY DEPARTMENT";

                using(OracleCommand cmd = new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using(OracleDataReader dr = cmd.ExecuteReader())
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


        public List<FailureReasonCount> FailureReasonCounts(string ReadingMonth)
        {
            List<FailureReasonCount> failureReasonCounts = new List<FailureReasonCount>();
            using(OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH MONTHS (READING_MONTH) AS
                (
                    SELECT TRIM(
                               REGEXP_SUBSTR(
                                   :READING_MONTH,
                                   '[^,]+',
                                   1,
                                   LEVEL
                               )
                           )
                    FROM DUAL
                    CONNECT BY REGEXP_SUBSTR(
                                   :READING_MONTH,
                                   '[^,]+',
                                   1,
                                   LEVEL
                               ) IS NOT NULL
                ),

                LATEST_FAILURE AS
                (
                    SELECT /*+
                               FULL(L)
                               PARALLEL(L,8)
                            */
                           L.METERNO,
                           L.CONS_REF,
                           L.MESSAGE,
                           L.ENTRY_DATE,
                           L.READING_MONTH,

                           ROW_NUMBER() OVER
                           (
                               PARTITION BY L.METERNO, L.READING_MONTH
                               ORDER BY L.ENTRY_DATE DESC
                           ) AS RN

                    FROM RCMPA.SMART_METER_SCHEDULER_LOGS L

                    WHERE L.READING_MONTH IN
                    (
                        SELECT M.READING_MONTH
                        FROM MONTHS M
                    )

                    AND L.MESSAGE NOT LIKE 'Data%'
                ),

                FAILURE_DATA AS
                (
                    SELECT
                        CASE
                            WHEN UPPER(L.MESSAGE) LIKE '%SYSTEM TITLE%'
                                THEN 'System Title Mismatch'

                            WHEN UPPER(L.MESSAGE) LIKE '%TCP%'
                                THEN 'TCP Connection Failed'

                            WHEN UPPER(L.MESSAGE) LIKE '%NO DATA%'
                                THEN 'No Data Found'

                            ELSE 'Others Failure Reason'
                        END AS FAILURE_REASON

                    FROM LATEST_FAILURE L

                    WHERE L.RN = 1

                      AND NOT EXISTS
                      (
                          SELECT /*+ FULL(B) PARALLEL(B,8) */
                                 1
                          FROM RCMPA.SMART_METER_BILLING_DATA B

                          WHERE B.CONS_REF = L.CONS_REF
                            AND B.READING_MONTH = L.READING_MONTH
                      )
                )

                SELECT /*+ PARALLEL(8) */
                       FAILURE_REASON,
                       COUNT(*) AS TOTAL_COUNT

                FROM FAILURE_DATA

                GROUP BY FAILURE_REASON

                ORDER BY TOTAL_COUNT DESC";
                using(OracleCommand cmd = new OracleCommand(query,con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using(OracleDataReader dr = cmd.ExecuteReader())
                    {
                        while(dr.Read())
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


        public List<FailureReasonCountBypl> FailureReasonCountsBYPL(string ReadingMonth)
        {
            List<FailureReasonCountBypl> failureReasonCounts = new List<FailureReasonCountBypl>();
            using (OracleConnection con = _db.GetConnection())
            {
                con.Open();
                string query = @"WITH
                    MONTHS AS
                    (
                       SELECT /*+ MATERIALIZE */ TRIM( REGEXP_SUBSTR( :READING_MONTH, '[^,]+', 1, LEVEL )) AS READING_MONTH
                      FROM DUAL CONNECT BY REGEXP_SUBSTR( :READING_MONTH, '[^,]+',  1, LEVEL ) IS NOT NULL
                    ),
                    SLCC_FAILED AS
                    (
                        SELECT METERNO, READING_MONTH, REMARKS
                        FROM
                        (
                            SELECT /*+ LEADING(M S) USE_NL(S) INDEX(S IDX_SLCC_SM_LOG_FAIL) */
                                   S.METERNO,
                                   S.BILL_MONTH AS READING_MONTH,
                                   S.REMARKS,
                                   ROW_NUMBER() OVER
                                   (
                                       PARTITION BY S.METERNO, S.BILL_MONTH
                                       ORDER BY S.INSERTED_DATE DESC NULLS LAST
                                   ) AS RN
                            FROM MONTHS M
                            JOIN RCMPA.SAP_SLCC_SMARTMETER_LOG S
                            ON S.BILL_MONTH = M.READING_MONTH
                            WHERE S.IS_FAILED = '1'
                              AND S.METERNO IS NOT NULL
                        )
                        WHERE RN = 1
                    ),
                    KCC_FAILED AS
                    (
                        SELECT METERNO, READING_MONTH, REMARKS
                        FROM
                        (
                            SELECT /*+ LEADING(M K) USE_NL(K) INDEX(K IDX_KCC_SM_LOG_FAIL) */
                                   K.METERNO,
                                   K.BILL_MONTH AS READING_MONTH,
                                   K.REMARKS,

                                   ROW_NUMBER() OVER
                                   (
                                       PARTITION BY K.METERNO, K.BILL_MONTH
                                       ORDER BY K.INSERTED_DATE DESC NULLS LAST
                                   ) AS RN
                            FROM MONTHS M
                            JOIN RCMPA.SAP_KCC_GCC_SMARTMETER_LOG K
                            ON K.BILL_MONTH = M.READING_MONTH
                            WHERE K.IS_FAILED = '1'
                              AND K.METERNO IS NOT NULL
                        )
                        WHERE RN = 1
                    ),
                    FAILED_DATA AS
                    (
                        SELECT METERNO, READING_MONTH, REMARKS
                        FROM SLCC_FAILED
                        UNION ALL
                        SELECT METERNO, READING_MONTH, REMARKS
                        FROM KCC_FAILED
                    )
                    SELECT
                        CASE
                            WHEN REMARKS IS NULL
                                THEN 'Unknown Failure'
                            WHEN REMARKS LIKE '%Smart  Meter date  is older then FormY SAP_MRO_DOWNLOAD_DATE for the meter no%'
                                THEN 'Date older then FormY'
                            WHEN REMARKS LIKE '%Data not found in form_y%'
                                THEN 'No Data Found in FORMY'
                            WHEN REMARKS LIKE '%Data not found in HES%'
                                THEN 'No Data Found '
                            ELSE 'Others Failure Reason'
                        END AS FAILURE_REASON,
                        COUNT(*) AS TOTAL_COUNT
                    FROM FAILED_DATA
                    GROUP BY
                        CASE
                            WHEN REMARKS IS NULL
                                THEN 'Unknown Failure'
                            WHEN REMARKS LIKE '%Smart  Meter date  is older then FormY SAP_MRO_DOWNLOAD_DATE for the meter no%'
                                THEN 'Date older then FormY'
                            WHEN REMARKS LIKE '%Data not found in form_y%'
                                THEN 'No Data Found in FORMY'
                            WHEN REMARKS LIKE '%Data not found in HES%'
                                THEN 'No Data Found '
                            ELSE 'Others Failure Reason'
                        END
                    ORDER BY TOTAL_COUNT DESC";
                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
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
