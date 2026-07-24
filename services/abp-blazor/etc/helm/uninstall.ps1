param (
    $Namespace = "hanhchinhso-local",
    $ReleaseName = "hanhchinhso-local",
    $User = ""
)

if ([string]::IsNullOrEmpty($User) -eq $false) {
    $Namespace += '-' + $User
    $ReleaseName += '-' + $User
}

function UninstallHelmReleaseIfExists {
    param (
        [string]$Name,
        [string]$Namespace
    )

    $exists = helm list -n $Namespace --short | Where-Object { $_ -eq $Name }
    if ($exists) {
        Write-Host "Uninstalling $Name in namespace $Namespace..."
        helm uninstall $Name --namespace $Namespace
    }
}

UninstallHelmReleaseIfExists -Name "abp-wg-easy" -Namespace $Namespace
UninstallHelmReleaseIfExists -Name "abp-studio-proxy" -Namespace $Namespace
UninstallHelmReleaseIfExists -Name $ReleaseName -Namespace $Namespace

exit $LASTEXITCODE
