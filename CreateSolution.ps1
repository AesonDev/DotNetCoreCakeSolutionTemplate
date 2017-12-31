

$ProjectPath = "./Demoapp/"
$templateUrl = "https://github.com/AesonDev/DotNetCoreCakeSolutionTemplate/raw/master/DotNetCoreCakeSolutionTemplate.zip"

Invoke-WebRequest -Uri $templateUrl -OutFile .\template.zip
Expand-Archive -Path .\template.zip -DestinationPath $ProjectPath

Remove-item .\template.zip