using Api.TradeTracesNTStub.Simulator.Ports;
using CoreWCF.Description;
using TracesNT.WebServices;

namespace TradeTracesNTStub.Test.Simulator;

/// <summary>
/// The TRACES contracts come from the shared <c>Defra.Trade.Gateway.TracesNT</c> package, so they carry
/// <c>System.ServiceModel</c> attributes rather than CoreWCF's own. CoreWCF recognises those by
/// attribute name and namespace — but if that compatibility ever lapsed for
/// <c>XmlSerializerFormat</c> in particular, CoreWCF would silently fall back to the
/// DataContractSerializer and every response would go out in the wrong wire format.
/// </summary>
public class ContractSerializationTests
{
    private static readonly ContractDescription[] s_contracts =
    [
        ContractDescription.GetContract<ChedCertificateSimulator>(typeof(ChedCertificatePort)),
        ContractDescription.GetContract<EuIntraCertificateSimulator>(typeof(EuIntraCertificatePort)),
        ContractDescription.GetContract<DocomCertificateRetrievalSimulator>(typeof(DocomCertificateRetrievalPort)),
        ContractDescription.GetContract<ReferenceDataSimulator>(typeof(ReferenceDataPort)),
        ContractDescription.GetContract<CustomsCertexChedSimulator>(typeof(CustomsCertexChedPort)),
    ];

    [Fact]
    public void EveryOperationSerialisesWithXmlSerializer()
    {
        foreach (var contract in s_contracts)
        {
            contract.Operations.Should().NotBeEmpty($"{contract.Name} should expose operations");

            foreach (var operation in contract.Operations)
            {
                operation
                    .OperationBehaviors.OfType<XmlSerializerOperationBehavior>()
                    .Should()
                    .NotBeEmpty(
                        $"{contract.Name}.{operation.Name} must use the XmlSerializer, not the DataContractSerializer"
                    );
            }
        }
    }

    [Fact]
    public void EveryContractKeepsItsTracesNamespace()
    {
        s_contracts.Should().AllSatisfy(contract => contract.Namespace.Should().StartWith("http://ec.europa.eu/"));
    }

    [Fact]
    public void AllFivePortsAreCovered()
    {
        s_contracts.Should().HaveCount(5);

        // CHED 9, EU-INTRA 5, DOCOM 3, reference data 9, customs 4.
        s_contracts.Sum(c => c.Operations.Count).Should().Be(30);
    }
}
