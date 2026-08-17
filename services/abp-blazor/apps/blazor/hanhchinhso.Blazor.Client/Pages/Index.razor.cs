using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using hanhchinhso.LanguageService.Localization;

namespace hanhchinhso.Blazor.Client.Pages;

public partial class Index
{
    public Index()
    {
        LocalizationResource = typeof(LanguageServiceResource);
    }



    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    private void Login()
    {
        Navigation.NavigateTo("/Account/Login", true);
    }

    protected override void Dispose(bool disposing)
    {
        PageLayout.ShowToolbar = true;
    }
}