#requires -Version 7.4
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:5080')
$ErrorActionPreference = 'Stop'
$taskAdmin = & "$PSScriptRoot/token.ps1" -Role Admin -Subject instructor
$taskStudent = & "$PSScriptRoot/token.ps1" -Role Student -Subject alumno
$taskOther = & "$PSScriptRoot/token.ps1" -Role Student -Subject otro
$taskAdminHeaders = @{ Authorization = "Bearer $taskAdmin" }
$taskStudentHeaders = @{ Authorization = "Bearer $taskStudent" }
$taskOtherHeaders = @{ Authorization = "Bearer $taskOther" }
$taskCaseVariant = & "$PSScriptRoot/token.ps1" -Role Student -Subject ALUMNO
$taskCaseHeaders = @{ Authorization = "Bearer $taskCaseVariant" }
$taskHealth = Invoke-WebRequest "$BaseUrl/health/ready"
if ($taskHealth.StatusCode -ne 200) { throw 'Readiness incorrecto.' }
$taskUnauthorized = Invoke-WebRequest "$BaseUrl/api/v1/products" -SkipHttpErrorCheck
if ($taskUnauthorized.StatusCode -ne 401) { throw 'Se esperaba 401 sin token.' }
$taskProductBody = @{ sku = 'CURSO-' + [guid]::NewGuid().ToString('N').Substring(0,8); name = 'Curso de arquitectura'; price = 125.50 } | ConvertTo-Json
$taskProduct = Invoke-RestMethod "$BaseUrl/api/v1/products" -Method Post -Headers $taskAdminHeaders -ContentType 'application/json' -Body $taskProductBody
$taskOrderBody = @{ items = @(@{ productId = $taskProduct.id; quantity = 2 }) } | ConvertTo-Json -Depth 4
$taskOrder = Invoke-RestMethod "$BaseUrl/api/v1/orders" -Method Post -Headers $taskStudentHeaders -ContentType 'application/json' -Body $taskOrderBody
if ($taskOrder.total -ne 251) { throw 'Total incorrecto.' }
$taskDenied = Invoke-WebRequest "$BaseUrl/api/v1/orders/$($taskOrder.id)" -Headers $taskOtherHeaders -SkipHttpErrorCheck
if ($taskDenied.StatusCode -ne 403) { throw 'Falló aislamiento de propietario.' }
$taskCaseOrders = Invoke-RestMethod "$BaseUrl/api/v1/orders" -Headers $taskCaseHeaders
if ($taskCaseOrders.items.id -contains $taskOrder.id) { throw 'Falló aislamiento ordinal de identidad (alumno vs ALUMNO).' }
$taskCancelled = Invoke-RestMethod "$BaseUrl/api/v1/orders/$($taskOrder.id)/cancel" -Method Post -Headers $taskStudentHeaders -ContentType 'application/json' -Body (@{ version = $taskOrder.version } | ConvertTo-Json)
if ($taskCancelled.status -ne 'Cancelled') { throw 'Estado de cancelación incorrecto.' }
Write-Host "Smoke correcto: 401, producto, pedido total251, protección403, identidad ordinal, cancelación. OrderId=$($taskOrder.id)"
