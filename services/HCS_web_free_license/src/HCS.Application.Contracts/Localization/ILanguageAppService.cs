using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace HCS.Localization;

public interface ILanguageAppService : IApplicationService
{
    Task<PagedResultDto<LanguageDto>> GetListAsync(GetLanguagesInput input);
    Task<List<LanguageOptionDto>> GetEnabledListAsync();
    Task<LanguageDto> GetAsync(Guid id);
    Task<LanguageDto> CreateAsync(CreateLanguageDto input);
    Task<LanguageDto> UpdateAsync(Guid id, UpdateLanguageDto input);
    Task DeleteAsync(Guid id);
}
