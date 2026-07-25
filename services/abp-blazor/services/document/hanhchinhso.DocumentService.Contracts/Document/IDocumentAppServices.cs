using Volo.Abp.Application.Services;

namespace hanhchinhso.DocumentService.Documents;

public interface IDocumentAppService :
    ICrudAppService<DocumentDto, Guid, DocumentListInput, CreateUpdateDocumentDto>;

public interface IDocumentFileAppService
{
    Task<IReadOnlyList<DocumentFileDto>> GetListAsync(Guid documentId);
    Task DeleteAsync(Guid id, string concurrencyStamp);
}
