using System.Security;
using System.Text;
using System.Xml.Linq;

namespace SmartMeterReadingDash.Services
{
    public class EmailService
    {
        private const string ServiceUrl =
            "http://10.125.64.86/delhiV2/ISUService.asmx";

        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task<string> SendMailAsync(
     string toAddress,
     string ccAddress,
     string bccAddress,
     string senderMailId,
     string subject,
     string htmlBody,
     string? attachmentPath = null)
        {
            string escapedTo =
                SecurityElement.Escape(
                    toAddress ?? string.Empty) ?? string.Empty;

            string escapedCc =
                SecurityElement.Escape(
                    ccAddress ?? string.Empty) ?? string.Empty;

            string escapedBcc =
                SecurityElement.Escape(
                    bccAddress ?? string.Empty) ?? string.Empty;

            string escapedSubject =
                SecurityElement.Escape(
                    subject ?? string.Empty) ?? string.Empty;

            string escapedSender =
                SecurityElement.Escape(
                    senderMailId ?? string.Empty) ?? string.Empty;

            string escapedAttachment =
                SecurityElement.Escape(
                    attachmentPath ?? string.Empty) ?? string.Empty;

            string safeHtmlBody =
                (htmlBody ?? string.Empty)
                    .Replace(
                        "]]>",
                        "]]]]><![CDATA[>");

            string soapBody = $@"
<soap:Envelope
    xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
    xmlns:xsd='http://www.w3.org/2001/XMLSchema'
    xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>

    <soap:Body>

        <SendEmail_smtp xmlns='http://tempuri.org/'>

            <toAddress>{escapedTo}</toAddress>

            <CCAddress>{escapedCc}</CCAddress>

            <BCCAddress>{escapedBcc}</BCCAddress>

            <subject>{escapedSubject}</subject>

            <Mailbody><![CDATA[{safeHtmlBody}]]></Mailbody>

            <MailAttachmentAddress>{escapedAttachment}</MailAttachmentAddress>

            <_sHTML>Y</_sHTML>

            <SenderMailID>{escapedSender}</SenderMailID>

        </SendEmail_smtp>

    </soap:Body>

</soap:Envelope>";

            using var httpClient = new HttpClient();

            using var content =
                new StringContent(
                    soapBody,
                    Encoding.UTF8,
                    "text/xml");

            content.Headers.Add(
                "SOAPAction",
                "http://tempuri.org/SendEmail_smtp");

            _logger.LogInformation(
                "Sending email through ISU email service. " +
                "To: {To}, CC: {CC}, BCC: {BCC}, Attachment: {Attachment}",
                toAddress,
                ccAddress,
                bccAddress,
                attachmentPath ?? "(none)");

            var response =
                await httpClient.PostAsync(
                    ServiceUrl,
                    content);

            string responseBody =
                await response.Content.ReadAsStringAsync();


            // ============================================================
            // HTTP LEVEL CHECK
            // ============================================================

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "ISU email service returned HTTP {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    responseBody);

                throw new HttpRequestException(
                    $"Email service returned HTTP " +
                    $"{(int)response.StatusCode}: {responseBody}");
            }


            // ============================================================
            // SOAP APPLICATION LEVEL CHECK
            // ============================================================

            string? sendResult = null;

            try
            {
                var xml =
                    XDocument.Parse(responseBody);

                sendResult =
                    xml
                        .Descendants()
                        .FirstOrDefault(
                            x =>
                                x.Name.LocalName ==
                                "SendEmail_smtpResult")
                        ?.Value
                        ?.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to parse ISU email service response. Response: {Response}",
                    responseBody);

                throw new InvalidOperationException(
                    "Invalid response received from ISU email service.",
                    ex);
            }


            // ============================================================
            // ACTUAL EMAIL RESULT
            // ============================================================

            if (!string.Equals(
                    sendResult,
                    "Y",
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "ISU email service FAILED. " +
                    "SendEmail_smtpResult={Result}. " +
                    "To={To}, CC={CC}, BCC={BCC}, Attachment={Attachment}. " +
                    "Full response={Response}",
                    sendResult,
                    toAddress,
                    ccAddress,
                    bccAddress,
                    attachmentPath,
                    responseBody);

                throw new InvalidOperationException(
                    $"ISU email service failed. " +
                    $"SendEmail_smtpResult={sendResult}");
            }


            // ============================================================
            // ACTUAL SUCCESS
            // ============================================================

            _logger.LogInformation(
                "Email sent successfully through ISU email service. " +
                "To: {To}, CC: {CC}, BCC: {BCC}",
                toAddress,
                ccAddress,
                bccAddress);

            return responseBody;
        }

        public async Task<string> SendOtpAsync(
            string toAddress,
            string subject,
            string htmlBody,
            string senderMailId =
                "Brpl.Nonsapsupport@reliancegroupindia.com")
        {
            return await SendMailAsync(
                toAddress: toAddress,
                ccAddress: toAddress,
                bccAddress: toAddress,
                senderMailId: senderMailId,
                subject: subject,
                htmlBody: htmlBody,
                attachmentPath: null);
        }
    }
}