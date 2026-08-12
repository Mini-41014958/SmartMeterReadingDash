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
                        SUM(KIMBAL_DOWNLOAD) AS KIMBAL_DOWNLOAD
                    FROM
                    (
                        SELECT
                            SM.READING_MONTH,

                            COUNT(*) AS DOWNLOAD_COUNT,

                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE '90%'
                                      OR SM.METERNO LIKE 'AL%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS ALLIED_DOWNLOAD,

                            SUM(
                                CASE
                                    WHEN SM.METERNO LIKE '91%'
                                      OR SM.METERNO LIKE 'KI%'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS KIMBAL_DOWNLOAD

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
                        SUM(KIMBAL_FAILED) AS KIMBAL_FAILED
                    FROM
                    (
                        SELECT
                            L.READING_MONTH,

                            COUNT(DISTINCT L.METERNO) AS FAILED_COUNT,

                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE '90%'
                                      OR L.METERNO LIKE 'AL%'
                                    THEN L.METERNO
                                END
                            ) AS ALLIED_FAILED,

                            COUNT(
                                DISTINCT CASE
                                    WHEN L.METERNO LIKE '91%'
                                      OR L.METERNO LIKE 'KI%'
                                    THEN L.METERNO
                                END
                            ) AS KIMBAL_FAILED

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

                    D.KIMBAL_DOWNLOAD + F.KIMBAL_FAILED AS KIMBALCOUNT

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
                        SAP_SLCC_DATA AS
                        (
                            SELECT /*+ PARALLEL(SF,8) */

                                COUNT(*) AS TOTAL_METERS,

                                SUM(
                                    CASE
                                        WHEN SF.METERNO LIKE '92%'
                                          OR SF.METERNO LIKE 'AL%'
                                          OR SF.METERNO LIKE '99%'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS ALLIED_COUNT,

                                SUM(
                                    CASE
                                        WHEN SF.METERNO LIKE 'KI%'
                                          OR SF.METERNO LIKE '97%'
                                          OR SF.METERNO LIKE '98%'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS KIMBAL_COUNT,

                                SUM(
                                    CASE
                                        WHEN SF.SAP_DEPARTMENT = 'SLCC'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS SLCC_COUNT,
                                0 AS MLCC_COUNT,
                                0 AS GCC_COUNT,
                                0 AS KCC_COUNT
                            FROM RCMPA.SAP_SLCC_FORMY SF
                            WHERE SF.SAP_COMPANY = 'BYPL'
                              AND SF.READING_MONTH IN
                              (
                                  SELECT M.READING_MONTH
                                  FROM MONTHS M
                              )
                              AND SF.SAP_MR_REASON_CODE = '01'
                              AND SF.CSTS_CD = 'R'
                              AND SF.METERNO NOT LIKE '%D%'
                              AND
                              (
                                    SF.METERNO LIKE '92______'
                                 OR SF.METERNO LIKE '99______'
                                 OR SF.METERNO LIKE '98______'
                                 OR SF.METERNO LIKE '97______'
                                 OR SF.METERNO LIKE 'AL________'
                                 OR SF.METERNO LIKE 'KI________'
                              )
                        ),
                        SAP_FORMY_DATA AS
                        (
                            SELECT /*+ PARALLEL(F,8) */
                                COUNT(*) AS TOTAL_METERS,
                                SUM(
                                    CASE
                                        WHEN F.METERNO LIKE '92%'
                                          OR F.METERNO LIKE 'AL%'
                                          OR F.METERNO LIKE '99%'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS ALLIED_COUNT,
                                SUM(
                                    CASE
                                        WHEN F.METERNO LIKE 'KI%'
                                          OR F.METERNO LIKE '97%'
                                          OR F.METERNO LIKE '98%'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS KIMBAL_COUNT,
                                SUM(
                                    CASE
                                        WHEN F.SAP_DEPARTMENT = 'SLCC'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS SLCC_COUNT,
                                SUM(
                                    CASE
                                        WHEN F.SAP_DEPARTMENT = 'MLCC'
                                         AND F.CYCLE <> '0N'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS MLCC_COUNT,
                                SUM(
                                    CASE
                                        WHEN F.SAP_DEPARTMENT = 'GCC'
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS GCC_COUNT,
                                SUM(
                                    CASE
                                        WHEN (
                                                F.SAP_DEPARTMENT = 'MLCC'
                                                AND F.CYCLE = '0N'
                                             )
                                          OR F.CYCLE IN ('KA','KC','KG')
                                        THEN 1
                                        ELSE 0
                                    END
                                ) AS KCC_COUNT
                            FROM RCMPA.SAP_FORMY F
                            WHERE F.SAP_COMPANY = 'BYPL'
                              AND F.READING_MONTH IN
                              (
                                  SELECT M.READING_MONTH
                                  FROM MONTHS M
                              )
                              AND F.SAP_MR_REASON_CODE = '01'
                              AND F.CSTS_CD = 'R'
                              AND F.METERNO NOT LIKE '%D%'
                              AND
                              (
                                    F.METERNO LIKE '92______'
                                 OR F.METERNO LIKE '99______'
                                 OR F.METERNO LIKE '98______'
                                 OR F.METERNO LIKE '97______'
                                 OR F.METERNO LIKE 'AL________'
                                 OR F.METERNO LIKE 'KI________'
                              )
                        )

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
                            SELECT
                                TOTAL_METERS,
                                ALLIED_COUNT,
                                KIMBAL_COUNT,
                                SLCC_COUNT,
                                MLCC_COUNT,
                                GCC_COUNT,
                                KCC_COUNT
                            FROM SAP_SLCC_DATA
                            UNION ALL
                            SELECT
                                TOTAL_METERS,
                                ALLIED_COUNT,
                                KIMBAL_COUNT,
                                SLCC_COUNT,
                                MLCC_COUNT,
                                GCC_COUNT,
                                KCC_COUNT
                            FROM SAP_FORMY_DATA
                        )";

                using (OracleCommand cmd = new OracleCommand(query, con))
                {
                    cmd.Parameters.Add(":READING_MONTH", OracleDbType.Varchar2).Value = ReadingMonth;
                    using (OracleDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            totalMeterSummary.TotalMeter = dr["TOTAL_METERS"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TOTAL_METERS"]);
                            totalMeterSummary.AlliedCount = dr["ALLIED_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ALLIED_COUNT"]);
                            totalMeterSummary.KimbalCount = dr["KIMBAL_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["KIMBAL_COUNT"]);
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

                    BASE_DATA AS
                    (
                        SELECT /*+ PARALLEL(SF,8) */

                               SF.MTR_READ_MODE,
                               SF.READING_DATE,
                               SF.NEW_MTR_NO,
                               SF.MTR_CORR_STS,
                               SF.MTR_NO_CORR

                        FROM RCMPA.SAP_SLCC_FORMY SF

                        WHERE SF.SAP_COMPANY = 'BYPL'

                          AND SF.READING_MONTH IN
                          (
                              SELECT M.READING_MONTH
                              FROM MONTHS M
                          )

                          AND SF.SAP_MR_REASON_CODE = '01'

                          AND SF.CSTS_CD = 'R'

                          AND SF.METERNO NOT LIKE '%D%'

                          AND
                          (
                                SF.METERNO LIKE '92______'
                             OR SF.METERNO LIKE '99______'
                             OR SF.METERNO LIKE '98______'
                             OR SF.METERNO LIKE '97______'
                             OR SF.METERNO LIKE 'AL________'
                             OR SF.METERNO LIKE 'KI________'
                          )

                        UNION ALL

                        SELECT /*+ PARALLEL(F,8) */

                               F.MTR_READ_MODE,
                               F.READING_DATE,
                               F.NEW_MTR_NO,
                               F.MTR_CORR_STS,
                               F.MTR_NO_CORR

                        FROM RCMPA.SAP_FORMY F

                        WHERE F.SAP_COMPANY = 'BYPL'

                          AND F.READING_MONTH IN
                          (
                              SELECT M.READING_MONTH
                              FROM MONTHS M
                          )

                          AND F.SAP_MR_REASON_CODE = '01'

                          AND F.CSTS_CD = 'R'

                          AND F.METERNO NOT LIKE '%D%'

                          AND
                          (
                                F.METERNO LIKE '92______'
                             OR F.METERNO LIKE '99______'
                             OR F.METERNO LIKE '98______'
                             OR F.METERNO LIKE '97______'
                             OR F.METERNO LIKE 'AL________'
                             OR F.METERNO LIKE 'KI________'
                          )
                    ),

                    SUMMARY AS
                    (
                        SELECT /*+ PARALLEL(8) */

                            SUM(
                                CASE
                                    WHEN MTR_READ_MODE = '1'
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS HES_DOWNLOAD,

                            SUM(
                                CASE
                                    WHEN MTR_READ_MODE = '0'
                                      OR (
                                            MTR_READ_MODE IS NULL
                                            AND READING_DATE IS NOT NULL
                                         )
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS MANUAL,

                            SUM(
                                CASE
                                    WHEN READING_DATE IS NULL
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS READING_PENDING,

                            SUM(
                                CASE
                                    WHEN MTR_READ_MODE = '1'
                                     AND (
                                            NEW_MTR_NO IS NOT NULL
                                         OR MTR_CORR_STS IS NOT NULL
                                         OR MTR_NO_CORR IS NOT NULL
                                     )
                                    THEN 1
                                    ELSE 0
                                END
                            ) AS MISMATCH

                        FROM BASE_DATA
                    )

                    SELECT

                        HES_DOWNLOAD,

                        (
                            MISMATCH
                            + MANUAL
                            + READING_PENDING
                        ) AS HES_FAILED,

                        (
                            HES_DOWNLOAD
                            + MISMATCH
                            + MANUAL
                            + READING_PENDING
                        ) AS TOTAL,

                        ROUND(
                            HES_DOWNLOAD * 100 /
                            NULLIF(
                                HES_DOWNLOAD
                                + MISMATCH
                                + MANUAL
                                + READING_PENDING,
                                0
                            ),
                            2
                        ) AS DOWNLOAD_PERCENTAGE,

                        ROUND(
                            (
                                MISMATCH
                                + MANUAL
                                + READING_PENDING
                            ) * 100 /
                            NULLIF(
                                HES_DOWNLOAD
                                + MISMATCH
                                + MANUAL
                                + READING_PENDING,
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



        // Get Meter Download Detailed Summary Allied + Kimbal including all department for the current month till day - 1
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

                            WHEN SUBSTR(F.METERNO, 1, 2) IN('91','KI')
                                THEN 'KIMBAL'
                        END AS METER_TYPE,

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

                    SLCC_SUMMARY AS
                    (
                        SELECT /*+ PARALLEL(SF,8) */

                               SF.SAP_DEPARTMENT AS DEPARTMENT,

                               COUNT(*) AS TOTAL_METERS,

                               SUM(
                                   CASE
                                       WHEN SF.MTR_READ_MODE = '1'
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS HES_DOWNLOAD,

                               SUM(
                                   CASE
                                       WHEN SF.MTR_READ_MODE = '0'
                                         OR (
                                               SF.MTR_READ_MODE IS NULL
                                               AND SF.READING_DATE IS NOT NULL
                                            )
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS MANUAL,

                               SUM(
                                   CASE
                                       WHEN SF.READING_DATE IS NULL
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS PENDING,

                               SUM(
                                   CASE
                                       WHEN SF.MTR_READ_MODE = '1'
                                        AND (
                                               SF.NEW_MTR_NO IS NOT NULL
                                            OR SF.MTR_CORR_STS IS NOT NULL
                                            OR SF.MTR_NO_CORR IS NOT NULL
                                            )
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS MISMATCH

                        FROM RCMPA.SAP_SLCC_FORMY SF

                        WHERE SF.SAP_COMPANY = 'BYPL'

                          AND SF.READING_MONTH IN
                          (
                              SELECT M.READING_MONTH
                              FROM MONTHS M
                          )

                          AND SF.SAP_MR_REASON_CODE = '01'

                          AND SF.CSTS_CD = 'R'

                          AND SF.METERNO NOT LIKE '%D%'

                          AND
                          (
                               SF.METERNO LIKE '92______'
                            OR SF.METERNO LIKE '99______'
                            OR SF.METERNO LIKE '98______'
                            OR SF.METERNO LIKE '97______'
                            OR SF.METERNO LIKE 'AL________'
                            OR SF.METERNO LIKE 'KI________'
                          )

                        GROUP BY SF.SAP_DEPARTMENT
                    ),

                    FORMY_SUMMARY AS
                    (
                        SELECT /*+ PARALLEL(F,8) */

                               CASE
                                   WHEN F.SAP_DEPARTMENT = 'MLCC'
                                        AND F.CYCLE = '0N'
                                   THEN 'KCC'

                                   ELSE F.SAP_DEPARTMENT
                               END AS DEPARTMENT,

                               COUNT(*) AS TOTAL_METERS,

                               SUM(
                                   CASE
                                       WHEN F.MTR_READ_MODE = '1'
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS HES_DOWNLOAD,

                               SUM(
                                   CASE
                                       WHEN F.MTR_READ_MODE = '0'
                                         OR (
                                               F.MTR_READ_MODE IS NULL
                                               AND F.READING_DATE IS NOT NULL
                                            )
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS MANUAL,

                               SUM(
                                   CASE
                                       WHEN F.READING_DATE IS NULL
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS PENDING,

                               SUM(
                                   CASE
                                       WHEN F.MTR_READ_MODE = '1'
                                        AND (
                                               F.NEW_MTR_NO IS NOT NULL
                                            OR F.MTR_CORR_STS IS NOT NULL
                                            OR F.MTR_NO_CORR IS NOT NULL
                                            )
                                       THEN 1
                                       ELSE 0
                                   END
                               ) AS MISMATCH

                        FROM RCMPA.SAP_FORMY F

                        WHERE F.SAP_COMPANY = 'BYPL'

                          AND F.READING_MONTH IN
                          (
                              SELECT M.READING_MONTH
                              FROM MONTHS M
                          )

                          AND F.SAP_MR_REASON_CODE = '01'

                          AND F.CSTS_CD = 'R'

                          AND F.METERNO NOT LIKE '%D%'

                          AND
                          (
                               F.METERNO LIKE '92______'
                            OR F.METERNO LIKE '99______'
                            OR F.METERNO LIKE '98______'
                            OR F.METERNO LIKE '97______'
                            OR F.METERNO LIKE 'AL________'
                            OR F.METERNO LIKE 'KI________'
                          )

                        GROUP BY
                            CASE
                                WHEN F.SAP_DEPARTMENT = 'MLCC'
                                     AND F.CYCLE = '0N'
                                THEN 'KCC'

                                ELSE F.SAP_DEPARTMENT
                            END
                    ),

                    FINAL_SUMMARY AS
                    (
                        SELECT
                            DEPARTMENT,
                            TOTAL_METERS,
                            HES_DOWNLOAD,
                            MANUAL,
                            PENDING,
                            MISMATCH
                        FROM SLCC_SUMMARY

                        UNION ALL

                        SELECT
                            DEPARTMENT,
                            TOTAL_METERS,
                            HES_DOWNLOAD,
                            MANUAL,
                            PENDING,
                            MISMATCH
                        FROM FORMY_SUMMARY
                    )

                    SELECT /*+ PARALLEL(8) */

                           DEPARTMENT,

                           SUM(TOTAL_METERS) AS TOTAL_METERS,

                           SUM(HES_DOWNLOAD) AS HES_DOWNLOAD,

                           SUM(MANUAL) AS MANUAL,

                           SUM(PENDING) AS PENDING,

                           SUM(MISMATCH) AS MISMATCH,

                           SUM(MANUAL)
                           + SUM(PENDING)
                           + SUM(MISMATCH) AS DOWNLOAD_FAILED

                    FROM FINAL_SUMMARY
                    GROUP BY DEPARTMENT
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
                                Failed = dr["DOWNLOAD_FAILED"] == DBNull.Value ? 0 : Convert.ToInt32(dr["DOWNLOAD_FAILED"]),

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
