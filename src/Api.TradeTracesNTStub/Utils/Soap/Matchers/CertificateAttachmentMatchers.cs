using WireMock.Matchers;

namespace Api.TradeTracesNTStub.Utils.Soap.Matchers;

public static class CertificateAttachmentMatchers
{
    public static IMatcher[] ValidSubmitCertificateAttachmentRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'SubmitCertificateAttachmentRequest']/*[local-name() = 'Attachment' and text()]")
            ])
            .ToArray();

    public static IMatcher[] ValidGetCertificateAttachmentRequest() =>
        MessageMatchers.ValidHeaders()
            .Concat(
            [
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetCertificateAttachmentRequest']/*[local-name() = 'FileName' and text()]"),
                new XPathMatcher(MessageMatchers.BodyXPath + "/*[local-name() = 'GetCertificateAttachmentRequest']/*[local-name() = 'DocumentId' and text()]"),
            ])
            .ToArray();
}