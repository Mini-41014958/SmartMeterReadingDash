using System.Text;

namespace SmartMeterReadingDash.Services
{
    public class emailService
    {
        private const string ServiceUrl = "http://10.125.64.86/delhiV2/ISUService.asmx";

        public async Task<string> SendMailAsync(string toAddress, string ccAddress, string bccAddress,  string senderMailId, string subject, string htmlBody)
        {
            string soapBody = $@"
                <soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                               xmlns:xsd='http://www.w3.org/2001/XMLSchema'
                               xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                  <soap:Body>
                    <SendEmail_smtp xmlns='http://tempuri.org/'>
                      <toAddress>{toAddress}</toAddress>
                      <CCAddress>{ccAddress}</CCAddress>
                      <BCCAddress>{bccAddress}</BCCAddress>
                      <subject>{System.Security.SecurityElement.Escape(subject)}</subject>
                      <Mailbody><![CDATA[{htmlBody}]]></Mailbody>
                      <MailAttachmentAddress></MailAttachmentAddress>
                      <_sHTML>Y</_sHTML>
                      <SenderMailID>{senderMailId}</SenderMailID>
                    </SendEmail_smtp>
                  </soap:Body>
                </soap:Envelope>";

            using var httpClient = new HttpClient();
            var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tempuri.org/SendEmail_smtp");

            var response = await httpClient.PostAsync(ServiceUrl, content);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> SendOtpAsync( string toAddress, string subject,string htmlBody,
         string senderMailId = "Brpl.Nonsapsupport@reliancegroupindia.com")
        {
            string soapBody = $@"
                <soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
                               xmlns:xsd='http://www.w3.org/2001/XMLSchema'
                               xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>
                  <soap:Body>
                    <SendEmail_smtp xmlns='http://tempuri.org/'>
                      <toAddress>{toAddress}</toAddress>
                      <CCAddress>{toAddress}</CCAddress>
                      <BCCAddress>{toAddress}</BCCAddress>
                      <subject>{System.Security.SecurityElement.Escape(subject)}</subject>
                      <Mailbody><![CDATA[{htmlBody}]]></Mailbody>
                      <MailAttachmentAddress></MailAttachmentAddress>
                      <_sHTML>Y</_sHTML>
                      <SenderMailID>{senderMailId}</SenderMailID>
                    </SendEmail_smtp>
                  </soap:Body>
                </soap:Envelope>";

            using var httpClient = new HttpClient();
            var content = new StringContent(soapBody, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "http://tempuri.org/SendEmail_smtp");
            var response = await httpClient.PostAsync(ServiceUrl, content);
            return await response.Content.ReadAsStringAsync();
        }

    }
}

