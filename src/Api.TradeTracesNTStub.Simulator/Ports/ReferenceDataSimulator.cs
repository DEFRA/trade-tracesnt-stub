using TracesNT.WebServices;

namespace Api.TradeTracesNTStub.Simulator.Ports;

public class ReferenceDataSimulator : ReferenceDataPort
{
    public Task<GetClassificationTreesResponse> getClassificationTreesAsync(GetClassificationTreesRequest request) =>
        throw SimulatorFaults.NotImplemented("getClassificationTrees");

    public Task<GetClassificationTreeResponse> getClassificationTreeAsync(GetClassificationTreeRequest request) =>
        throw SimulatorFaults.NotImplemented("getClassificationTree");

    public Task<GetClassificationTreeUpdatesResponse> getClassificationTreeUpdatesAsync(
        GetClassificationTreeUpdatesRequest request
    ) => throw SimulatorFaults.NotImplemented("getClassificationTreeUpdates");

    public Task<GetClassificationTreeNodeDetailResponse> getClassificationTreeNodeDetailAsync(
        GetClassificationTreeNodeDetailRequest request
    ) => throw SimulatorFaults.NotImplemented("getClassificationTreeNodeDetail");

    public Task<GetCertificateModelResponse> getCertificateModelAsync(GetCertificateModelRequest request) =>
        throw SimulatorFaults.NotImplemented("getCertificateModel");

    public Task<GetLaboratoryTestCategoriesResponse> getLaboratoryTestCategoriesAsync(
        GetLaboratoryTestCategoriesRequest request
    ) => throw SimulatorFaults.NotImplemented("getLaboratoryTestCategories");

    public Task<GetLaboratoryTestsResponse> getLaboratoryTestsAsync(GetLaboratoryTestsRequest request) =>
        throw SimulatorFaults.NotImplemented("getLaboratoryTests");

    public Task<GetClassificationSectionsResponse> getClassificationSectionsAsync(
        GetClassificationSectionsRequest request
    ) => throw SimulatorFaults.NotImplemented("getClassificationSections");

    public Task<GetMetadatasResponse> getMetadatasAsync(GetMetadatasRequest request) =>
        throw SimulatorFaults.NotImplemented("getMetadatas");
}
