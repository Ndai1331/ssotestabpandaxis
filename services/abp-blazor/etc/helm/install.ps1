param (
	$ChartName="abptestwithsso",
	$Namespace="abptestwithsso-local",
	$ReleaseName="abptestwithsso-local",
	$DotnetEnvironment="Staging",
    $User = ""
)

# Create values.localdev.yaml if not exists
$localDevFilePath = Join-Path $PSScriptRoot "abptestwithsso/values.localdev.yaml"
if (!(Test-Path $localDevFilePath)) {
	New-Item -ItemType File -Path $localDevFilePath | Out-Null
}

$FinalReleaseName = $ReleaseName
if([string]::IsNullOrEmpty($User) -eq $false)
{
    $Namespace += '-' + $User
    $FinalReleaseName += '-' + $User
}

# Install (or upgrade) the Helm chart
helm upgrade --install ${FinalReleaseName} ${ChartName} --namespace ${Namespace} --create-namespace --set global.dotnetEnvironment=${DotnetEnvironment} -f "abptestwithsso/values.localdev.yaml" -f "$ChartName/values.${ReleaseName}.yaml"
exit $LASTEXITCODE